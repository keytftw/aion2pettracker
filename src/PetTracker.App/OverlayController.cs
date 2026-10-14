using System.Collections.Concurrent;
using PetTracker.Capture;
using PetTracker.Core;

namespace PetTracker.App;

public sealed class OverlayController : IAsyncDisposable
{
    private readonly ConcurrentQueue<LiveMessage> queue = new();
    private readonly StateStore store;
    private readonly string logPath;
    private PassiveCapture? capture;
    private int overflow;
    private uint observedActor;
    private Task? stopping;
    public bool LiveMode { get; }
    public bool Running => capture?.Active==true;
    public bool Listening=>capture?.Listening==true;
    public PetCatalog Catalog { get; }
    public Tracker Tracker { get; }
    public LiveSession Session { get; }
    public string Folder { get; }
    public List<string> Logs { get; } = [];
    public Queue<string> SoulDiagnostics {get;}=new();
    public OverlayController(bool live, string? folder = null)
    {
        LiveMode = live;
        Folder = folder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aion2PetTracker", live ? "Live" : "Demo");
        Directory.CreateDirectory(Folder); logPath = Path.Combine(Folder, "tracker.log");
        Catalog = PetCatalog.Load(Path.Combine(AppContext.BaseDirectory, "data", "pet-catalog.json"));
        store = new(Path.Combine(Folder, "state.json"), Log);
        var saved=store.Load();
        if(live)foreach(var profile in saved.Profiles.Values){profile.Snapshot=null;profile.LastKnownTotals.Clear();}
        Tracker = new(Catalog, saved, Log); Session = new(Tracker, Log, Catalog);
        if (live && Tracker.Saved.Profiles.Values.FirstOrDefault() is { } previous) Tracker.Select(previous.Scope);
        if (!live) Tracker.Select(new("Keyt", "DEMO"));
    }
    public void Log(string text)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} {text}";
        Logs.Add(line); if (Logs.Count > 500) Logs.RemoveAt(0);
        if(text.Contains("soulBodyHex=",StringComparison.Ordinal))
        {SoulDiagnostics.Enqueue(line);while(SoulDiagnostics.Count>128)SoulDiagnostics.Dequeue();}
        try { File.AppendAllText(logPath, line + Environment.NewLine); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Logs.Add("LOG HATASI: " + ex.Message); }
    }
    public void Save() => store.Save(Tracker.Saved);
    public void Start()
    {
        if (!LiveMode || capture is not null) return;
        stopping = null;
        capture = new(message =>
        {
            if (message is OwnIdentity own) Volatile.Write(ref observedActor, own.ActorId);
            if (message is StreamBegan or StreamEnded || message is IntegrityLost {IdentityMayBeLost:true}) Volatile.Write(ref observedActor, 0);
            if (message is TargetObserved target && target.ActorId != Volatile.Read(ref observedActor)) return;
            if (queue.Count >= 2048) { Interlocked.Exchange(ref overflow, 1); capture?.RequestStop(); return; }
            queue.Enqueue(message);
        });
        capture.Start(); Log("Pasif canlı okuma başlatıldı.");
    }
    public Task Stop() => stopping ??= StopCore();
    private async Task StopCore()
    {
        var old = capture;
        if (old is null) return;
        await old.DisposeAsync(); capture = null;
        do { Pump(); } while (!queue.IsEmpty);
        Save();
    }
    public bool Pump()
    {
        var changed = false;
        if (Interlocked.Exchange(ref overflow, 0) != 0)
        {
            while (queue.TryDequeue(out _)) { }
            Session.Accept(new StreamEnded("Arayüz kuyruğu doldu; tam collection gerekiyor")); changed = true;
        }
        for (var i = 0; i < 256 && queue.TryDequeue(out var message); i++)
        {
            try { Session.Accept(message); }
            catch (Exception ex) { Session.Accept(new IntegrityLost("Olay işleme hatası: " + ex.Message)); }
            changed = true;
        }
        if (changed)
            try { Save(); } catch (Exception ex) { Log("KAYIT HATASI: " + ex.Message); Tracker.Gap("Kayıt yazılamadı"); }
        return changed;
    }
    public void LoadPreview()
    {
        Tracker.Synchronize(new(Tracker.Current.Scope, "demo", 0,
            [new(1114,2,28), new(1106,2,13), new(1007,3,0), new(1124,0,0), new(1178,1,8)]));
        foreach (var id in new[] { 1114, 1106, 1007, 1124, 1178 }) Tracker.Pin(id, true);
    }
    public async ValueTask DisposeAsync() { await Stop(); Save(); }
}
