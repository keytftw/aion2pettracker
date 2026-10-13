// SPDX-License-Identifier: GPL-3.0-only
// Adapted from AION 2 Alerts 0.5.6 / cyberbadger6969/aion2-dps-meter; see THIRD_PARTY_NOTICES.md.
using System.Buffers;
using K4os.Compression.LZ4;

namespace PetTracker.Capture.Protocol;

public delegate void PacketHandler(ushort opcode, ReadOnlySpan<byte> body);

/// <summary>
/// Splits one server→client byte stream into game packets.
/// <code>
/// frame  = varint len, [0xF0..0xFE], payload        (frame size = len + varintBytes − 4)
/// payload = opcode(2) body | FF FF u32 rawSize lz4-block   (the block holds more frames, possibly nested)
/// </code>
/// 0x00 bytes between frames are padding. After lost data the decoder hunts for the next heartbeat frame
/// (<c>0E 00 36</c>, ~19 per second) instead of guessing at varints, which would produce phantom events.
/// </summary>
public sealed class FrameDecoder
{
    public const int MaxFrame = 65_535;
    public const int MaxUnbundledWait = 16_384;
    public const int MaxBundle = 8 * 1024 * 1024;
    private const int MaxHuntBytes = 256 * 1024;

    private static readonly byte[][] HeartbeatSignatures = [[0x0E, 0x00, 0x36], [0x06, 0x00, 0x36]];

    private readonly PacketHandler _onPacket;
    private byte[] _buf = new byte[128 * 1024];
    private int _len;
    private bool _hunting;
    private int _huntedBytes;

    /// <param name="startsAligned">True when the stream was captured from its first byte (SYN seen). Otherwise the
    /// first segment may start mid-frame, so the decoder begins by hunting for a heartbeat.</param>
    public FrameDecoder(PacketHandler onPacket, bool startsAligned = true)
    {
        _onPacket = onPacket;
        _hunting = !startsAligned;
    }

    public long Frames { get; private set; }
    public long Bundles { get; private set; }
    public long BadBundles { get; private set; }
    public long Resyncs { get; private set; }
    public long DroppedBytes { get; private set; }
    public event Action? DataLost;

    /// <summary>Call when the TCP layer skipped missing bytes: the next byte is not a frame boundary.</summary>
    public void Reset()
    {
        _len = 0;
        _hunting = true;
        _huntedBytes = 0;
    }

    public void Feed(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return;
        if (_len + data.Length > _buf.Length)
        {
            var bigger = new byte[Math.Max(_buf.Length * 2, _len + data.Length)];
            Buffer.BlockCopy(_buf, 0, bigger, 0, _len);
            _buf = bigger;
        }
        data.CopyTo(_buf.AsSpan(_len));
        _len += data.Length;
        Drain();
    }

    private void Drain()
    {
        var pos = 0;
        while (pos < _len)
        {
            var span = _buf.AsSpan(pos, _len - pos);

            if (_hunting)
            {
                var hb = FindHeartbeat(span);
                if (hb < 0)
                {
                    var keep = Math.Min(span.Length, 2); // a signature may straddle two segments
                    var skip = span.Length - keep;
                    pos += skip;
                    DroppedBytes += skip;
                    _huntedBytes += skip;
                    // Keep hunting. Never guess frame boundaries when the signature is unsupported.
                    break;
                }
                pos += hb;
                DroppedBytes += hb;
                _hunting = false;
                continue;
            }

            if (span[0] == 0x00)
            {
                pos++;
                continue;
            }

            if (!Wire.TryVarint(span, 0, out var len, out var vlen))
            {
                if (span.Length < 5) break; // need more bytes
                pos += Resync();
                continue;
            }

            var size = (long)len + vlen - 4;
            if (len < 6 || size > MaxFrame)
            {
                pos += Resync();
                continue;
            }

            if (size > span.Length)
            {
                if (size > MaxUnbundledWait && !IsBundle(span, vlen))
                {
                    pos += Resync();
                    continue;
                }
                break; // wait for the rest of the frame
            }

            HandleFrame(span[..(int)size], vlen, depth: 0);
            pos += (int)size;
        }

        if (pos > 0)
        {
            _len -= pos;
            if (_len > 0) Buffer.BlockCopy(_buf, pos, _buf, 0, _len);
        }
    }

    /// <summary>Returns how many bytes to skip. Normally switches into heartbeat hunting.</summary>
    private int Resync()
    {
        Resyncs++;
        DroppedBytes++;
        DataLost?.Invoke();
        _hunting = true;
        _huntedBytes = 0;
        return 1;
    }

    private static int FindHeartbeat(ReadOnlySpan<byte> span)
    {
        var best = -1;
        foreach (var sig in HeartbeatSignatures)
        {
            var i = span.IndexOf(sig);
            if (i >= 0 && (best < 0 || i < best)) best = i;
        }
        return best;
    }

    private static bool IsBundle(ReadOnlySpan<byte> frame, int vlen)
    {
        var o = vlen;
        if (o < frame.Length && frame[o] is >= 0xF0 and <= 0xFE) o++;
        return o + 1 < frame.Length && frame[o] == 0xFF && frame[o + 1] == 0xFF;
    }

    private void HandleFrame(ReadOnlySpan<byte> frame, int vlen, int depth)
    {
        Frames++;
        var o = vlen;
        if (o < frame.Length && frame[o] is >= 0xF0 and <= 0xFE) o++;
        if (o + 2 > frame.Length) return;

        if (frame[o] == 0xFF && frame[o + 1] == 0xFF)
        {
            Bundles++;
            o += 2;
            if (!Wire.TryU32(frame, ref o, out var rawSize) || rawSize == 0 || rawSize > MaxBundle || depth > 4)
            {
                BadBundles++;
                DataLost?.Invoke();
                return;
            }
            var target = ArrayPool<byte>.Shared.Rent((int)rawSize);
            try
            {
                var n = LZ4Codec.Decode(frame[o..], target.AsSpan(0, (int)rawSize));
                if (n <= 0 || n!=rawSize)
                {
                    BadBundles++;
                    DataLost?.Invoke();
                    return;
                }
                WalkInner(target.AsSpan(0, n), depth + 1);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(target);
            }
            return;
        }

        var opcode = (ushort)(frame[o] << 8 | frame[o + 1]);
        _onPacket(opcode, frame[(o + 2)..]);
    }

    /// <summary>Frames inside a decompressed bundle. No resync here: an invalid length ends the bundle.</summary>
    private void WalkInner(ReadOnlySpan<byte> data, int depth)
    {
        var pos = 0;
        while (pos < data.Length)
        {
            if (data[pos] == 0x00)
            {
                pos++;
                continue;
            }
            var span = data[pos..];
            if (!Wire.TryVarint(span, 0, out var len, out var vlen)){DataLost?.Invoke();return;}
            var size = (long)len + vlen - 4;
            if (len < 6 || size > span.Length){DataLost?.Invoke();return;}
            HandleFrame(span[..(int)size], vlen, depth);
            pos += (int)size;
        }
    }
}

