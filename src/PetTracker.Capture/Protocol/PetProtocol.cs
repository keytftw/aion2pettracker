// SPDX-License-Identifier: GPL-3.0-only
// Pet layouts adapted from reference PetPackets.cs; identity subset from PacketParser.cs.
// See THIRD_PARTY_NOTICES.md, data/PET-SOURCES.md and licenses/.
using PetTracker.Core;

namespace PetTracker.Capture.Protocol;

public static class PetProtocol
{
    public static LiveMessage? Decode(string stream, long frame, ushort opcode, ReadOnlySpan<byte> body)
    {
        switch (opcode)
        {
            case 0x0090:
                return ReadCollection(body, out var reason, out var trailingBytes) is { } entries ? new CollectionReceived(stream, frame, entries, trailingBytes, OmittedEntriesAreZero: true)
                    : new IntegrityLost($"Collection reddedildi ({body.Length} bayt): {reason}; sayaçlar korunuyor");
            case 0x0D90:
                // Record only shape/frame metadata, never the raw packet. A rejected
                // shape must remain uncertain until its meaning is verified.
                var soulShape=$"opcode=0x0D90, uzunluk={body.Length}, tür="+(body.IsEmpty?"yok":$"0x{body[0]:X2}")+$", frame={frame}";
                // The reported type-02, 17-byte variant may contain two entries.
                // Record bounded game-data candidates only, never raw bytes or
                // inferred gains. Its semantics still require live corroboration.
                if(body.Length==17&&body[0]==2)
                {
                    var firstId=Wire.U32(body,1);var firstAmount=Wire.U32(body,5);
                    var secondId=Wire.U32(body,9);var secondAmount=Wire.U32(body,13);
                    if(firstId is >=1000 and <=9999&&secondId is >=1000 and <=9999&&firstAmount is >0 and <1000&&secondAmount is >0 and <1000)
                        soulShape+=$", toplu soul adayı: Pet ID {firstId} +{firstAmount}; Pet ID {secondId} +{secondAmount} (doğrulanmadı; sayaca uygulanmadı)";
                    else soulShape+=", iki geçerli Pet ID/miktar çiftiyle eşleşmedi";
                }
                if (body.Length >= 9 && body[0] == 1)
                {
                    var id = Wire.U32(body, 1); var amount = Wire.U32(body, 5);
                    if (id > 0 && id <= int.MaxValue && amount is > 0 and < 1000)
                        return new SoulReceived(stream, frame, (int)id, (int)amount);
                    return new IntegrityLost($"Geçersiz soul: Pet ID {id}, miktar {amount} ({soulShape})",IdentityMayBeLost:false);
                }
                return new IntegrityLost($"Soul mesajı biçimi geçersiz/eksik ({soulShape}); yeni tam collection gerekiyor",IdentityMayBeLost:false);
            case 0x3336:
                return (LiveMessage?)ReadIdentity(stream, frame, body) ?? new IntegrityLost("Kendi karakter kimliği okunamadı; karakter eşlemesi durduruldu");
            case 0x4A36:
                var at = 0;
                if (Wire.TryVarint(body, ref at, out var actor) && actor is >= 100 and <= 9_999_999 && at < body.Length)
                    return new OwnActor(stream, actor);
                return new IntegrityLost("Kendi actor kimliği geçersiz");
            default:
                // Includes 0x1190 shards, player info, damage, attacks and kills: no soul increments.
                return opcode is 0x4136 or 0x4236 or 0x0238 or 0x0338 ? PetTargetProtocol.Decode(stream, opcode, body) : null;
        }
    }
    public static List<CollectionEntry>? ReadCollection(ReadOnlySpan<byte> body) => ReadCollection(body, out _);
    public static List<CollectionEntry>? ReadCollection(ReadOnlySpan<byte> body, out string error) => ReadCollection(body, out error, out _);
    public static List<CollectionEntry>? ReadCollection(ReadOnlySpan<byte> body, out string error, out int trailingBytes)
    {
        error = "";
        trailingBytes = 0;
        var at = 4;
        if (body.Length < 6 || !Wire.TryVarint(body, ref at, out var count)) return Reject(out error, "Level listesi başlığı eksik");
        if (count > 512 || body.Length - at < count * 12L) return Reject(out error, $"Level sayısı/boyutu geçersiz: {count}, offset {at}");
        var levels = new Dictionary<int, int>();
        for (var i = 0; i < count; i++)
        {
            var id = Wire.U32(body, at); var repeat = Wire.U32(body, at + 4); var level = Wire.U32(body, at + 8); at += 12;
            if (id == 0 || id > int.MaxValue || id != repeat || level > 3 || !levels.TryAdd((int)id, (int)level))
                return Reject(out error, $"Level kaydı {i}: ID {id}, tekrar {repeat}, level {level}");
        }
        if (!Wire.TryVarint(body, ref at, out var points)) return Reject(out error, $"Soul listesi başlığı eksik, offset {at}");
        if (points > 512 || body.Length - at < points * 8L) return Reject(out error, $"Soul sayısı/boyutu geçersiz: {points}, offset {at}");
        var souls = new Dictionary<int, int>();
        for (var i = 0; i < points; i++)
        {
            var id = Wire.U32(body, at); var amount = Wire.U32(body, at + 4); at += 8;
            if (id == 0 || id > int.MaxValue || amount > 74 || !souls.TryAdd((int)id, (int)amount))
                return Reject(out error, $"Soul kaydı {i}: ID {id}, miktar {amount}");
        }
        // The reference and observed Global packet append other fields after the declared pet lists.
        // Validate both pet lists completely; never interpret the remaining fields as pet progress.
        var result = new List<CollectionEntry>();
        foreach (var id in levels.Keys.Union(souls.Keys))
        {
            var entry = new CollectionEntry(id, levels.GetValueOrDefault(id), souls.GetValueOrDefault(id));
            try { _ = Progress.Total(entry); } catch (ArgumentException) { return Reject(out error, $"Aşama sınırı geçersiz: ID {id}, Lv{entry.Level}, soul {entry.WithinLevel}"); }
            result.Add(entry);
        }
        trailingBytes = body.Length - at;
        return result;
    }
    private static List<CollectionEntry>? Reject(out string error, string message) { error = message; return null; }
    private static OwnIdentity? ReadIdentity(string stream, long frame, ReadOnlySpan<byte> b)
    {
        var at = 0;
        if (!Wire.TryVarint(b, ref at, out var actor) || actor is < 1 or > 9_999_999) return null;
        at += 4;
        if (at + 2 > b.Length || (b[at] & 1) == 0) return null;
        var len = b[at + 1]; at += 2;
        if (len is < 1 or > 36 || at + len + 6 > b.Length || !Wire.TryName(b.Slice(at, len), out var name)) return null;
        at += len;
        var server = Wire.U16(b, at); var cls = Wire.U32(b, at + 2);
        if (server is < 1000 or >= 3000 || !(cls is >= 5 and <= 36 or >= 45 and <= 48)) return null;
        return new(stream, frame, actor, new(name, server.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }
}
