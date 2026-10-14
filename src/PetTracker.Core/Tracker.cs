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
        =>GainBatch([gain]);
    public void GainBatch(IReadOnlyList<SoulEvent> gains)
    {
        if(gains.Count is <1 or >512)throw new ArgumentException("Soul kayıt sayısı geçersiz.");
        var gain=gains[0];
        foreach(var entry in gains)
        {
            if(entry.Scope!=Current.Scope||entry.Scope!=gain.Scope||entry.StreamId!=gain.StreamId||entry.FrameId!=gain.FrameId)
                throw new ArgumentException("Soul kayıtları aynı karakter, akış ve frame'e ait olmalı.");
            if(entry.PetId<=0||entry.Amount<=0||entry.Amount>=1000||entry.FrameId<0||string.IsNullOrWhiteSpace(entry.StreamId))
                throw new ArgumentException("Soul olayı ID, miktar veya frame kimliği geçersiz.");
        }
        // Validate all entries and arithmetic before changing any state.
        foreach(var group in gains.GroupBy(entry=>entry.PetId))
        {var amount=group.Sum(entry=>(long)entry.Amount);_=checked(Session.Souls.GetValueOrDefault(group.Key)+amount);_=checked(Current.LastKnownTotals.GetValueOrDefault(group.Key)+amount);}
        if (stream == gain.StreamId && gain.FrameId <= lastFrame)
        { log($"Tekrar/eski frame uygulanmadı: {gain.StreamId}/{gain.FrameId}."); return; }
        if (stream != gain.StreamId) { Gap("Yeni akış için snapshot yok"); stream = gain.StreamId; }
        lastFrame = gain.FrameId;
        for(var index=0;index<gains.Count;index++)ApplyGain(gains[index] with {RecordIndex=index});
    }
    private void ApplyGain(SoulEvent gain)
    {
        Session.Events.Add(gain);
        Session.Souls[gain.PetId] = checked(Session.Souls.GetValueOrDefault(gain.PetId) + gain.Amount);
        Session.Active.Add(gain.PetId); Current.Preferences.Dismissed.Remove(gain.PetId);
        ReportUnknown(gain.PetId);
        if (Current.Snapshot is not null)
        {
            // Continue observed gains from the last collection baseline even after
            // uncertainty. Missing gains are never guessed and Quality stays Uncertain
            // until a new authoritative snapshot replaces these provisional totals.
            if (!Current.LastKnownTotals.ContainsKey(gain.PetId) &&
                Current.Snapshot is { OmittedEntriesAreZero: true } baseline && baseline.StreamId == gain.StreamId &&
                catalog.Pets.ContainsKey(gain.PetId))
            {
                Current.LastKnownTotals[gain.PetId] = 0;
                log($"Pet ID {gain.PetId}: son tam collection'da seviye/soul kaydı yok; başlangıç 0. Kesinlik: {Quality}.");
            }
            if (!Current.LastKnownTotals.TryGetValue(gain.PetId, out var total))
                Gap($"Pet ID {gain.PetId} snapshot içinde yok");
            else if (total >= 105) Gap($"MAX pet {gain.PetId} için soul olayı; snapshot ile doğrulayın");
            else
            {
                var observed=checked(total + gain.Amount);Current.LastKnownTotals[gain.PetId]=observed;
                if(Quality==DataQuality.Exact&&total<105 && observed>=105)ReachedMax?.Invoke(gain.PetId);
            }
        }
        log($"Pet ID {gain.PetId}: +{gain.Amount} soul. " + (Quality == DataQuality.Exact ? "Sayaç güncellendi." : "Oturuma kaydedildi; mevcut sayaç kesin değil, collection ile doğrulayın."));
    }
    private void ReportUnknown(int id)
    { if (!catalog.Pets.ContainsKey(id)) log($"Bilinmeyen Pet ID: {id}. Başka pete eşlenmedi."); }
}
