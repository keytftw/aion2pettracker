using PetTracker.Capture;
using PetTracker.Capture.Protocol;

if (args.Length >= 2 && args[0] == "--replay")
{
    var bytes = File.ReadAllBytes(args[1]);
    var entries = PetProtocol.ReadCollection(bytes, out var error, out var trailing);
    if (entries is null) { Console.WriteLine("FAIL: " + error); Environment.ExitCode = 1; return; }
    var tracker = new PetTracker.Core.Tracker(new PetTracker.Core.PetCatalog([]), new(), _ => { });
    var session = new LiveSession(tracker, _ => { });
    session.Accept(new StreamBegan("offline-replay"));
    session.Accept(new CollectionReceived("offline-replay", 1, entries, trailing));
    session.Accept(new OwnIdentity("offline-replay", 2, 150, new("OfflineValidation", "2312")));
    Console.WriteLine($"PASS: {bytes.Length} bayt, {entries.Count} pet, {trailing} ek bayt sayılmadı, kalite {tracker.Quality}.");
    return;
}

// A bounded, explicitly run diagnostic. No counters, game inputs, credentials or complete traffic dump.
var folder = Path.GetFullPath(args.Length > 0 ? args[0] : "collection-diagnostics");
var seconds = args.Length > 1 ? Math.Clamp(int.Parse(args[1]), 1, 120) : 45;
Directory.CreateDirectory(folder);
var logPath = Path.Combine(folder, "diagnostics.log");
var gate = new object();
var count = 0;
void Log(string text)
{
    lock (gate)
    {
        var line = DateTimeOffset.Now.ToString("O") + " " + text;
        Console.WriteLine(line); File.AppendAllText(logPath, line + Environment.NewLine);
    }
}
await using var capture = new PassiveCapture(message =>
{
    switch (message)
    {
        case CaptureNotice note: Log(note.Text); break;
        case IntegrityLost loss: Log(loss.Reason); break;
        case OwnIdentity: Log("Kendi karakter kimliği algılandı"); break;
        case CollectionReceived full: Log($"Collection çözüldü: {full.Entries.Count} kayıt"); break;
    }
}, body =>
{
    if (++count > 8) return;
    File.WriteAllBytes(Path.Combine(folder, $"collection-{count:D2}.bin"), body.ToArray());
    var result = PetProtocol.ReadCollection(body.Span, out var error);
    Log($"Collection {count}: {body.Length} bayt. " + (result is null ? error : $"OK, {result.Count} pet"));
});
capture.Start();
Log($"{seconds} saniye yalnızca pet collection tanısı. Sayaç kayıtları değiştirilmiyor.");
await Task.Delay(TimeSpan.FromSeconds(seconds));
Log($"Tanı süresi tamamlandı; {Math.Min(count, 8)} collection kaydı alındı.");
