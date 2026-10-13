using PetTracker.Core;
namespace PetTracker.Tests;
public class MaxCompletionTests
{
    private static Tracker Create(){var t=new Tracker(new PetCatalog([new(1114,"Spider","Fera")]),new SavedState(),_=>{});t.Select(new("Keyt","2312"));return t;}
    [Fact] public void SnapshotAlreadyMaxDoesNotNotify()
    {var t=Create();var notifications=0;t.ReachedMax+=_=>notifications++;t.Synchronize(new(t.Current.Scope,"s",0,[new(1114,3,0)]));Assert.Equal(0,notifications);}
    [Fact] public void ActualGainCrossingMaxNotifiesOnceAndDuplicateDoesNot()
    {var t=Create();var notifications=0;t.ReachedMax+=id=>{Assert.Equal(1114,id);notifications++;};t.Synchronize(new(t.Current.Scope,"s",0,[new(1114,2,74)]));var gain=new SoulEvent(t.Current.Scope,"s",1,1114,1);t.Gain(gain);t.Gain(gain);Assert.Equal(1,notifications);Assert.Equal(105,t.KnownTotal(1114));}
    [Fact] public void UncertainGainCannotNotifyMax()
    {var t=Create();var notifications=0;t.ReachedMax+=_=>notifications++;t.Synchronize(new(t.Current.Scope,"s",0,[new(1114,2,74)]));t.Gap("loss");t.Gain(new(t.Current.Scope,"s",1,1114,1));Assert.Equal(0,notifications);}
}
