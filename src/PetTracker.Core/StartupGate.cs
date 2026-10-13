namespace PetTracker.Core;

// Admission applies to this process only; acceptance is never restored from disk.
public enum StartupStage { Intro, Language, Terms, Synchronizing, Ready, Closed }
public sealed class StartupGate
{
    public StartupStage Stage { get; private set; } = StartupStage.Intro;
    public string? Language { get; private set; }
    public void FinishIntro(double seconds) { if(Stage==StartupStage.Intro && seconds>=3.5)Stage=StartupStage.Language; }
    public void SelectLanguage(string language) { if(Stage!=StartupStage.Language)throw new InvalidOperationException("Language step required"); Language=language;Stage=StartupStage.Terms; }
    public bool Accept(bool checkedConsent) { if(Stage!=StartupStage.Terms || !checkedConsent)return false;Stage=StartupStage.Synchronizing;return true; }
    public bool Observe(bool identityKnown, DataQuality quality, bool hasFreshSnapshot)
    { if(Stage!=StartupStage.Synchronizing || !identityKnown || quality!=DataQuality.Exact || !hasFreshSnapshot)return false;Stage=StartupStage.Ready;return true; }
    public void Close()=>Stage=StartupStage.Closed;
}
