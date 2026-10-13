namespace PetTracker.Core;

public static class PetPickerQuery
{
    public static PetDefinition[] Filter(PetCatalog catalog,string query,bool maxed,Func<int,long?> knownTotal)
    {
        var text=query.Trim();
        return catalog.Pets.Values.Where(p=>(knownTotal(p.Id) is >=105)==maxed)
            .Where(p=>p.Name.Contains(text,StringComparison.OrdinalIgnoreCase)||p.Id.ToString().Contains(text))
            .OrderBy(p=>p.Name).ToArray();
    }
}
