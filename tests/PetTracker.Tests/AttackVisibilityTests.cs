using PetTracker.Capture;
using PetTracker.Capture.Protocol;
using PetTracker.Core;

namespace PetTracker.Tests;

public sealed class AttackVisibilityTests
{
    private static readonly CharacterScope Scope = new("Keyt", "2312");
    private static (Tracker, LiveSession) Setup()
    {
        var catalog=new PetCatalog([new(1114,"Ashen Spider","Fera",[new(2101234,"Spider",20)])]);
        var tracker=new Tracker(catalog,new(),_=>{}); var session=new LiveSession(tracker,_=>{},catalog);
        session.Accept(new StreamBegan("s")); session.Accept(new OwnIdentity("s",1,150,Scope));
        session.Accept(new CollectionReceived("s",2,[new(1114,2,28)])); return(tracker,session);
    }
    [Fact]
    public void AlreadyMaxAttackStaysHiddenButExplicitPinWorks()
    {
        var (tracker,session)=Setup();tracker.Synchronize(new(Scope,"s",3,[new(1114,3,0)]));
        session.Accept(new NpcObserved("s",200,2101234));session.Accept(new TargetObserved("s",150,200));
        Assert.Empty(tracker.VisibleIds);Assert.Empty(tracker.Session.Attacked);Assert.Equal(105L,tracker.KnownTotal(1114));
        tracker.Pin(1114,true);Assert.Equal([1114],tracker.VisibleIds);
    }
    [Fact]
    public void ActualSoulReachingMaxKeepsCardVisibleUntilDismissed()
    {
        var (tracker,session)=Setup();tracker.Synchronize(new(Scope,"s",3,[new(1114,2,74)]));
        session.Accept(new NpcObserved("s",200,2101234));session.Accept(new TargetObserved("s",150,200));
        session.Accept(new SoulReceived("s",4,1114,1));Assert.Equal([1114],tracker.VisibleIds);Assert.Equal(105L,tracker.KnownTotal(1114));
        tracker.Dismiss(1114);session.Accept(new TargetObserved("s",150,200));Assert.Empty(tracker.VisibleIds);
    }
    [Fact]
    public void NewMaxSnapshotHidesAttackOnlyRowWithoutRemovingPins()
    {
        var (tracker,session)=Setup();session.Accept(new NpcObserved("s",200,2101234));session.Accept(new TargetObserved("s",150,200));
        Assert.Equal([1114],tracker.VisibleIds);tracker.Synchronize(new(Scope,"s",3,[new(1114,3,0)]));Assert.Empty(tracker.VisibleIds);
        tracker.Pin(1114,true);Assert.Equal([1114],tracker.VisibleIds);
    }
    [Fact]
    public void OwnAttackActivatesOnlyMatchingPetWithoutChangingSouls()
    {
        var (tracker,session)=Setup(); session.Accept(new NpcObserved("s",200,2101234));
        session.Accept(new TargetObserved("s",999,200)); Assert.Empty(tracker.VisibleIds);
        session.Accept(new TargetObserved("s",150,200)); Assert.Equal([1114],tracker.VisibleIds);
        Assert.Equal(58L,tracker.KnownTotal(1114)); Assert.Empty(tracker.Session.Events); Assert.Empty(tracker.Session.Souls);
    }
    [Fact]
    public void DismissedAttackRowStaysHiddenUntilRealSoulAndProgressIsPreserved()
    {
        var (tracker,session)=Setup(); session.Accept(new NpcObserved("s",200,2101234)); session.Accept(new TargetObserved("s",150,200));
        tracker.Dismiss(1114); session.Accept(new TargetObserved("s",150,200)); Assert.Empty(tracker.VisibleIds);
        session.Accept(new SoulReceived("s",3,1114,1)); Assert.Equal([1114],tracker.VisibleIds); Assert.Equal(59L,tracker.KnownTotal(1114));
    }
    [Fact]
    public void AmbiguousNpcCannotSelectWrongPet()
    {
        var catalog=new PetCatalog([new(1114,"One","Fera",[new(2101234,"Npc",20)]),new(1115,"Two","Fera",[new(2101234,"Npc",20)])]);
        Assert.Empty(catalog.PetByNpc);
    }
    [Fact]
    public void GapAndOtherStreamCannotReuseNpcMapping()
    {
        var (tracker,session)=Setup(); session.Accept(new NpcObserved("s",200,2101234)); session.Accept(new TargetObserved("other",150,200));
        Assert.Empty(tracker.VisibleIds); session.Accept(new IntegrityLost("loss")); session.Accept(new OwnIdentity("s",3,150,Scope));
        session.Accept(new TargetObserved("s",150,200)); Assert.Empty(tracker.VisibleIds); Assert.Equal(58L,tracker.KnownTotal(1114));
    }
    [Fact]
    public void SmallHitHeaderReadsOnlySourceAndTargetAndNeverDamage()
    {
        var hit=Assert.IsType<TargetObserved>(PetTargetProtocol.Decode("s",0x0338,new byte[]{150,1,0,200,1,100,0,0,0}));
        Assert.Equal(150u,hit.ActorId);Assert.Equal(200u,hit.TargetId);
        Assert.Null(PetTargetProtocol.Decode("s",0x0338,new byte[]{150,1,1,200,1,100,0,0,0}));
    }
    [Fact]
    public void LargeSpawnIdsFoldIntoTargetSpaceAndSummonReuseClearsNpcMapping()
    {
        const uint actor=4_000_000_000; const int code=2101234;
        var bytes=new List<byte>(); var remaining=actor;
        do { var b=(byte)(remaining&127); remaining>>=7; bytes.Add((byte)(b|(remaining>0?128:0))); } while(remaining>0);
        var prefix=bytes.ToArray();
        bytes.AddRange(new byte[]{12,0,0,(byte)(code&255),(byte)((code>>8)&255),(byte)((code>>16)&255),0,0,2});
        var npc=Assert.IsType<NpcObserved>(PetTargetProtocol.Decode("s",0x4136,bytes.ToArray()));
        Assert.Equal((actor&0x3fff)|0x4000,npc.ActorId);Assert.Equal(code,npc.NpcCode);
        var summon=Assert.IsType<EntityGone>(PetTargetProtocol.Decode("s",0x4136,prefix.Concat(new byte[]{0x5f,0}).ToArray()));
        Assert.Equal(npc.ActorId,summon.ActorId);
    }
}
