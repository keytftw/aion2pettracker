using PetTracker.App;
using PetTracker.Core;
using PetTracker.Setup;
namespace PetTracker.Tests;
public class InstallerLocalizationTests
{
    [Fact] public void EveryInstallLanguageHasTermsConsentAndInstallerMessages()
    {
        Assert.True(InstallerText.Complete);Assert.True(UiText.Complete);Assert.True(StartupText.Complete);Assert.True(TermsContent.Complete);
        Assert.Equal(InstallationProfile.Languages,UiText.Languages.Select(language=>language.Code));
        foreach(var (code,_) in UiText.Languages)
        {
            Assert.False(string.IsNullOrWhiteSpace(InstallerText.Get(code,"install")));
            Assert.False(string.IsNullOrWhiteSpace(InstallerText.Get(code,"readyHint")));
            foreach(var key in new[]{"location","locationHint","folder","browse","desktop"})Assert.False(string.IsNullOrWhiteSpace(InstallerText.Get(code,key)));
            Assert.False(string.IsNullOrWhiteSpace(StartupText.Get(code,"consent")));
            Assert.Contains("13.",TermsContent.Get(code));
        }
    }
}
