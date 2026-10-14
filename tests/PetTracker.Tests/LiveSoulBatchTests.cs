using System.Buffers.Binary;
using PetTracker.Core;
using PetTracker.Capture;
using PetTracker.Capture.Protocol;
namespace PetTracker.Tests;
public sealed class LiveSoulBatchTests
{
    private static readonly byte[] LiveBody=Convert.FromHexString("020E040000010000006504000001000000");
    private static readonly CharacterScope Scope=new("Test","2312");
    private static (Tracker Tracker,LiveSession Session) Create()
    {
        var tracker=new Tracker(new([new(1038,"Airon","Fera"),new(1125,"Aberrant Zaif","Fera")]),new(),_=>{});
        var session=new LiveSession(tracker,_=>{});session.Accept(new StreamBegan("s"));
        session.Accept(new OwnIdentity("s",1,150,Scope));session.Accept(new CollectionReceived("s",2,[new(1038,1,11),new(1125,2,8)]));return(tracker,session);
    }
    [Fact]public void ReportedLiveMessageMatchesBothInGameScreenshotsAndReplayDoesNotDoubleCount()
    {
        var(t,s)=Create();var message=Assert.IsType<SoulBatchReceived>(PetProtocol.Decode("s",27377,0x0D90,LiveBody));
        s.Accept(message);s.Accept(message);
        Assert.Equal(17L,t.KnownTotal(1038));Assert.Equal(new Progress(1,12,25,13),Progress.FromTotal(t.KnownTotal(1038)!.Value));
        Assert.Equal(39L,t.KnownTotal(1125));Assert.Equal(new Progress(2,9,75,66),Progress.FromTotal(t.KnownTotal(1125)!.Value));
        Assert.Equal(2,t.Session.Events.Count);Assert.Equal(1L,t.Session.Souls[1038]);Assert.Equal(1L,t.Session.Souls[1125]);Assert.Equal(DataQuality.Exact,t.Quality);
        s.Accept(Assert.IsType<SoulBatchReceived>(PetProtocol.Decode("s",27378,0x0D90,LiveBody)));
        Assert.Equal(18L,t.KnownTotal(1038));Assert.Equal(40L,t.KnownTotal(1125));
    }
    [Fact]public void InvalidLastRecordCannotPartiallyIncrementFirstPet()
    {
        var(t,s)=Create();var bad=LiveBody.ToArray();bad[13]=0;
        s.Accept(Assert.IsType<IntegrityLost>(PetProtocol.Decode("s",3,0x0D90,bad)));
        Assert.Equal(16L,t.KnownTotal(1038));Assert.Equal(38L,t.KnownTotal(1125));Assert.Empty(t.Session.Events);Assert.Equal(DataQuality.Uncertain,t.Quality);Assert.True(s.IdentityKnown);
    }
    [Fact]public void ReportedSingleBatchSingleSequenceKeepsExactCountsForBothPets()
    {
        var(t,s)=Create();t.Synchronize(new(Scope,"s",3,[new(1038,1,9),new(1125,2,4)]));
        for(var frame=4;frame<8;frame++)s.Accept(new SoulReceived("s",frame,1125,1));
        s.Accept(new SoulReceived("s",8,1038,1));s.Accept(PetProtocol.Decode("s",9,0x0D90,LiveBody)!);s.Accept(new SoulReceived("s",10,1038,1));
        Assert.Equal(17L,t.KnownTotal(1038));Assert.Equal(39L,t.KnownTotal(1125));
        Assert.Equal(3L,t.Session.Souls[1038]);Assert.Equal(5L,t.Session.Souls[1125]);Assert.Equal(DataQuality.Exact,t.Quality);
    }
    [Fact]public void BatchWaitsForIdentityAndNeverLeaksToAnotherCharacter()
    {
        var(t,s)=Create();s.Accept(new StreamBegan("new"));s.Accept(PetProtocol.Decode("new",1,0x0D90,LiveBody)!);
        Assert.Empty(t.Session.Events);Assert.False(s.IdentityKnown);
        var other=new CharacterScope("Other","2312");s.Accept(new OwnIdentity("new",2,160,other));
        Assert.Equal(other,t.Current.Scope);Assert.Equal(2,t.Session.Events.Count);Assert.Null(t.KnownTotal(1038));
        Assert.Equal(16L,t.Saved.Profiles[Scope.Key].LastKnownTotals[1038]);
    }
    [Fact]public void RepeatedPetEntriesCrossMaxTogetherWithoutFalseUncertainty()
    {
        var(t,s)=Create();t.Synchronize(new(Scope,"s",3,[new(1038,2,74)]));
        var body=LiveBody.ToArray();BinaryPrimitives.WriteUInt32LittleEndian(body.AsSpan(9),1038);
        s.Accept(PetProtocol.Decode("s",4,0x0D90,body)!);
        Assert.Equal(106L,t.KnownTotal(1038));Assert.Equal(2L,t.Session.Souls[1038]);Assert.Equal(2,t.Session.Events.Count);Assert.Equal(DataQuality.Exact,t.Quality);
    }
    [Fact]public void ThreeEntriesAndWrongLengthsAreValidatedAsOneCompleteMessage()
    {
        var body=new byte[25];body[0]=3;LiveBody.AsSpan(1).CopyTo(body.AsSpan(1));LiveBody.AsSpan(1,8).CopyTo(body.AsSpan(17));
        Assert.Equal(3,Assert.IsType<SoulBatchReceived>(PetProtocol.Decode("s",3,0x0D90,body)).Entries.Count);
        Assert.IsType<IntegrityLost>(PetProtocol.Decode("s",4,0x0D90,body.AsSpan(0,24)));
        Assert.IsType<IntegrityLost>(PetProtocol.Decode("s",5,0x0D90,new byte[]{1,14,4,0,0,1,0,0,0,0}));
    }
    [Fact]public void TcpSplitAndRetransmissionDeliverOneCompleteBatch()
    {
        var(t,s)=Create();var messages=new List<LiveMessage>();var stream=new ServerStream("s",m=>{messages.Add(m);if(m is not StreamBegan)s.Accept(m);},100);
        byte[] frame=[23,0x0D,0x90,..LiveBody];
        // Advance the test snapshot to an earlier stream frame identity.
        t.Select(Scope);t.Synchronize(new(Scope,"s",0,[new(1038,1,11),new(1125,2,8)]));
        stream.Push(101,frame.AsSpan(0,8),0);stream.Push(109,frame.AsSpan(8),1);stream.Push(101,frame,2);
        Assert.Single(messages.OfType<SoulBatchReceived>());Assert.Equal(17L,t.KnownTotal(1038));Assert.Equal(39L,t.KnownTotal(1125));Assert.Equal(DataQuality.Exact,t.Quality);
    }
}
