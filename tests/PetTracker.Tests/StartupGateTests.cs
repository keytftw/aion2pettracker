using PetTracker.Core;
namespace PetTracker.Tests;
public class StartupGateTests
{
    [Fact] public void CannotSkipIntroLanguageOrUncheckedTerms()
    {
        var gate=new StartupGate();Assert.False(gate.Accept(true));Assert.False(gate.Observe(true,DataQuality.Exact,true));
        gate.FinishIntro(3.4);Assert.Equal(StartupStage.Intro,gate.Stage);gate.FinishIntro(3.5);
        gate.SelectLanguage("tr");Assert.False(gate.Accept(false));Assert.Equal(StartupStage.Terms,gate.Stage);
        Assert.True(gate.Accept(true));Assert.Equal(StartupStage.Synchronizing,gate.Stage);
    }
    [Theory] [InlineData(false,DataQuality.Exact,true)] [InlineData(true,DataQuality.Uncertain,true)] [InlineData(true,DataQuality.Exact,false)]
    public void IncompleteOrCachedDataCannotOpenTracker(bool identity,DataQuality quality,bool fresh)
    {var gate=Waiting();Assert.False(gate.Observe(identity,quality,fresh));Assert.Equal(StartupStage.Synchronizing,gate.Stage);}
    [Fact] public void UnconfiguredProcessRequiresOnboarding()
    {var old=Waiting();Assert.True(old.Observe(true,DataQuality.Exact,true));Assert.Equal(StartupStage.Ready,old.Stage);Assert.Equal(StartupStage.Intro,new StartupGate().Stage);}
    [Fact] public void ClosingPreventsAdmission()
    {var gate=Waiting();gate.Close();Assert.False(gate.Observe(true,DataQuality.Exact,true));}
    private static StartupGate Waiting(){var gate=new StartupGate();gate.FinishIntro(4);gate.SelectLanguage("en");gate.Accept(true);return gate;}
}
