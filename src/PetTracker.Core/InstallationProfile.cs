using System.Text.Json;

namespace PetTracker.Core;

public sealed record InstallationProfile(string Language, string AcceptedTermsVersion, DateTimeOffset AcceptedAt)
{
    public string? InstallDirectory { get; init; }
    public bool DesktopShortcut { get; init; } = true;
    public static readonly string[] Languages = ["tr", "en", "de", "ru", "es", "fr", "pt", "ar", "hi", "zh"];
    public static bool Supports(string language) => Languages.Contains(language, StringComparer.Ordinal);
    public bool IsValid => Supports(Language) && !string.IsNullOrWhiteSpace(AcceptedTermsVersion) && AcceptedAt != default;
    public static string DataFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aion2PetTracker");
    public static string ReceiptPath => Path.Combine(DataFolder, "installation.json");
    public static InstallationProfile? Load(string path)
    {
        if (!File.Exists(path)) return null;
        try { var profile = JsonSerializer.Deserialize<InstallationProfile>(File.ReadAllText(path)); return profile?.IsValid == true ? profile : null; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return null; }
    }
    public void Save(string path)
    {
        if (!IsValid) throw new InvalidDataException("Installation consent is incomplete.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(this)); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

public enum InstallationStage { Language, Terms, Location, Installing, Finished }
public sealed class InstallationGate
{
    public InstallationStage Stage { get; private set; } = InstallationStage.Language;
    public string? Language { get; private set; }
    public void SelectLanguage(string language)
    {
        if (Stage != InstallationStage.Language || !InstallationProfile.Supports(language)) throw new InvalidOperationException("Choose a supported language first.");
        Language = language; Stage = InstallationStage.Terms;
    }
    public bool Accept(bool consent)
    {
        if (Stage != InstallationStage.Terms || !consent) return false;
        Stage = InstallationStage.Location; return true;
    }
    public void Back()
    {
        if (Stage == InstallationStage.Terms) Stage = InstallationStage.Language;
        else if (Stage == InstallationStage.Location) Stage = InstallationStage.Terms;
    }
    public void BeginInstall()
    {
        if (Stage != InstallationStage.Location) throw new InvalidOperationException("Installation terms have not been accepted.");
        Stage = InstallationStage.Installing;
    }
    public bool Complete(bool succeeded)
    {
        if (Stage != InstallationStage.Installing) return false;
        Stage = succeeded ? InstallationStage.Finished : InstallationStage.Location;
        return succeeded;
    }
    public InstallationProfile CreateReceipt(string termsVersion, DateTimeOffset acceptedAt)
    {
        if (Stage != InstallationStage.Finished) throw new InvalidOperationException("Installation has not completed.");
        return new(Language!, termsVersion, acceptedAt);
    }
}
