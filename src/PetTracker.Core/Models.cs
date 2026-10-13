using System.Text.Json;

namespace PetTracker.Core;

public enum DataQuality { WaitingForSnapshot, Exact, Uncertain }
public sealed record CharacterScope(string Character, string Server)
{
    public string Key => JsonSerializer.Serialize(new[] { Server, Character });
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Character) || string.IsNullOrWhiteSpace(Server) || Character.Length > 128 || Server.Length > 128)
            throw new ArgumentException("Karakter ve sunucu boş olamaz; en fazla 128 karakter olabilir.");
    }
}
public sealed record CollectionEntry(int PetId, int Level, int WithinLevel);
public sealed record AuthoritativeCollectionSnapshot(CharacterScope Scope, string StreamId, long FrameId, List<CollectionEntry> Entries, bool OmittedEntriesAreZero = false);
public sealed record SoulEvent(CharacterScope Scope, string StreamId, long FrameId, int PetId, int Amount);
public sealed class UIPreferences
{
    public HashSet<int> Pinned { get; set; } = [];
    public HashSet<int> Dismissed { get; set; } = [];
}
public sealed class ProfileState
{
    public CharacterScope Scope { get; set; } = new("", "");
    public AuthoritativeCollectionSnapshot? Snapshot { get; set; }
    public Dictionary<int, long> LastKnownTotals { get; set; } = [];
    public UIPreferences Preferences { get; set; } = new();
}
public sealed class SavedState
{
    public int Version { get; set; } = 1;
    public Dictionary<string, ProfileState> Profiles { get; set; } = [];
}
public sealed class SessionActivity
{
    public HashSet<int> Active { get; } = [];
    public HashSet<int> Attacked { get; } = [];
    public Dictionary<int, long> Souls { get; } = [];
    public List<SoulEvent> Events { get; } = [];
}
public sealed record Progress(int Level, long WithinLevel, int Goal, long Remaining)
{
    public static Progress FromTotal(long total)
    {
        if (total < 0) throw new ArgumentOutOfRangeException(nameof(total));
        return total switch
        {
            < 5 => new(0, total, 5, 5 - total),
            < 30 => new(1, total - 5, 25, 30 - total),
            < 105 => new(2, total - 30, 75, 105 - total),
            _ => new(3, 0, 0, 0)
        };
    }
    public static long Total(CollectionEntry entry)
    {
        var goal = entry.Level switch { 0 => 5, 1 => 25, 2 => 75, 3 => 1, _ => 0 };
        if (entry.PetId <= 0 || entry.WithinLevel < 0 || entry.WithinLevel >= goal)
            throw new ArgumentException($"Geçersiz collection kaydı: Pet ID {entry.PetId}.");
        return (entry.Level switch { 0 => 0, 1 => 5, 2 => 30, _ => 105 }) + entry.WithinLevel;
    }
}
public sealed record PetDropper(int NpcCode, string Name, int Level);
public sealed record PetDefinition(int Id, string Name, string Genus, List<PetDropper>? Droppers = null)
{
    public override string ToString() => $"{Id} · {Name}";
}
public sealed class PetCatalog
{
    public IReadOnlyDictionary<int, PetDefinition> Pets { get; }
    public IReadOnlyDictionary<int, int> PetByNpc { get; }
    public PetCatalog(IEnumerable<PetDefinition> pets)
    {
        Pets = pets.ToDictionary(p => p.Id);
        PetByNpc = Pets.Values.SelectMany(p => (p.Droppers ?? []).Select(d => (d.NpcCode, PetId: p.Id)))
            .Where(pair => pair.NpcCode > 0).Distinct().GroupBy(pair => pair.NpcCode).Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single().PetId);
        if (Pets.Values.Any(p => p.Id <= 0 || string.IsNullOrWhiteSpace(p.Name)))
            throw new InvalidDataException("Pet kataloğu geçersiz.");
    }
    public static PetCatalog Load(string path) => new(JsonSerializer.Deserialize<List<PetDefinition>>(File.ReadAllText(path))
        ?? throw new InvalidDataException("Pet kataloğu boş."));
    public string Name(int id) => Pets.TryGetValue(id, out var pet) ? pet.Name : $"Bilinmeyen pet #{id}";
}
