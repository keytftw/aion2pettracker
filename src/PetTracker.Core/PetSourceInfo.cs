using System.Text.Json;
namespace PetTracker.Core;

public sealed record PetSourceInfo(int Id,bool Verified,string? Source,string? Checked,string[]? Facts,string? Kind=null,string? Context=null,string? ContextSource=null,int? ContextWorld=null)
{
    public bool HasMonsterSource=>Verified&&(Facts??[]).Any(f=>f.Contains("Monster drop",StringComparison.OrdinalIgnoreCase)||f.Contains("Kill reward",StringComparison.OrdinalIgnoreCase));
}
public sealed class PetSources
{
    private readonly IReadOnlyDictionary<int,PetSourceInfo> entries;
    private PetSources(IReadOnlyDictionary<int,PetSourceInfo> entries)=>this.entries=entries;
    public PetSourceInfo? Get(int id)=>entries.GetValueOrDefault(id);
    public static PetSources Load(string path)
    {
        var rows=JsonSerializer.Deserialize<PetSourceInfo[]>(File.ReadAllText(path))??throw new InvalidDataException("Missing pet acquisition data.");
        if(rows.Length>512||rows.Any(p=>p.Id<=0||p.Verified&&(p.Facts is null||p.Facts.Length==0||p.Source is null||!Uri.TryCreate(p.Source,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.Host!="wikily.gg")))throw new InvalidDataException("Invalid pet acquisition data.");
        if(rows.Any(p=>p.Kind is not (null or "worldBoss" or "dungeonBoss" or "shop" or "other")||p.Context is not null&&(!p.Verified||p.ContextSource is null||!Uri.TryCreate(p.ContextSource,UriKind.Absolute,out var url)||url.Scheme!="https"||url.Host!="wikily.gg")))throw new InvalidDataException("Invalid pet guide context.");
        return new(rows.ToDictionary(p=>p.Id));
    }
}
