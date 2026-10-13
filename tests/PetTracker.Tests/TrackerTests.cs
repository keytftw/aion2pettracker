using PetTracker.Core;

namespace PetTracker.Tests;

public sealed class TrackerTests
{
    private static readonly CharacterScope Scope = new("Karakter", "Global-1");
    private static PetCatalog Catalog() => new([new(1002, "Pet", "Fera"), new(1003, "Pet", "Fera")]);
    private static Tracker Create(SavedState? saved = null, Action<string>? log = null)
    {
        var t = new Tracker(Catalog(), saved ?? new(), log ?? (_ => { }));
        t.Select(Scope); return t;
    }
    private static void Sync(Tracker t, int level = 0, int count = 0, long frame = 0) =>
        t.Synchronize(new(t.Current.Scope, "stream", frame, [new(1002, level, count)]));
    private static void Gain(Tracker t, long frame, int amount = 1, int id = 1002, string stream = "stream") =>
        t.Gain(new(t.Current.Scope, stream, frame, id, amount));

    [Theory]
    [InlineData(0, 4, 1, 1, 0, 25)]
    [InlineData(1, 24, 1, 2, 0, 75)]
    [InlineData(2, 74, 1, 3, 0, 0)]
    [InlineData(2, 46, 1, 2, 47, 28)]
    [InlineData(0, 4, 27, 2, 1, 74)]
    [InlineData(0, 0, 110, 3, 0, 0)]
    public void LevelBoundaries(int level, int count, int amount, int expectedLevel, long expectedCount, long remaining)
    {
        var t = Create(); Sync(t, level, count); Gain(t, 1, amount);
        var p = Progress.FromTotal(t.KnownTotal(1002)!.Value);
        Assert.Equal(expectedLevel, p.Level); Assert.Equal(expectedCount, p.WithinLevel); Assert.Equal(remaining, p.Remaining);
    }
    [Fact]
    public void IdenticalRealGainsCountTwiceButSameFrameOnce()
    {
        var t = Create(); Sync(t); Gain(t, 1); Gain(t, 2); Gain(t, 2);
        Assert.Equal(2L, t.KnownTotal(1002)); Assert.Equal(2L, t.Session.Souls[1002]); Assert.Equal(2, t.Session.Events.Count);
    }
    [Fact]
    public void ReconciliationKeepsPreferencesAndDoesNotActivateCollectionRows()
    {
        var t = Create(); t.Pin(1002, true); t.Dismiss(1003); Sync(t, 2, 46);
        Assert.Equal(76L, t.KnownTotal(1002)); Assert.Equal(DataQuality.Exact, t.Quality);
        Assert.Equal([1002], t.VisibleIds); Assert.Contains(1003, t.Current.Preferences.Dismissed);
        Gain(t, 1); Sync(t, 1, 0, 2);
        Assert.Equal(5L, t.KnownTotal(1002)); Assert.Equal(1, t.Current.Snapshot!.Entries.Single().Level);
        Assert.Contains(1002, t.Current.Preferences.Pinned);
    }
    [Fact]
    public void DismissDoesNotChangeProgressAndNextGainReactivates()
    {
        var t = Create(); Sync(t); Gain(t, 1); t.Pin(1002, true); t.Dismiss(1002);
        Assert.Contains(1002, t.VisibleIds); t.Pin(1002, false); Assert.Empty(t.VisibleIds);
        Assert.Equal(1L, t.KnownTotal(1002)); Gain(t, 2); Assert.Contains(1002, t.VisibleIds);
        Assert.DoesNotContain(1002, t.Current.Preferences.Dismissed); Assert.Equal(2L, t.KnownTotal(1002));
    }
    [Fact]
    public void GapRetainsCountersAndRecordsGainsUntilSnapshot()
    {
        var t = Create(); Sync(t, 2, 46); t.Gap("missing"); Gain(t, 1);
        Assert.Equal(DataQuality.Uncertain, t.Quality); Assert.Equal(76L, t.KnownTotal(1002));
        Assert.Single(t.Session.Events); Assert.Equal(1L, t.Session.Souls[1002]);
        Sync(t, 2, 48, 2); Assert.Equal(78L, t.KnownTotal(1002)); Assert.Equal(DataQuality.Exact, t.Quality);
    }
    [Fact]
    public void UnknownIdIsLoggedWithoutInventingBaseline()
    {
        var messages = new List<string>(); var t = Create(log: messages.Add); Sync(t); Gain(t, 1, id: 99999);
        Assert.Null(t.KnownTotal(99999)); Assert.Equal(DataQuality.Uncertain, t.Quality);
        Assert.Contains(99999, t.VisibleIds); Assert.Contains(messages, m => m.Contains("Bilinmeyen Pet ID: 99999"));
        Assert.Equal("Bilinmeyen pet #99999", Catalog().Name(99999));
    }
    [Fact]
    public void WaitingGainsAreRetainedAndSnapshotDoesNotDoubleApplyThem()
    {
        var t = Create(); Assert.Equal(DataQuality.WaitingForSnapshot, t.Quality); Gain(t, 1);
        Assert.Null(t.KnownTotal(1002)); Sync(t, 0, 4, 2);
        Assert.Equal(4L, t.KnownTotal(1002)); Assert.Single(t.Session.Events);
    }
    [Fact]
    public void CharacterAndServerAreIsolated()
    {
        var t = Create(); Sync(t, 1, 10); t.Pin(1002, true);
        t.Select(new("Karakter", "Global-2")); Assert.Null(t.KnownTotal(1002)); Assert.Empty(t.VisibleIds);
        Sync(t, 2, 20); t.Select(new("Başka", "Global-1")); Assert.Null(t.KnownTotal(1002));
        t.Select(Scope); Assert.Equal(15L, t.KnownTotal(1002)); Assert.Contains(1002, t.VisibleIds);
        Assert.Equal(DataQuality.Uncertain, t.Quality); Assert.Empty(t.Session.Events);
    }
    [Fact]
    public void ReconnectAndNewStreamInvalidateExactness()
    {
        var t = Create(); Sync(t); Gain(t, 1); t.Reconnect(); Gain(t, 0, stream: "new");
        Assert.Equal(1L, t.KnownTotal(1002)); Assert.Equal(2L, t.Session.Souls[1002]); Assert.Equal(DataQuality.Uncertain, t.Quality);
        t.Synchronize(new(Scope, "new", 1, [new(1002, 0, 3)])); Gain(t, 2, stream: "new");
        Assert.Equal(4L, t.KnownTotal(1002));
    }
    [Fact]
    public void InvalidSnapshotIsAtomicAndStaleSnapshotIgnored()
    {
        var t = Create(); Sync(t, 0, 4);
        Assert.Throws<ArgumentException>(() => t.Synchronize(new(Scope, "stream", 1, [new(1002, 0, 2), new(1003, 1, 25)])));
        Assert.Equal(4L, t.KnownTotal(1002));
        Assert.Throws<ArgumentException>(() => t.Synchronize(new(Scope, "stream", 1, [new(1002, 0, 1), new(1002, 0, 2)])));
        Gain(t, 2); Sync(t, 0, 0, 1); Assert.Equal(5L, t.KnownTotal(1002));
        Assert.Throws<ArgumentException>(() => t.Synchronize(new(new("Other", "Server"), "x", 0, [])));
    }
    [Fact]
    public void SnapshotRemainsAuthoritativeBaselineWhileCounterAdvances()
    {
        var t = Create(); Sync(t, 2, 46); Gain(t, 1);
        Assert.Equal(46, t.Current.Snapshot!.Entries.Single().WithinLevel); Assert.Equal(77L, t.KnownTotal(1002));
    }
    [Fact]
    public void KnownPetOmittedFromValidatedSparseCollectionCountsTwoRealGains()
    {
        var t = Create();
        t.Synchronize(new(Scope, "stream", 0, [new(1002, 0, 4)], OmittedEntriesAreZero: true));
        Assert.Equal(0L, t.KnownTotal(1003)); Assert.Single(t.Current.LastKnownTotals);
        Gain(t, 1, id: 1003); Gain(t, 2, id: 1003);
        Assert.Equal(2L, t.KnownTotal(1003)); Assert.Equal(DataQuality.Exact, t.Quality);
        Assert.Equal(new Progress(0, 2, 5, 3), Progress.FromTotal(t.KnownTotal(1003)!.Value));
        Assert.Equal(2L, t.Session.Souls[1003]); Assert.Contains(1003, t.VisibleIds);
        Gain(t, 2, id: 1003); Assert.Equal(2L, t.KnownTotal(1003));
    }
    [Fact]
    public void GenericSnapshotDoesNotInventZeroForMissingKnownPet()
    {
        var t = Create(); Sync(t); Gain(t, 1, id: 1003);
        Assert.Null(t.KnownTotal(1003)); Assert.Equal(DataQuality.Uncertain, t.Quality);
    }
    [Fact]
    public void SparseSnapshotNeverInitializesUnknownIdOrMissingPetAfterGap()
    {
        var t = Create(); t.Synchronize(new(Scope, "stream", 0, [], OmittedEntriesAreZero: true));
        Gain(t, 1, id: 99999); Assert.Null(t.KnownTotal(99999)); Assert.Equal(DataQuality.Uncertain, t.Quality);
        t.Synchronize(new(Scope, "stream", 2, [], OmittedEntriesAreZero: true));
        t.Gap("loss"); Gain(t, 3, id: 1003);
        Assert.Equal(0L, t.KnownTotal(1003)); Assert.DoesNotContain(1003, t.Current.LastKnownTotals.Keys);
        Assert.Equal(DataQuality.Uncertain, t.Quality); Assert.Equal(1L, t.Session.Souls[1003]);
    }
    [Fact]
    public void SparseSnapshotCannotSupplyZeroAfterReconnectOrFromPersistedCache()
    {
        var t = Create(); t.Synchronize(new(Scope, "stream", 0, [], OmittedEntriesAreZero: true));
        t.Reconnect(); Gain(t, 0, id: 1003, stream: "new"); Assert.Equal(0L, t.KnownTotal(1003));
        Assert.DoesNotContain(1003, t.Current.LastKnownTotals.Keys);
        var loaded = Create(t.Saved); Gain(loaded, 1, id: 1003);
        Assert.Equal(0L, loaded.KnownTotal(1003)); Assert.DoesNotContain(1003, loaded.Current.LastKnownTotals.Keys);
        Assert.Equal(DataQuality.Uncertain, loaded.Quality);
    }
    [Fact]
    public void PinnedZaifUsesSparseZeroBaselineWithoutWritingCountersOrSoulEvents()
    {
        var t = new Tracker(new([new(1124, "Zaif", "Fera")]), new(), _ => { }); t.Select(Scope);
        t.Pin(1124, true); Assert.Null(t.KnownTotal(1124));
        t.Synchronize(new(Scope, "stream", 0, [], OmittedEntriesAreZero: true));
        Assert.Equal(new Progress(0, 0, 5, 5), Progress.FromTotal(t.KnownTotal(1124)!.Value));
        Assert.Contains(1124, t.VisibleIds); Assert.Empty(t.Current.LastKnownTotals); Assert.Empty(t.Session.Events);
        Assert.Equal(DataQuality.Exact, t.Quality);
        t.Pin(1124, false); Assert.Empty(t.VisibleIds); Assert.Equal(0L, t.KnownTotal(1124));
    }
    [Fact]
    public void PinnedZeroBaselineBecomesLastKnownOnGapAndReconcilesOnNextSnapshot()
    {
        var t = Create(); t.Synchronize(new(Scope, "stream", 0, [], OmittedEntriesAreZero: true)); t.Pin(1003, true);
        t.Gap("loss"); Assert.Equal(0L, t.KnownTotal(1003)); Assert.Equal(DataQuality.Uncertain, t.Quality);
        t.Synchronize(new(Scope, "stream", 1, [new(1003, 2, 13)], OmittedEntriesAreZero: true));
        Assert.Equal(43L, t.KnownTotal(1003)); Assert.Equal(DataQuality.Exact, t.Quality); Assert.Contains(1003, t.VisibleIds);
    }
    [Fact]
    public void UnobservedSpecialAndUnknownCatalogIdsRemainUnknownWhenPinned()
    {
        var t = new Tracker(new([new(1001, "Special pet", "Special")]), new(), _ => { }); t.Select(Scope);
        t.Synchronize(new(Scope, "stream", 0, [], OmittedEntriesAreZero: true));
        t.Pin(1001, true); t.Pin(99999, true);
        Assert.Null(t.KnownTotal(1001)); Assert.Null(t.KnownTotal(99999)); Assert.Empty(t.Current.LastKnownTotals);
    }
    [Fact]
    public void GainForMaxPetRequiresVerification()
    {
        var t = Create(); Sync(t, 3); Gain(t, 1);
        Assert.Equal(105L, t.KnownTotal(1002)); Assert.Equal(DataQuality.Uncertain, t.Quality); Assert.Single(t.Session.Events);
    }
    [Fact]
    public void SavedStateRoundTripsWithPreferencesAndRestartsUncertain()
    {
        var path = Path.Combine(Path.GetTempPath(), "pet-tests-" + Guid.NewGuid(), "state.json");
        try
        {
            var store = new StateStore(path, _ => { }); var t = Create(); Sync(t, 2, 46); Gain(t, 1);
            t.Pin(1002, true); t.Dismiss(1003); store.Save(t.Saved); store.Save(t.Saved);
            Assert.True(File.Exists(path + ".bak")); var loaded = Create(store.Load());
            Assert.Equal(77L, loaded.KnownTotal(1002)); Assert.Equal(DataQuality.Uncertain, loaded.Quality);
            Assert.Contains(1002, loaded.VisibleIds); Assert.Contains(1003, loaded.Current.Preferences.Dismissed); Assert.Empty(loaded.Session.Events);
        }
        finally { Directory.Delete(Path.GetDirectoryName(path)!, true); }
    }
    [Fact]
    public void CorruptSaveIsReportedAndPreserved()
    {
        var path = Path.GetTempFileName(); var logs = new List<string>();
        try
        {
            File.WriteAllText(path, "{broken");
            Assert.Throws<InvalidDataException>(() => new StateStore(path, logs.Add).Load());
            Assert.Equal("{broken", File.ReadAllText(path)); Assert.Contains(logs, m => m.Contains("Kayıt okunamadı"));
        }
        finally { File.Delete(path); }
    }
    [Theory]
    [InlineData(-1, 0)] [InlineData(0, 5)] [InlineData(1, 25)] [InlineData(2, 75)] [InlineData(3, 1)]
    public void InvalidLevelsRejected(int level, int count) => Assert.Throws<ArgumentException>(() => Progress.Total(new(1002, level, count)));
}
