using System.Text.Json;

namespace PetTracker.Core;

public sealed class OverlayPreferences
{
    public int OpacityPercent { get; set; } = 100;
    public bool Locked { get; set; }
    public bool AlwaysOnTop { get; set; } = true;
    public int Height { get; set; } = OverlayHeight.Pixels(OverlayHeight.DefaultCards);
    public int VisibleCards { get; set; } = OverlayHeight.DefaultCards;
    public int HideHotkeyKey { get; set; } = 0x50;
    public int HideHotkeyModifiers { get; set; } = 2;
    public int CompactHotkeyKey { get; set; } = 0x55;
    public int CompactHotkeyModifiers { get; set; } = 2;
    public string Language { get; set; } = "tr";
    public int LayoutVersion { get; set; } = 4;
    public int? Left { get; set; }
    public int? Top { get; set; }
    public int MapWorldId { get; set; } = 1010;
    public bool MapShowAllFarmable { get; set; }
    public bool MapHideUnlocated { get; set; }
    public bool MapUnlocatedCategory { get; set; }
    public bool MapAllPetsCategory { get; set; }
    public bool MapPortraitMarkers { get; set; } = true;
    public bool RiftAlertsEnabled { get; set; }
    public bool RiftSoundEnabled { get; set; } = true;
    public int RiftLeadMinutes { get; set; } = 5;
    public List<string> RiftDelivered { get; set; } = [];
    public int MapWidth { get; set; } = 1100;
    public int MapHeight { get; set; } = 800;
    public int? MapLeft { get; set; }
    public int? MapTop { get; set; }
    public void Validate()
    {
        OpacityPercent = Math.Clamp(OpacityPercent, 30, 100);
        RiftLeadMinutes = Math.Clamp(RiftLeadMinutes, 1, 120);
        RiftDelivered = (RiftDelivered??[]).TakeLast(32).ToList();
        VisibleCards = OverlayHeight.Clamp(VisibleCards);
        Height = OverlayHeight.Pixels(VisibleCards);
        if(!ShortcutKey.IsValid(CompactHotkeyKey,CompactHotkeyModifiers)) {CompactHotkeyKey=0x55;CompactHotkeyModifiers=2;}
        if(!ShortcutKey.IsValid(HideHotkeyKey,HideHotkeyModifiers)||HideHotkeyKey==CompactHotkeyKey&&HideHotkeyModifiers==CompactHotkeyModifiers){HideHotkeyKey=0x50;HideHotkeyModifiers=2;}
        MapWidth = Math.Clamp(MapWidth, 760, 1800);
        MapHeight = Math.Clamp(MapHeight, 550, 1400);
        if (!InstallationProfile.Supports(Language)) Language = "tr";
    }
    public static OverlayPreferences Load(string path, Action<string> log)
    {
        if (!File.Exists(path)) return new();
        try
        {
            var json = File.ReadAllText(path);
            var result = JsonSerializer.Deserialize<OverlayPreferences>(json) ?? throw new InvalidDataException("Overlay ayarları boş");
            // Missing LayoutVersion in legacy JSON must remain version zero for migration.
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty(nameof(LayoutVersion), out _)) result.LayoutVersion = 0;
            if (result.LayoutVersion < 2) { result.Height = Math.Clamp(result.Height / 2, 220, 500); result.LayoutVersion = 2; }
            if (result.LayoutVersion < 3) { result.Height = (int)Math.Round(result.Height * 4d / 3); result.LayoutVersion = 3; }
            if(result.LayoutVersion<4){result.VisibleCards=OverlayHeight.DefaultCards;result.LayoutVersion=4;}
            result.Validate(); return result;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { log("Overlay ayarları okunamadı: " + ex.Message); return new(); }
    }
    public void Save(string path)
    {
        Validate(); Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true })); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

public static class ShortcutKey
{
    public static bool IsValid(int key,int modifiers)=>(key is >=0x30 and <=0x39 or >=0x41 and <=0x5A or >=0x70 and <=0x87)
        && (modifiers&~0x7)==0 && modifiers!=0;
}
