using PetTracker.Core;
namespace PetTracker.Tests;
public sealed class PetMonsterNameTests
{
    [Fact] public void CatalogProvidesNpcNamesEvenWithoutMapCoordinates()
    {
        var names=new PetMonsterNames();names.MergeCatalog(new([new(1234,"Collection name","Fera",[new(2400000,"Actual monster",30)])]));
        Assert.Equal("(Actual monster)",names.Caption(1234,2400000));Assert.Equal("Actual monster",names.Details(1234));
        Assert.Equal("",names.Caption(4321,2400000));
    }
    [Fact] public void RegionalMonsterNamesKeepBothAlternativesAndNumericPetIdentity()
    {
        var names=PetMonsterNames.Load(Path.Combine(AppContext.BaseDirectory,"data","maps","spawns.json"));
        Assert.Equal("",names.Caption(1125));
        Assert.Contains("Golden Zaif · Altgard",names.Details(1125));Assert.Contains("Yellow-Maned Zaif · Verteron",names.Details(1125));
        Assert.Contains("Canyon Airon",names.Details(1038));Assert.DoesNotContain("Zaif",names.Details(1038));
        Assert.Equal("",names.Caption(int.MaxValue));Assert.Equal("",names.Details(int.MaxValue));
        Assert.Equal("(Golden Zaif)",names.Caption(1125,2400578));
        Assert.Equal("",names.Caption(1125,999999));
        Assert.DoesNotContain("Golden Zaif",names.Caption(1038,2400578));
    }
    [Fact] public void LongerSourceListsAreSummarizedWithoutLosingTooltipDetails()
    {
        var names=PetMonsterNames.Load(Path.Combine(AppContext.BaseDirectory,"data","maps","spawns.json"));
        Assert.Equal("",names.Caption(1038));Assert.Equal(6,names.Details(1038).Split('\n').Length);
    }
}
