using PetTracker.Capture;
using PetTracker.Capture.Protocol;
using PetTracker.Core;
namespace PetTracker.Tests;
public class SoulShapeDiagnosticsTests
{
    [Theory] [InlineData(0,"yok")] [InlineData(1,"0x01")] [InlineData(8,"0x01")]
    public void RejectedSoulReportsShapeWithoutWeakeningValidation(int length,string marker)
    {
        var body=new byte[length];if(length>0)body[0]=1;
        var rejected=Assert.IsType<IntegrityLost>(PetProtocol.Decode("s",17,0x0D90,body));
        Assert.Contains($"uzunluk={length}",rejected.Reason);Assert.Contains("tür="+marker,rejected.Reason);Assert.Contains("frame=17",rejected.Reason);
    }
    [Fact] public void OtherDiscriminatorCannotSilentlyBecomeSoulOrBeIgnored()
    {var body=new byte[9];body[0]=0;var rejected=Assert.IsType<IntegrityLost>(PetProtocol.Decode("s",3,0x0D90,body));Assert.Contains("tür=0x00",rejected.Reason);}
    [Fact] public void AppBeforeGameAndRejectedSoulRequireFreshSnapshotToResumeExactCounts()
    {
        var catalog=new PetCatalog([new(1143,"Test pet","Fera")]);var tracker=new Tracker(catalog,new(),_=>{});var session=new LiveSession(tracker,_=>{},catalog);var scope=new CharacterScope("Test","2312");
        session.Accept(new StreamBegan("s"));session.Accept(new CollectionReceived("s",1,[new(1143,2,64)]));session.Accept(new OwnIdentity("s",2,150,scope));
        Assert.Equal(DataQuality.Exact,tracker.Quality);Assert.Equal(94L,tracker.KnownTotal(1143));
        session.Accept(new SoulReceived("s",3,1143,1));session.Accept(new SoulReceived("s",4,1143,1));Assert.Equal(96L,tracker.KnownTotal(1143));
        session.Accept(PetProtocol.Decode("s",5,0x0D90,new byte[]{0})!);session.Accept(new SoulReceived("s",6,1143,1));
        Assert.Equal(DataQuality.Uncertain,tracker.Quality);Assert.False(session.IdentityKnown);Assert.Equal(96L,tracker.KnownTotal(1143));
        session.Accept(new CollectionReceived("s",7,[new(1143,2,67)]));session.Accept(new OwnIdentity("s",8,150,scope));session.Accept(new SoulReceived("s",9,1143,1));
        Assert.Equal(DataQuality.Exact,tracker.Quality);Assert.Equal(98L,tracker.KnownTotal(1143));
    }
}
