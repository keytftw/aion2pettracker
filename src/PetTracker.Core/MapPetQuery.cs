namespace PetTracker.Core;

public static class MapPetQuery
{
    public static PetDefinition[] Filter(PetCatalog catalog,MapData data,int worldId,string query,bool maxed,bool hideUnlocated,Func<int,long?> total)
        =>PetPickerQuery.Filter(catalog,query,maxed,total)
            .Where(pet=>data.Locations(pet.Id,worldId).Count>0||(!hideUnlocated&&data.WorldsForPet(pet.Id).Count==0))
            .OrderBy(pet=>data.WorldsForPet(pet.Id).Count==0).ThenBy(pet=>pet.Name).ToArray();

    public static MapLocation[] FarmableLocations(PetCatalog catalog,MapData data,int worldId,Func<int,long?> total)
        =>catalog.Pets.Values.Where(pet=>total(pet.Id) is not >=105)
            .SelectMany(pet=>data.Locations(pet.Id,worldId)).ToArray();
}
