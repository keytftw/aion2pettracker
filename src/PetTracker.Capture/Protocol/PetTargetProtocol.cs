// SPDX-License-Identifier: GPL-3.0-only
// Narrow spawn/cast/hit identity subset adapted from reference PacketParser.cs. No damage reader.
namespace PetTracker.Capture.Protocol;

public static class PetTargetProtocol
{
    public static LiveMessage? Decode(string stream, ushort opcode, ReadOnlySpan<byte> body)
    {
        var at = 0;
        if (!Wire.TryVarint(body, ref at, out var actor) || actor == 0 || (opcode != 0x4136 && actor > 9_999_999)) return null;
        if (opcode == 0x4236) return new EntityGone(stream, actor);
        if (opcode == 0x4136)
        {
            if (at + 2 > body.Length) return null;
            var kind = body[at];
            var normalized = actor > 1_000_000 ? (actor & 0x3FFF) | 0x4000 : actor;
            if (kind is 0x5F or 0x1C or 0x1F or 0x1D or 0x5D) return new EntityGone(stream, normalized);
            for (var i = at + 3; i < Math.Min(body.Length - 2, at + 60); i++)
            {
                if (body[i] != 0 || body[i + 1] is not (0 or 0x40) || body[i + 2] != 2) continue;
                var code = (int)Wire.U24(body, i - 3);
                return code > 0 ? new NpcObserved(stream, normalized, code) : null;
            }
            return new EntityGone(stream, normalized); // Do not retain a previous NPC mapping for a reused actor.
        }
        if (opcode == 0x0338)
        {
            if (!Wire.TryU8(body, ref at, out var zero) || zero != 0 || !Wire.TryVarint(body, ref at, out var target) || target == actor || target is < 1 or > 9_999_999 ||
                !Wire.TryU32(body, ref at, out var skill) || skill is < 1 or > 299_999_999) return null;
            return new TargetObserved(stream, actor, target);
        }
        if (opcode == 0x0238)
        {
            if (!Wire.TryVarint(body, ref at, out _) || !Wire.TryU32(body, ref at, out var skill) || skill is < 1 or > 299_999_999 || at + 2 > body.Length) return null;
            at += 2;
            if (!Wire.TryVarint(body, ref at, out var target) || target == actor || target is < 1 or > 9_999_999) return null;
            return new TargetObserved(stream, actor, target);
        }
        return null;
    }
}
