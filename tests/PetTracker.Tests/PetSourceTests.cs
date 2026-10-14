using PetTracker.Core;
namespace PetTracker.Tests;
public sealed class PetSourceTests
{
    [Fact] public void BundledAcquisitionDataCoversEveryCatalogIdWithoutGuessingTheMissingWikiPet()
    {
        var folder=Path.Combine(AppContext.BaseDirectory,"data");var catalog=PetCatalog.Load(Path.Combine(folder,"pet-catalog.json"));
        var sources=PetSources.Load(Path.Combine(folder,"pet-sources.json"));
        Assert.All(catalog.Pets.Keys,id=>Assert.NotNull(sources.Get(id)));
        Assert.Equal(206,catalog.Pets.Keys.Count(id=>sources.Get(id)!.Verified));
        Assert.False(sources.Get(1299)!.Verified);
        Assert.Contains(sources.Get(1204)!.Facts!,fact=>fact.Contains("Shop"));
        Assert.Contains(sources.Get(1001)!.Facts!,fact=>fact.Contains("Founder's Pack"));
    }
    [Fact] public void MissingCoordinatesCannotBeUsedAsEvidenceThatAMonsterCannotBeFarmed()
    {
        Assert.True(new PetSourceInfo(1,true,"https://wikily.gg/aion-2/pets/test","2026-10-14",["Boss · Kill reward"]).HasMonsterSource);
        Assert.False(new PetSourceInfo(2,false,null,null,[]).HasMonsterSource);
        Assert.False(new PetSourceInfo(3,true,"https://wikily.gg/aion-2/pets/test","2026-10-14",["Shop"]).HasMonsterSource);
    }
}
