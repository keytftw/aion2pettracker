using System.Globalization;
using System.Text.Json;

namespace PetTracker.Core;

public readonly record struct MapPoint(double X,double Y);
public sealed record MapWorld(int Id,string Name,string ImageKey,double X0,double Y0,int Columns,int Rows,double SectorSize)
{
    // Source's documented world bounds and inverted Y convention; not geographic latitude/longitude.
    public MapPoint Project(double x,double y)=>new((x-X0)/(Columns*SectorSize),1+(Y0-y)/(Rows*SectorSize));
}
public sealed record MapLocation(int PetId,int WorldId,int NpcId,string NpcName,int Level,string? Area,MapPoint Point);
public sealed class MapData
{
    public DateOnly SnapshotDate { get; }
    public IReadOnlyDictionary<int,MapWorld> Worlds { get; }
    private readonly Dictionary<int,MapLocation[]> locations;
    public int IgnoredPoints { get; }
    private MapData(DateOnly date,Dictionary<int,MapWorld> worlds,Dictionary<int,MapLocation[]> locations,int ignored)
    {SnapshotDate=date;Worlds=worlds;this.locations=locations;IgnoredPoints=ignored;}
    public IReadOnlyList<MapLocation> Locations(int petId,int worldId)=>locations.GetValueOrDefault(petId,[]).Where(point=>point.WorldId==worldId).ToArray();
    public IReadOnlyList<int> WorldsForPet(int petId)=>locations.GetValueOrDefault(petId,[]).Select(point=>point.WorldId).Distinct().ToArray();
    public int? BestWorld(int petId,int preferred)
    {
        var groups=locations.GetValueOrDefault(petId,[]).GroupBy(point=>point.WorldId).ToArray();
        return groups.Any(group=>group.Key==preferred)?preferred:groups.OrderByDescending(group=>group.Count()).ThenBy(group=>group.Key).Select(group=>(int?)group.Key).FirstOrDefault();
    }
    public static MapData Load(string path)
    {
        try { return Parse(File.ReadAllText(path)); }
        catch(Exception ex) when(ex is JsonException or FormatException or InvalidOperationException or KeyNotFoundException or OverflowException)
        {throw new InvalidDataException("Map data is invalid.",ex);}
    }
    public static MapData Parse(string json)
    {
        using var document=JsonDocument.Parse(json);var root=document.RootElement;
        if(!DateOnly.TryParseExact(root.GetProperty("fetched").GetString(),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date))throw new InvalidDataException("Invalid map snapshot date.");
        var worlds=new Dictionary<int,MapWorld>();
        foreach(var entry in root.GetProperty("worlds").EnumerateObject())
        {
            var id=int.Parse(entry.Name,CultureInfo.InvariantCulture);var value=entry.Value;
            var key=value.GetProperty("key").GetString()!;var name=value.GetProperty("name").GetString()!;
            var sectors=value.GetProperty("sectors").EnumerateArray().Select(s=>s.GetDouble()).ToArray();
            if(id<=0||worlds.Count>=16||string.IsNullOrWhiteSpace(name)||string.IsNullOrEmpty(key)||key.Length>80||key.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c!='_')||
                sectors.Length!=5||sectors.Any(s=>!double.IsFinite(s))||sectors[2]<1||sectors[2]>64||sectors[3]<1||sectors[3]>64||sectors[2]!=(int)sectors[2]||sectors[3]!=(int)sectors[3]||sectors[4]<=0||sectors[4]>10000000)
                throw new InvalidDataException("Map bounds or image key is invalid.");
            if(!worlds.TryAdd(id,new(id,name,key,sectors[0],sectors[1],(int)sectors[2],(int)sectors[3],sectors[4])))throw new InvalidDataException("Duplicate map ID.");
        }
        if(worlds.Count==0)throw new InvalidDataException("No map worlds.");
        var locations=new Dictionary<int,MapLocation[]>();var ignored=0;var count=0;
        foreach(var entry in root.GetProperty("pets").EnumerateObject())
        {
            var petId=int.Parse(entry.Name,CultureInfo.InvariantCulture);if(petId<=0||locations.Count>=512)throw new InvalidDataException("Invalid pet ID.");
            var points=new List<MapLocation>();
            foreach(var npc in entry.Value.EnumerateArray())
            {
                var npcId=npc.GetProperty("npc").GetInt32();var name=npc.GetProperty("name").GetString()!;var level=npc.GetProperty("level").GetInt32();
                if(npcId<=0||string.IsNullOrWhiteSpace(name)||level<0||level>200)throw new InvalidDataException("Invalid map creature.");
                foreach(var worldEntry in npc.GetProperty("worlds").EnumerateArray())
                {
                    var worldId=worldEntry.GetProperty("world").GetInt32();var area=worldEntry.GetProperty("area").GetString();
                    foreach(var raw in worldEntry.GetProperty("points").EnumerateArray())
                    {
                        if(++count>100000)throw new InvalidDataException("Too many map points.");
                        var xy=raw.EnumerateArray().Select(value=>value.GetDouble()).ToArray();
                        if(xy.Length!=2||xy.Any(value=>!double.IsFinite(value)))throw new InvalidDataException("Invalid map coordinate.");
                        if(!worlds.TryGetValue(worldId,out var world)){ignored++;continue;}
                        var point=world.Project(xy[0],xy[1]);
                        if(point.X<0||point.X>1||point.Y<0||point.Y>1){ignored++;continue;}
                        points.Add(new(petId,worldId,npcId,name,level,area,point));
                    }
                }
            }
            if(!locations.TryAdd(petId,points.ToArray()))throw new InvalidDataException("Duplicate map Pet ID.");
        }
        return new(date,worlds,locations,ignored);
    }
}
