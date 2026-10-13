using System.Text.Json;

namespace PetTracker.App;

public sealed class OverlayPreferences
{
    public int OpacityPercent { get; set; } = 100;
    public bool Locked { get; set; }
    public bool AlwaysOnTop { get; set; } = true;
    public int Height { get; set; } = 467;
    public string Language { get; set; } = "tr";
    public int LayoutVersion { get; set; }
    public int? Left { get; set; }
    public int? Top { get; set; }
    public void Validate()
    {
        OpacityPercent = Math.Clamp(OpacityPercent, 30, 100);
        Height = Math.Clamp(Height, 293, 1200);
        if (!UiText.IsSupported(Language)) Language="tr";
    }
    public static OverlayPreferences Load(string path, Action<string> log)
    {
        if (!File.Exists(path)) return new() { LayoutVersion=3 };
        try
        {
            var result = JsonSerializer.Deserialize<OverlayPreferences>(File.ReadAllText(path)) ?? throw new InvalidDataException("Overlay ayarları boş");
            if(result.LayoutVersion<2) { result.Height=Math.Clamp(result.Height/2,220,500);result.LayoutVersion=2; }
            if(result.LayoutVersion<3){result.Height=(int)Math.Round(result.Height*4d/3);result.LayoutVersion=3;}
            result.Validate(); return result;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { log("Overlay ayarları okunamadı: " + ex.Message); return new(); }
    }
    public void Save(string path)
    {
        Validate(); var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, path, true);
    }
}
