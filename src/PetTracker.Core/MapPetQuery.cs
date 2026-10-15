namespace PetTracker.Core;

public static class MapPetQuery
{
    public static PetDefinition[] Filter(PetCatalog catalog,MapData data,int worldId,string query,bool maxed,bool hideUnlocated,Func<int,long?> total,string genus="",IReadOnlySet<int>? trackedIds=null)
        =>PetPickerQuery.Filter(catalog,query,maxed,total)
            .Where(pet=>trackedIds is null||trackedIds.Contains(pet.Id))
            .Where(pet=>string.IsNullOrEmpty(genus)||string.Equals(pet.Genus,genus,StringComparison.OrdinalIgnoreCase))
            .Where(pet=>worldId<0?(!hideUnlocated||data.WorldsForPet(pet.Id).Count>0):worldId==0?data.WorldsForPet(pet.Id).Count==0:data.Locations(pet.Id,worldId).Count>0)
            .OrderBy(pet=>data.WorldsForPet(pet.Id).Count==0).ThenBy(pet=>pet.Name).ToArray();

    public static MapLocation[] FarmableLocations(PetCatalog catalog,MapData data,int worldId,Func<int,long?> total,string genus="")
        =>catalog.Pets.Values.Where(pet=>total(pet.Id) is not >=105)
            .Where(pet=>string.IsNullOrEmpty(genus)||string.Equals(pet.Genus,genus,StringComparison.OrdinalIgnoreCase))
            .SelectMany(pet=>data.Locations(pet.Id,worldId)).ToArray();

    public static MapLocation[] TrackedLocations(MapData data,int worldId,IEnumerable<int> trackedIds)
        =>trackedIds.Distinct().SelectMany(id=>data.Locations(id,worldId)).ToArray();
}
