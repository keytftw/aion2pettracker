using PetTracker.Core;
using PetTracker.Capture;
namespace PetTracker.Tests;
public sealed class TeleportCollectionTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public void CollectionEvidenceDistinguishesReceivedFromAppliedForBothIdentityOrders(bool identityFirst)
    {
        var logs=new List<string>();var tracker=new Tracker(new([new(1002,"Pet","Fera")]),new(),logs.Add);var session=new LiveSession(tracker,logs.Add);var scope=new CharacterScope("Test","2312");
        session.Accept(new StreamBegan("s"));session.Accept(new OwnIdentity("s",1,150,scope));session.Accept(new CollectionReceived("s",2,[new(1002,1,5)]));
        tracker.Gap("uncertain before teleport");session.Accept(new SoulReceived("s",3,1002,1));Assert.Equal(11L,tracker.KnownTotal(1002));
        if(identityFirst)session.Accept(new OwnIdentity("s",4,150,scope));
        session.Accept(new CollectionReceived("s",5,[new(1002,1,8)]));
        Assert.Equal(2,session.CollectionsReceived);Assert.Equal(identityFirst?2:1,session.CollectionsApplied);
        if(!identityFirst){Assert.Equal(DataQuality.Uncertain,tracker.Quality);session.Accept(new OwnIdentity("s",6,150,scope));}
        Assert.Equal(2,session.CollectionsApplied);Assert.Equal(13L,tracker.KnownTotal(1002));Assert.Equal(DataQuality.Exact,tracker.Quality);
        Assert.Contains(logs,l=>l.StartsWith("[COLLECTION RX]"));Assert.Contains(logs,l=>l.StartsWith("[COLLECTION APPLY]"));
    }
    [Fact]
    public void DuplicateCollectionIsReceivedButNeverReportedAsNewAppliedSnapshot()
    {
        var tracker=new Tracker(new([new(1002,"Pet","Fera")]),new(),_=>{});var session=new LiveSession(tracker,_=>{});var scope=new CharacterScope("Test","2312");
        session.Accept(new StreamBegan("s"));session.Accept(new OwnIdentity("s",1,150,scope));session.Accept(new CollectionReceived("s",2,[new(1002,0,1)]));
        session.Accept(new OwnIdentity("s",3,150,scope));session.Accept(new CollectionReceived("s",2,[new(1002,0,1)]));
        Assert.Equal(2,session.CollectionsReceived);Assert.Equal(1,session.CollectionsApplied);Assert.Contains("Eski/tekrar",session.CollectionStatus);
    }
}
