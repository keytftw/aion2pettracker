using System.Text.Json;
namespace PetTracker.Core;
public sealed class PetMonsterNames
{
    private readonly Dictionary<int,(string Caption,string Details)> entries=[];
    private readonly Dictionary<(int Pet,int Npc),string> npcNames=[];
    public string Caption(int petId,int observedNpc=0)=>npcNames.TryGetValue((petId,observedNpc),out var name)?"("+name+")":"";
    public string Details(int petId)=>entries.TryGetValue(petId,out var item)?item.Details:"";
    public void MergeCatalog(PetCatalog catalog)
    {
        foreach(var pet in catalog.Pets.Values)
        {
            var sources=(pet.Droppers??[]).Where(d=>d.NpcCode>0&&!string.IsNullOrWhiteSpace(d.Name)).ToArray();
            foreach(var npc in sources)npcNames.TryAdd((pet.Id,npc.NpcCode),npc.Name);
            var names=sources.Select(d=>d.Name).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
            if(names.Length>0)entries.TryAdd(pet.Id,("("+(names.Length<=2?string.Join(" / ",names):names[0]+" +"+(names.Length-1))+")",string.Join("\n",names)));
        }
    }
    public static PetMonsterNames Load(string path)=>Parse(File.ReadAllText(path));
    public static PetMonsterNames Parse(string json)
    {
        using var document=JsonDocument.Parse(json);var result=new PetMonsterNames();
        var worlds=document.RootElement.GetProperty("worlds").EnumerateObject().ToDictionary(p=>p.Name,p=>p.Value.GetProperty("name").GetString()??p.Name);
        foreach(var pet in document.RootElement.GetProperty("pets").EnumerateObject())
        {
            if(!int.TryParse(pet.Name,out var id)||id<=0)continue;
            foreach(var npc in pet.Value.EnumerateArray())
                if(npc.TryGetProperty("npc",out var npcId)&&npc.GetProperty("name").GetString() is {Length:>0} npcName)result.npcNames[(id,npcId.GetInt32())]=npcName;
            var sources=pet.Value.EnumerateArray().Select(npc=>(Name:npc.GetProperty("name").GetString()??"",Worlds:npc.GetProperty("worlds").EnumerateArray().Select(w=>worlds.GetValueOrDefault(w.GetProperty("world").ToString(),w.GetProperty("world").ToString())).Distinct().ToArray())).Where(n=>!string.IsNullOrWhiteSpace(n.Name)).ToArray();
            var names=sources.Select(n=>n.Name).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
            if(names.Length==0)continue;
            var caption=names.Length<=2?string.Join(" / ",names):names[0]+" +"+(names.Length-1);
            result.entries[id]=("("+caption+")",string.Join("\n",sources.Select(n=>n.Name+" · "+string.Join(", ",n.Worlds)).Distinct()));
        }
        return result;
    }
}
