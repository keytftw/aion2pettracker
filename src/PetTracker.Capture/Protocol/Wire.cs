// SPDX-License-Identifier: GPL-3.0-only
// Adapted from AION 2 Alerts 0.5.6 / cyberbadger6969/aion2-dps-meter; see THIRD_PARTY_NOTICES.md.
using System.Buffers.Binary;
using System.Text;

namespace PetTracker.Capture.Protocol;

/// <summary>Primitive readers for the AION 2 wire format. All multi-byte integers are little-endian.</summary>
public static class Wire
{
    /// <summary>LEB128 unsigned varint, at most 5 bytes / 32 bits.</summary>
    public static bool TryVarint(ReadOnlySpan<byte> data, ref int offset, out uint value)
    {
        value = 0;
        if (offset < 0 || offset >= data.Length) return false;
        var shift = 0;
        for (var i = offset; i < data.Length && i - offset < 5; i++)
        {
            var b = data[i];
            if (shift == 28 && (b & 0xF0) != 0) { value = 0; return false; }
            value |= (uint)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                offset = i + 1;
                return true;
            }
            shift += 7;
        }
        value = 0;
        return false;
    }

    public static bool TryVarint(ReadOnlySpan<byte> data, int offset, out uint value, out int length)
    {
        var o = offset;
        if (TryVarint(data, ref o, out value))
        {
            length = o - offset;
            return true;
        }
        length = 0;
        return false;
    }

    public static bool TryU8(ReadOnlySpan<byte> data, ref int offset, out byte value)
    {
        if (offset < data.Length)
        {
            value = data[offset++];
            return true;
        }
        value = 0;
        return false;
    }

    public static bool TryU16(ReadOnlySpan<byte> data, ref int offset, out ushort value)
    {
        if (offset + 2 <= data.Length)
        {
            value = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
            offset += 2;
            return true;
        }
        value = 0;
        return false;
    }

    public static bool TryU32(ReadOnlySpan<byte> data, ref int offset, out uint value)
    {
        if (offset + 4 <= data.Length)
        {
            value = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
            return true;
        }
        value = 0;
        return false;
    }

    public static uint U24(ReadOnlySpan<byte> data, int offset) =>
        (uint)(data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16);

    public static uint U32(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);

    public static ushort U16(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    /// <summary>
    /// A character name field: 1–36 bytes of valid UTF-8 made only of letters and digits (any script) with at least
    /// one letter. Being this strict is what makes it safe to probe for names at guessed offsets.
    /// </summary>
    public static bool TryName(ReadOnlySpan<byte> field, out string name)
    {
        name = "";
        if (field.Length is < 1 or > 36) return false;
        string s;
        try
        {
            s = StrictUtf8.GetString(field);
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
        var letters = 0;
        foreach (var ch in s)
        {
            if (char.IsLetter(ch)) letters++;
            else if (!char.IsDigit(ch)) return false;
        }
        if (letters == 0 || s.Length > 16) return false;
        name = s;
        return true;
    }

    /// <summary>Tutorial characters are called <c>$</c> + random characters until named.</summary>
    public static bool IsPlaceholderName(ReadOnlySpan<byte> field) => field.Length > 1 && field[0] == (byte)'$';

    public static string Hex(ReadOnlySpan<byte> data, int max = 256)
    {
        var len = Math.Min(data.Length, max);
        var sb = new StringBuilder(len * 3);
        for (var i = 0; i < len; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(data[i].ToString("x2"));
        }
        if (data.Length > max) sb.Append(" …");
        return sb.ToString();
    }

    public static byte[] FromHex(string hex)
    {
        hex = hex.Replace(" ", "").Replace("\n", "").Replace("\r", "");
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++) bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }
}

