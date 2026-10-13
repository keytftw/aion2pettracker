namespace PetTracker.Core;

// Independently implemented offline state machine; no packet capture dependencies.
public sealed class Tracker
{
    private readonly PetCatalog catalog;
    private readonly Action<string> log;
    private string? stream;
    private long lastFrame = -1;
    public SavedState Saved { get; }
    public ProfileState Current { get; private set; } = null!;
    public bool HasCurrent => Current is not null;
    public SessionActivity Session { get; private set; } = new();
    public DataQuality Quality { get; private set; } = DataQuality.WaitingForSnapshot;
    public event Action<int>? ReachedMax;
    public Tracker(PetCatalog catalog, SavedState saved, Action<string> log)
    { this.catalog = catalog; Saved = saved; this.log = log; }

    public void Select(CharacterScope scope)
    {
        scope.Validate();
        if (!Saved.Profiles.TryGetValue(scope.Key, out var profile))
            Saved.Profiles.Add(scope.Key, profile = new() { Scope = scope });
        Current = profile;
        Session = new(); stream = null; lastFrame = -1;
        Quality = profile.Snapshot is null ? DataQuality.WaitingForSnapshot : DataQuality.Uncertain;
        log($"Karakter seçildi: {scope.Character} / {scope.Server}. Yeni tam snapshot bekleniyor.");
    }
    public IEnumerable<int> VisibleIds
    {
        get
        {
            var order=Current.Preferences.CardOrder.Select((id,index)=>(id,index)).GroupBy(pair=>pair.id).ToDictionary(group=>group.Key,group=>group.First().index);
            return Current.Preferences.Pinned.Union(Current.Preferences.RetainedCards.Union(Session.Active).Union(Session.Attacked.Where(id=>KnownTotal(id) is not >=105)).Where(id => !Current.Preferences.Dismissed.Contains(id)))
                .OrderBy(id=>order.GetValueOrDefault(id,int.MaxValue)).ThenBy(id=>id);
        }
    }
    public bool MoveCard(int id,int direction)
    {
        if(direction is not (-1 or 1))throw new ArgumentOutOfRangeException(nameof(direction));
        var visible=VisibleIds.ToArray();var index=Array.IndexOf(visible,id);var target=index+direction;
        if(index<0||target<0||target>=visible.Length)return false;
        var order=Current.Preferences.CardOrder.Concat(visible).Distinct().ToList();
        var from=order.IndexOf(id);var to=order.IndexOf(visible[target]);
        (order[from],order[to])=(order[to],order[from]);Current.Preferences.CardOrder=order;return true;
    }
    public void ObservePetAttack(int id)
    {
        if (!catalog.Pets.ContainsKey(id)) { ReportUnknown(id); return; }
        if(KnownTotal(id) is >=105)return; // Already completed pets require explicit pinning; a new soul reaching MAX still stays active.
        // User requested attack-driven visibility. This is separate from gains and progress.
        if (Session.Attacked.Add(id)) log($"Pet ID {id}: kendi saldırın nedeniyle gösteriliyor; soul sayacı değişmedi.");
    }
    public long? KnownTotal(int id)
    {
        if (Current.LastKnownTotals.TryGetValue(id, out var total)) return total;
        // Sparse collection also describes zero progress for omitted regular catalog pets.
        // This is a read-only baseline projection: pinning never writes or increments counters.
        // Cached/gap state may show it only as "last known"; Quality controls exactness in the UI.
        if (Current.Snapshot is { OmittedEntriesAreZero: true } baseline &&
            !baseline.Entries.Any(e => e.PetId == id) && catalog.Pets.TryGetValue(id, out var pet) &&
            !string.Equals(pet.Genus, "Special", StringComparison.OrdinalIgnoreCase)) return 0;
        return null;
    }
    public void Pin(int id, bool pinned)
    {
        if (id <= 0) throw new ArgumentException("Pet ID pozitif olmalı.");
        if (pinned)
        {
            Current.Preferences.Pinned.Add(id);Current.Preferences.RetainedCards.Add(id);
            Current.Preferences.Dismissed.Remove(id);
        }
        else if(Current.Preferences.Pinned.Remove(id)&&!Current.Preferences.Dismissed.Contains(id))
            Current.Preferences.RetainedCards.Add(id);
    }
    public void Dismiss(int id)
    { Current.Preferences.Pinned.Remove(id);Current.Preferences.RetainedCards.Remove(id);Current.Preferences.Dismissed.Add(id); Session.Active.Remove(id); }
    public void Gap(string reason)
    { Quality = DataQuality.Uncertain; log($"Doğrulama gerekiyor: {reason}. Son bilinen sayaçlar korundu."); }
    public void Reconnect()
    { stream = null; lastFrame = -1; Gap("Bağlantı yeniden başladı"); }

    public void Synchronize(AuthoritativeCollectionSnapshot snapshot)
    {
        if (snapshot.Scope != Current.Scope) throw new ArgumentException("Snapshot başka karakter/sunucuya ait.");
        if (string.IsNullOrWhiteSpace(snapshot.StreamId) || snapshot.FrameId < 0 || snapshot.Entries is null || snapshot.Entries.Count > 512)
            throw new ArgumentException("Tam snapshot kimliği veya boyutu geçersiz.");
        // Validate everything before replacing state. Do not populate all 207 catalog IDs as game pets.
        var totals = snapshot.Entries.ToDictionary(e => e.PetId, Progress.Total);
        if (stream == snapshot.StreamId && snapshot.FrameId <= lastFrame)
        { log("Eski/tekrar snapshot uygulanmadı."); return; }
        Current.Snapshot = snapshot with { Entries = snapshot.Entries.ToList() };
        Current.LastKnownTotals = totals;
        stream = snapshot.StreamId; lastFrame = snapshot.FrameId; Quality = DataQuality.Exact;
        foreach (var id in totals.Keys) ReportUnknown(id);
        log($"Tam collection eşitlendi: {totals.Count} pet. Sayaçlar kesin.");
    }
    public void Gain(SoulEvent gain)
    {
        if (gain.Scope != Current.Scope) throw new ArgumentException("Soul olayı başka karakter/sunucuya ait.");
        if (gain.PetId <= 0 || gain.Amount <= 0 || gain.Amount >= 1000 || gain.FrameId < 0 || string.IsNullOrWhiteSpace(gain.StreamId))
            throw new ArgumentException("Soul olayı ID, miktar veya frame kimliği geçersiz.");
        if (stream == gain.StreamId && gain.FrameId <= lastFrame)
        { log($"Tekrar/eski frame uygulanmadı: {gain.StreamId}/{gain.FrameId}."); return; }
        if (stream != gain.StreamId) { Gap("Yeni akış için snapshot yok"); stream = gain.StreamId; }
        lastFrame = gain.FrameId;
        Session.Events.Add(gain);
        Session.Souls[gain.PetId] = checked(Session.Souls.GetValueOrDefault(gain.PetId) + gain.Amount);
        Session.Active.Add(gain.PetId); Current.Preferences.Dismissed.Remove(gain.PetId);
        ReportUnknown(gain.PetId);
        if (Quality == DataQuality.Exact)
        {
            // A complete Global collection is sparse: both omitted level and omitted points mean zero.
            // Materialize only after an actual gain confirms a catalogued Pet ID in this same intact stream.
            // Generic snapshots, unknown IDs, cached state and gap/reconnect paths never take this shortcut.
            if (!Current.LastKnownTotals.ContainsKey(gain.PetId) &&
                Current.Snapshot is { OmittedEntriesAreZero: true } baseline && baseline.StreamId == gain.StreamId &&
                catalog.Pets.ContainsKey(gain.PetId))
            {
                Current.LastKnownTotals[gain.PetId] = 0;
                log($"Pet ID {gain.PetId}: doğrulanmış tam collection'da seviye/soul kaydı yok; ilk gerçek soul için başlangıç 0.");
            }
            if (!Current.LastKnownTotals.TryGetValue(gain.PetId, out var total))
                Gap($"Pet ID {gain.PetId} snapshot içinde yok");
            else if (total >= 105) Gap($"MAX pet {gain.PetId} için soul olayı; snapshot ile doğrulayın");
            else
            {
                var updated=checked(total + gain.Amount);Current.LastKnownTotals[gain.PetId]=updated;
                if(total<105 && updated>=105)ReachedMax?.Invoke(gain.PetId);
            }
        }
        log($"Pet ID {gain.PetId}: +{gain.Amount} soul. " + (Quality == DataQuality.Exact ? "Sayaç güncellendi." : "Oturuma kaydedildi; sayaç için tam snapshot gerekiyor."));
    }
    private void ReportUnknown(int id)
    { if (!catalog.Pets.ContainsKey(id)) log($"Bilinmeyen Pet ID: {id}. Başka pete eşlenmedi."); }
}
