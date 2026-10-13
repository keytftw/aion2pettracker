// SPDX-License-Identifier: GPL-3.0-only
// Adapted from AION 2 Alerts 0.5.6 / cyberbadger6969/aion2-dps-meter; see THIRD_PARTY_NOTICES.md.
namespace PetTracker.Capture;

/// <summary>
/// In-order delivery of one TCP direction. Duplicates and overlaps are trimmed, out-of-order segments wait in a
/// small buffer, and a hole that does not fill within <see cref="GapTimeoutMs"/> (or a buffer over 2 MB) is skipped
/// — the caller is told so the frame decoder can resynchronise.
/// </summary>
public sealed class TcpReassembler
{
    public const int MaxPendingBytes = 2 * 1024 * 1024;
    public const int GapTimeoutMs = 1_500;

    private readonly Action<ReadOnlyMemory<byte>> _onData;
    private readonly Action _onGap;
    private readonly List<(uint Seq, byte[] Data, long TimeMs)> _pending = new();
    private int _pendingBytes;
    private uint _next;
    private bool _started;

    public TcpReassembler(Action<ReadOnlyMemory<byte>> onData, Action onGap)
    {
        _onData = onData;
        _onGap = onGap;
    }

    public long Delivered { get; private set; }
    public long Duplicates { get; private set; }
    public long OutOfOrder { get; private set; }
    public long Gaps { get; private set; }

    /// <summary>Anchor the stream at a known sequence number (ISN + 1 after a SYN).</summary>
    public void Start(uint nextSeq)
    {
        _next = nextSeq;
        _started = true;
    }

    public void Push(uint seq, ReadOnlySpan<byte> payload, long timeMs)
    {
        if (payload.IsEmpty) return;
        if (!_started) Start(seq);

        var diff = (int)(seq - _next);
        if (diff < 0)
        {
            if (-diff >= payload.Length)
            {
                Duplicates++;
                return;
            }
            payload = payload[-diff..];
            seq = _next;
            diff = 0;
        }

        if (diff == 0)
        {
            Deliver(payload.ToArray());
            DrainPending();
        }
        else
        {
            OutOfOrder++;
            var length = payload.Length;
            if (!_pending.Exists(p => p.Seq == seq && p.Data.Length >= length))
            {
                _pending.Add((seq, payload.ToArray(), timeMs));
                _pendingBytes += payload.Length;
            }
        }

        // Give up on a hole that is not going to be filled (packet lost before the capture point).
        while (_pending.Count > 0 && (_pendingBytes > MaxPendingBytes || timeMs - OldestPendingTime() > GapTimeoutMs))
        {
            SkipToEarliestPending();
            DrainPending();
        }
    }

    private long OldestPendingTime()
    {
        var t = long.MaxValue;
        foreach (var p in _pending) if (p.TimeMs < t) t = p.TimeMs;
        return t;
    }

    private void SkipToEarliestPending()
    {
        var best = 0;
        for (var i = 1; i < _pending.Count; i++)
            if ((int)(_pending[i].Seq - _pending[best].Seq) < 0) best = i;
        _next = _pending[best].Seq;
        Gaps++;
        _onGap();
    }

    private void DrainPending()
    {
        bool progressed;
        do
        {
            progressed = false;
            for (var i = 0; i < _pending.Count; i++)
            {
                var (seq, data, _) = _pending[i];
                var diff = (int)(seq - _next);
                if (diff > 0) continue;

                _pending.RemoveAt(i);
                _pendingBytes -= data.Length;
                if (-diff < data.Length) Deliver(-diff == 0 ? data : data[-diff..]);
                else Duplicates++;
                progressed = true;
                break;
            }
        } while (progressed);
    }

    private void Deliver(byte[] data)
    {
        _next += (uint)data.Length;
        Delivered += data.Length;
        _onData(data);
    }
}

