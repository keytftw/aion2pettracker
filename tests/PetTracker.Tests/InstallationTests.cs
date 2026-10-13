using PetTracker.Core;
namespace PetTracker.Tests;
public sealed class InstallationTests
{
    [Theory]
    [InlineData("tr")][InlineData("en")][InlineData("de")][InlineData("ru")][InlineData("es")]
    [InlineData("fr")][InlineData("pt")][InlineData("ar")][InlineData("hi")][InlineData("zh")]
    public void SetupMustAcceptTermsAndSucceedBeforeWritingReceipt(string language)
    {
        var gate=new InstallationGate();Assert.Throws<InvalidOperationException>(()=>gate.BeginInstall());
        gate.SelectLanguage(language);Assert.False(gate.Accept(false));Assert.Equal(InstallationStage.Terms,gate.Stage);
        Assert.Throws<InvalidOperationException>(()=>gate.CreateReceipt("v1",DateTimeOffset.UtcNow));
        Assert.True(gate.Accept(true));gate.BeginInstall();Assert.False(gate.Complete(false));Assert.Equal(InstallationStage.Location,gate.Stage);
        Assert.Throws<InvalidOperationException>(()=>gate.CreateReceipt("v1",DateTimeOffset.UtcNow));
        gate.BeginInstall();Assert.True(gate.Complete(true));Assert.Equal(language,gate.CreateReceipt("v1",DateTimeOffset.UtcNow).Language);
    }
    [Fact] public void GoingBackToLanguageRequiresNewConsent()
    {var gate=new InstallationGate();gate.SelectLanguage("en");gate.Accept(true);gate.Back();gate.Back();gate.SelectLanguage("tr");Assert.False(gate.Accept(false));Assert.Throws<InvalidOperationException>(()=>gate.BeginInstall());}
    [Fact] public void InstalledLaunchKeepsLanguageButStillRequiresFreshCollection()
    {
        var gate=new StartupGate();gate.ConfigureInstallation("de");gate.FinishIntro(4);
        Assert.Equal(StartupStage.Synchronizing,gate.Stage);Assert.Equal("de",gate.Language);
        Assert.False(gate.Observe(true,DataQuality.Exact,false));Assert.False(gate.Observe(false,DataQuality.Exact,true));Assert.False(gate.Observe(true,DataQuality.Uncertain,true));
        Assert.True(gate.Observe(true,DataQuality.Exact,true));
        var reopened=new StartupGate();reopened.ConfigureInstallation("de");reopened.FinishIntro(4);Assert.Equal(StartupStage.Synchronizing,reopened.Stage);
    }
    [Fact] public void UnsupportedLanguageCannotAuthorizeSetupOrStartup()
    {Assert.Throws<InvalidOperationException>(()=>new InstallationGate().SelectLanguage("xx"));Assert.Throws<InvalidOperationException>(()=>new StartupGate().ConfigureInstallation("xx"));}
    [Fact] public void PreferencesAndReceiptSurviveReopenAndLanguageChange()
    {
        var folder=Path.Combine(Path.GetTempPath(),"A2PetTracker-Test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var settingsPath=Path.Combine(folder,"prefs.json");var receiptPath=Path.Combine(folder,"installation.json");
        try
        {
            var preferences=new OverlayPreferences{Language="ru",OpacityPercent=65,VisibleCards=5,Locked=true,AlwaysOnTop=false,Left=350,Top=180};preferences.Save(settingsPath);
            new InstallationProfile("ru","v1",DateTimeOffset.UtcNow).Save(receiptPath);
            var saved=OverlayPreferences.Load(settingsPath,_=>{});Assert.Equal("ru",saved.Language);Assert.Equal(65,saved.OpacityPercent);Assert.Equal(OverlayHeight.Pixels(5),saved.Height);Assert.Equal(5,saved.VisibleCards);Assert.True(saved.Locked);Assert.False(saved.AlwaysOnTop);Assert.Equal(350,saved.Left);
            saved.Language="es";saved.Save(settingsPath);Assert.Equal("es",OverlayPreferences.Load(settingsPath,_=>{}).Language);Assert.True(InstallationProfile.Load(receiptPath)!.IsValid);
            // On startup preferences take precedence over the original installation language.
            var gate=new StartupGate();gate.ConfigureInstallation(OverlayPreferences.Load(settingsPath,_=>{}).Language);Assert.Equal("es",gate.Language);
            File.WriteAllText(receiptPath,"broken");Assert.Null(InstallationProfile.Load(receiptPath));
            Assert.Throws<InvalidDataException>(()=>new InstallationProfile("xx","v1",DateTimeOffset.UtcNow).Save(receiptPath));
        }
        finally {foreach(var file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);}
    }
    [Fact] public void LegacySettingsMigrateOnceWithoutResettingLanguageOrPosition()
    {
        var path=Path.GetTempFileName();try{File.WriteAllText(path,"{\"Language\":\"en\",\"Height\":700,\"OpacityPercent\":60,\"Left\":321}");var preferences=OverlayPreferences.Load(path,_=>{});Assert.Equal("en",preferences.Language);Assert.Equal(OverlayHeight.Pixels(3),preferences.Height);Assert.Equal(321,preferences.Left);preferences.Save(path);Assert.Equal(OverlayHeight.Pixels(3),OverlayPreferences.Load(path,_=>{}).Height);}finally{File.Delete(path);}
    }
}
