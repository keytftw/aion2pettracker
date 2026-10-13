using PetTracker.Core;
namespace PetTracker.Tests;

public sealed class InstallationLocationTests
{
    [Theory]
    [InlineData("")][InlineData("relative-folder")][InlineData("C:\\")][InlineData("D:\\")][InlineData("C:\\Apps\\setup.exe")]
    public void CannotInstallToRelativePathDriveRootOrExecutable(string path)
        =>Assert.Throws<ArgumentException>(()=>InstallationLocation.Normalize(path));
    [Fact] public void PreservesChosenDriveAndSpacesWithoutAppendingAnotherDirectory()
        =>Assert.Equal("D:\\My Apps\\A2PetTracker",InstallationLocation.Normalize("D:\\My Apps\\A2PetTracker\\"));
    [Fact] public void InstallationReceiptKeepsLocationAndShortcutChoice()
    {
        var path=Path.GetTempFileName();
        try
        {
            new InstallationProfile("tr","v1",DateTimeOffset.UtcNow){InstallDirectory="D:\\My Apps\\A2PetTracker",DesktopShortcut=false}.Save(path);
            var saved=InstallationProfile.Load(path)!;
            Assert.Equal("D:\\My Apps\\A2PetTracker",saved.InstallDirectory);Assert.False(saved.DesktopShortcut);
            File.WriteAllText(path,"{\"Language\":\"en\",\"AcceptedTermsVersion\":\"v1\",\"AcceptedAt\":\"2026-10-14T00:00:00Z\"}");
            var legacy=InstallationProfile.Load(path)!;Assert.Null(legacy.InstallDirectory);Assert.True(legacy.DesktopShortcut);
        }
        finally{File.Delete(path);}
    }
}
