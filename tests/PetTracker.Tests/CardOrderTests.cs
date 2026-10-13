using PetTracker.Core;

namespace PetTracker.Tests;

public sealed class CardOrderTests
{
    [Fact]
    public void OrderSurvivesSaveReloadAndSnapshotWithoutChangingProgress()
    {
        var scope=new CharacterScope("Test","1");
        var catalog=new PetCatalog([new(1,"One","Fera"),new(2,"Two","Fera"),new(3,"Three","Fera")]);
        var tracker=new Tracker(catalog,new(),_=>{});tracker.Select(scope);
        foreach(var id in new[]{1,2,3})tracker.Pin(id,true);
        tracker.Synchronize(new(scope,"s",0,[new(1,1,4),new(2,2,10),new(3,0,2)]));
        Assert.True(tracker.MoveCard(3,-1));Assert.True(tracker.MoveCard(3,-1));
        Assert.Equal(new[]{3,1,2},tracker.VisibleIds);Assert.False(tracker.MoveCard(3,-1));
        var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".json");
        try
        {
            var store=new StateStore(path,_=>{});store.Save(tracker.Saved);
            var restored=new Tracker(catalog,store.Load(),_=>{});restored.Select(scope);
            restored.Synchronize(new(scope,"next",0,[new(1,1,4),new(2,2,10),new(3,0,2)]));
            Assert.Equal(new[]{3,1,2},restored.VisibleIds);Assert.Equal(9L,restored.KnownTotal(1));
            Assert.Equal(40L,restored.KnownTotal(2));Assert.Equal(2L,restored.KnownTotal(3));
            restored.Pin(1,false);restored.Dismiss(1);Assert.Equal(new[]{3,2},restored.VisibleIds);
            restored.Pin(1,true);Assert.Equal(new[]{3,1,2},restored.VisibleIds);
            restored.Select(new("Other","1"));foreach(var id in new[]{1,2,3})restored.Pin(id,true);
            Assert.Equal(new[]{1,2,3},restored.VisibleIds);
        }
        finally {if(File.Exists(path))File.Delete(path);}
    }
    [Theory]
    [InlineData(0,10,3,0)]
    [InlineData(38,10,3,71)]
    [InlineData(140,10,3,142)]
    [InlineData(2000,10,3,497)]
    [InlineData(2000,3,3,0)]
    [InlineData(-100,10,3,0)]
    public void ScrollStopsAtCardBoundariesAndListEdges(float offset,int pets,int slots,float expected)
        =>Assert.Equal(expected,CardScroll.Snap(offset,pets,slots,71));
}
