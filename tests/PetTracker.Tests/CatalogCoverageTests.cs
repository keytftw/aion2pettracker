using PetTracker.Core;

namespace PetTracker.Tests;

public sealed class CatalogCoverageTests
{
    private static PetCatalog Catalog() => PetCatalog.Load(Path.Combine(AppContext.BaseDirectory, "data", "pet-catalog.json"));
    public static IEnumerable<object[]> AllIds() => Catalog().Pets.Keys.Order().Select(id => new object[] { id });

    [Fact]
    public void ReferenceCatalogHas207UniqueIdsIncluding7SpecialWithoutClaimingGameMembership()
    {
        var catalog = Catalog();
        Assert.Equal(207, catalog.Pets.Count);
        Assert.Equal(7, catalog.Pets.Values.Count(p => p.Genus == "Special"));
        Assert.All(catalog.Pets.Keys, id => Assert.InRange(id, 1000, 9999));
    }
    [Theory]
    [MemberData(nameof(AllIds))]
    public void EveryCatalogIdUsesNumericIdentityAndAllLevelBoundaries(int id)
    {
        var catalog = Catalog();
        var cases = new (int Level, int Count, int Amount, int NextLevel, long NextCount, long Remaining)[]
        { (0,4,1,1,0,25), (1,24,1,2,0,75), (2,74,1,3,0,0), (2,46,1,2,47,28), (0,4,27,2,1,74), (0,0,110,3,0,0) };
        foreach (var c in cases)
        {
            var scope = new CharacterScope("Offline", "CatalogTest");
            var tracker = new Tracker(catalog, new(), _ => { }); tracker.Select(scope);
            tracker.Synchronize(new(scope, "s", 0, [new(id, c.Level, c.Count)], OmittedEntriesAreZero: true));
            Assert.Empty(tracker.VisibleIds);
            tracker.Gain(new(scope, "s", 1, id, c.Amount));
            var progress = Progress.FromTotal(tracker.KnownTotal(id)!.Value);
            Assert.Equal(c.NextLevel, progress.Level); Assert.Equal(c.NextCount, progress.WithinLevel); Assert.Equal(c.Remaining, progress.Remaining);
            Assert.Equal([id], tracker.VisibleIds); Assert.Equal(DataQuality.Exact, tracker.Quality);
            Assert.Equal(c.Amount, tracker.Session.Souls[id]); Assert.Single(tracker.Current.LastKnownTotals);
        }
    }
    [Fact]
    public void SameNamesCannotMergeDifferentPetIds()
    {
        var catalog = new PetCatalog([new(1002, "Same name", "Fera"), new(1003, "Same name", "Fera")]);
        var scope = new CharacterScope("Offline", "CatalogTest"); var tracker = new Tracker(catalog, new(), _ => { }); tracker.Select(scope);
        tracker.Synchronize(new(scope, "s", 0, [new(1002, 0, 1), new(1003, 1, 3)]));
        tracker.Gain(new(scope, "s", 1, 1002, 1));
        Assert.Equal(2L, tracker.KnownTotal(1002)); Assert.Equal(8L, tracker.KnownTotal(1003)); Assert.Equal([1002], tracker.VisibleIds);
    }
}
