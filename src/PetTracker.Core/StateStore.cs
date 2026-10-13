using System.Text.Json;

namespace PetTracker.Core;

public sealed class StateStore(string path, Action<string> log)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public SavedState Load()
    {
        if (!File.Exists(path)) return new();
        try
        {
            var state = JsonSerializer.Deserialize<SavedState>(File.ReadAllText(path), Json)
                ?? throw new InvalidDataException("Kayıt boş.");
            Validate(state);
            log("Kalıcı kayıt yüklendi; kesinlik için yeni snapshot gerekiyor.");
            return state;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NullReferenceException)
        {
            log($"Kayıt okunamadı: {ex.Message}. Orijinal dosya korunacak.");
            throw new InvalidDataException("Kayıt bozuk veya okunamıyor. Dosyayı yedekleyip farklı bir kayıt yolu seçin.", ex);
        }
    }
    public void Save(SavedState state)
    {
        Validate(state);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp = path + ".tmp";
        try
        {
            using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            { JsonSerializer.Serialize(file, state, Json); file.Flush(true); }
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
            else File.Move(temp, path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { log($"Kayıt yazılamadı: {ex.Message}"); throw; }
    }
    private static void Validate(SavedState state)
    {
        if (state.Version != 1 || state.Profiles is null) throw new InvalidDataException("Kayıt sürümü geçersiz.");
        foreach (var (key, p) in state.Profiles)
        {
            p.Scope.Validate();
            if (key != p.Scope.Key || p.LastKnownTotals is null || p.Preferences?.Pinned is null || p.Preferences.Dismissed is null || p.Preferences.CardOrder is null || p.Preferences.RetainedCards is null ||
                p.LastKnownTotals.Any(v => v.Key <= 0 || v.Value < 0) || p.Preferences.Pinned.Concat(p.Preferences.Dismissed).Concat(p.Preferences.RetainedCards).Any(id => id <= 0))
                throw new InvalidDataException("Karakter kaydı geçersiz.");
            if(p.Preferences.CardOrder.Any(id=>id<=0)||p.Preferences.CardOrder.Distinct().Count()!=p.Preferences.CardOrder.Count)
                throw new InvalidDataException("Pet kart sırası geçersiz.");
            if (p.Snapshot is { } s)
            {
                if (s.Scope != p.Scope || string.IsNullOrWhiteSpace(s.StreamId) || s.FrameId < 0 || s.Entries.Count > 512)
                    throw new InvalidDataException("Kayıt snapshot'ı geçersiz.");
                _ = s.Entries.ToDictionary(e => e.PetId, Progress.Total);
            }
        }
    }
}
