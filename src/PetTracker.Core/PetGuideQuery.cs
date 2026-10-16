namespace PetTracker.Core;

// These are recorded farm regions, not proof that a pet cannot be obtained from a chest elsewhere.
public static class PetGuideQuery
{
    public static bool IsExclusive(MapData data,int id,int world,PetSourceInfo? source=null)
    {
        var worlds=data.WorldsForPet(id);
        if(world<=0||worlds.Count!=1||worlds[0]!=world)return false;
        // A source can name a second region even when its coordinates have not been imported.
        return source?.Verified!=true||!(source.Facts??[]).Where(f=>f.Contains("Monster drop",StringComparison.OrdinalIgnoreCase))
            .Any(f=>data.Worlds.Values.Any(w=>w.Id!=world&&f.Split(',').Any(part=>string.Equals(part.Trim(),w.Name,StringComparison.OrdinalIgnoreCase))));
    }
    public static bool IsSpecial(MapData data,PetDefinition pet,PetSourceInfo? source)
        =>data.WorldsForPet(pet.Id).Count==0||source?.Kind is "worldBoss" or "dungeonBoss";

    public static string Kind(MapData data,PetDefinition pet,PetSourceInfo? source)
        =>source?.Kind??(pet.Genus=="Special"?"shop":data.WorldsForPet(pet.Id).Count==0?"other":"field");

    public static PetDefinition[] Filter(PetCatalog catalog,MapData data,PetSources sources,int world,bool maxed,string genus,bool special,string kind,string query,Func<int,long?> total)
        =>PetPickerQuery.Filter(catalog,query,maxed,total)
            .Where(p=>string.IsNullOrEmpty(genus)||p.Genus==genus)
            .Where(p=>special?IsSpecial(data,p,sources.Get(p.Id)):world>0?IsExclusive(data,p.Id,world,sources.Get(p.Id)):data.WorldsForPet(p.Id).Count==1&&IsExclusive(data,p.Id,data.WorldsForPet(p.Id)[0],sources.Get(p.Id)))
            .Where(p=>!special||world<0||data.WorldsForPet(p.Id).Contains(world)||sources.Get(p.Id)?.ContextWorld==world)
            .Where(p=>!special||string.IsNullOrEmpty(kind)||Kind(data,p,sources.Get(p.Id))==kind).ToArray();

    public static (string Key,string Detail) Route(MapData data,PetDefinition pet,PetSourceInfo? source,int world)
    {
        if(data.WorldsForPet(pet.Id).Count>0)
        {
            var target=data.BestWorld(pet.Id,world)!.Value;
            return ("routeField",string.Join(" / ",data.Locations(pet.Id,target).Select(p=>p.NpcName).Distinct().Take(2)));
        }
        if(source?.Verified!=true)return ("routeUnknown","");
        var facts=source.Facts??[];
        string? Find(string text)=>facts.FirstOrDefault(f=>f.Contains(text,StringComparison.OrdinalIgnoreCase));
        if(Find("Episode quest") is {} quest)return ("routeQuest",quest.Split('·')[0].Trim());
        if(Find("Special and Pets tabs") is {} merchant)return ("routeMerchant",merchant.Split('·')[0].Trim());
        if(Find("Main menu Shop") is {} shop)return ("routeShop",shop);
        if(Find("Season 1 tab") is {} pass)return ("routeShop",pass);
        if(Find("Pets tab") is {} festival)return ("routeShop",festival);
        if(Find("Ascension Trial, Easy") is {} trial)return ("routeTrial",trial.Split('·')[0].Trim());
        if(Find("Opened from this chest") is {} chest)return ("routeChest",chest.Split('·')[0].Trim());
        return ("routeUnknown","");
    }
}
