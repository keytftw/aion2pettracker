namespace PetTracker.Core;

// Every process still requires a fresh snapshot; installed onboarding is persisted separately.
public enum StartupStage { Intro, Language, Terms, Synchronizing, Ready, Closed }
public sealed class StartupGate
{
    public StartupStage Stage { get; private set; } = StartupStage.Intro;
    public string? Language { get; private set; }
    private bool installed;
    public void ConfigureInstallation(string language)
    {
        if (Stage != StartupStage.Intro || !InstallationProfile.Supports(language)) throw new InvalidOperationException("Invalid installed language.");
        Language = language; installed = true;
    }
    public void FinishIntro(double seconds) { if(Stage==StartupStage.Intro && seconds>=3.5)Stage=installed?StartupStage.Synchronizing:StartupStage.Language; }
    public void SelectLanguage(string language) { if(Stage!=StartupStage.Language)throw new InvalidOperationException("Language step required"); Language=language;Stage=StartupStage.Terms; }
    public bool Accept(bool checkedConsent) { if(Stage!=StartupStage.Terms || !checkedConsent)return false;Stage=StartupStage.Synchronizing;return true; }
    public bool Observe(bool identityKnown, DataQuality quality, bool hasFreshSnapshot)
    { if(Stage!=StartupStage.Synchronizing || !identityKnown || quality!=DataQuality.Exact || !hasFreshSnapshot)return false;Stage=StartupStage.Ready;return true; }
    public void Close()=>Stage=StartupStage.Closed;
}
