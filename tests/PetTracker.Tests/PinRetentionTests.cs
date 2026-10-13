using PetTracker.Core;

namespace PetTracker.Tests;

public sealed class PinRetentionTests
{
    private static readonly CharacterScope Scope=new("Player","1");
    private static PetCatalog Catalog()=>new([new(1,"Pet","Fera")]);
    private static Tracker Create(SavedState? state=null)
    {var tracker=new Tracker(Catalog(),state??new(),_=>{});tracker.Select(Scope);return tracker;}

    [Theory]
    [InlineData(0,2)]
    [InlineData(3,0)]
    public void UnpinKeepsCardAcrossSnapshotReconnectAndReload(int level,int souls)
    {
        var tracker=Create();tracker.Synchronize(new(Scope,"s",0,[new(1,level,souls)]));
        tracker.Pin(1,true);tracker.Pin(1,false);
        Assert.Contains(1,tracker.VisibleIds);Assert.Empty(tracker.Current.Preferences.Pinned);
        var total=tracker.KnownTotal(1);tracker.Reconnect();
        tracker.Synchronize(new(Scope,"next",0,[new(1,level,souls)]));
        Assert.Contains(1,tracker.VisibleIds);Assert.Equal(total,tracker.KnownTotal(1));Assert.Empty(tracker.Session.Events);
        var path=Path.GetTempFileName();try
        {
            var store=new StateStore(path,_=>{});store.Save(tracker.Saved);var restored=Create(store.Load());
            Assert.Contains(1,restored.VisibleIds);Assert.Empty(restored.Current.Preferences.Pinned);
            restored.Dismiss(1);store.Save(restored.Saved);restored=Create(store.Load());
            Assert.Empty(restored.VisibleIds);Assert.Equal(total,restored.KnownTotal(1));
            restored.Pin(1,true);Assert.Contains(1,restored.VisibleIds);Assert.DoesNotContain(1,restored.Current.Preferences.Dismissed);
        }finally{File.Delete(path);}
    }

    [Fact] public void DeleteRemovesPinnedCardAndNextSoulReactivatesWithoutRepinning()
    {
        var tracker=Create();tracker.Synchronize(new(Scope,"s",0,[new(1,0,2)]));tracker.Pin(1,true);
        tracker.Dismiss(1);Assert.Empty(tracker.VisibleIds);Assert.Empty(tracker.Current.Preferences.Pinned);
        Assert.Empty(tracker.Current.Preferences.RetainedCards);Assert.Equal(2L,tracker.KnownTotal(1));
        tracker.Gain(new(Scope,"s",1,1,1));Assert.Contains(1,tracker.VisibleIds);
        Assert.Empty(tracker.Current.Preferences.Pinned);Assert.Equal(3L,tracker.KnownTotal(1));
    }

    [Fact] public void LegacyPinnedCardRemainsVisibleWhenUnpinnedAndOtherCharactersAreIsolated()
    {
        var state=new SavedState();var profile=new ProfileState{Scope=Scope};profile.Preferences.Pinned.Add(1);state.Profiles[Scope.Key]=profile;
        var tracker=Create(state);tracker.Pin(1,false);Assert.Contains(1,tracker.VisibleIds);
        tracker.Select(new("Other","1"));Assert.Empty(tracker.VisibleIds);
        tracker.Select(Scope);Assert.Contains(1,tracker.VisibleIds);
    }
}
