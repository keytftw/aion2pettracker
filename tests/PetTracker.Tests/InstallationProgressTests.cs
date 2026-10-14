using PetTracker.Core;
namespace PetTracker.Tests;
public sealed class InstallationProgressTests
{
    [Fact]public void FastInstallCannotFinishBeforeSixSeconds()
    {var p=new InstallationProgress();p.Report(1);Assert.False(p.CanFinish(TimeSpan.FromSeconds(5.999)));Assert.True(p.CanFinish(TimeSpan.FromSeconds(6)));Assert.Equal(.5,p.Displayed(TimeSpan.FromSeconds(3)));}
    [Fact]public void SlowInstallNeverFillsUntilWorkIsComplete()
    {var p=new InstallationProgress();p.Report(.4);Assert.Equal(.4,p.Displayed(TimeSpan.FromSeconds(30)));Assert.False(p.CanFinish(TimeSpan.FromSeconds(30)));p.Report(.95);Assert.Equal(.95,p.Displayed(TimeSpan.FromSeconds(30)));p.Report(1);Assert.True(p.CanFinish(TimeSpan.FromSeconds(30)));}
    [Fact]public void ProgressCannotMoveBackwardsOrAcceptInvalidValues()
    {var p=new InstallationProgress();p.Report(.9);p.Report(.3);Assert.Equal(.9,p.CompletedWork);Assert.Throws<ArgumentOutOfRangeException>(()=>p.Report(double.NaN));}
}
