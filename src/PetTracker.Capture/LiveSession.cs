using PetTracker.Core;

namespace PetTracker.Capture;

// Used only on the UI thread. Never binds a new collection to a stale identity.
public sealed class LiveSession(Tracker tracker, Action<string> log, PetCatalog? catalog = null)
{
    private string? stream;
    private OwnIdentity? identity;
    private OwnIdentity? binding;
    private bool identityFresh;
    private uint? pendingActor;
    private readonly List<LiveMessage> pending = [];
    private readonly Dictionary<uint, int> npcs = [];
    private readonly HashSet<uint> ownTargets = [];
    public bool IdentityKnown => identity is not null;
    public long CollectionsReceived {get;private set;}
    public long CollectionsApplied {get;private set;}
    public string CollectionStatus {get;private set;}="Henüz collection gelmedi.";
    public string Status { get; private set; } = "Canlı okuma durduruldu";
    public void Accept(LiveMessage message)
    {
        switch (message)
        {
            case CaptureNotice note: Status = note.Text; log(note.Text); return;
            case StreamBegan began:
                ClearScene();
                ClearPending("Yeni bağlantı"); stream = began.StreamId; identity = null; binding = null; pendingActor = null; identityFresh = false;
                tracker.Reconnect(); Status = "Oyun kimliği ve collection bekleniyor"; return;
            case StreamEnded ended:
                ClearScene();
                ClearPending("Bağlantı sonu"); identity = null; identityFresh = false; pendingActor = null; stream = null;
                tracker.Gap(ended.Reason); Status = ended.Reason; return;
            case IntegrityLost gap:
                if(!gap.IdentityMayBeLost)
                {
                    // A complete but unsupported soul message does not constitute
                    // a connection or character change. Preserve the known binding
                    // and subsequent verified activity, while totals remain uncertain.
                    tracker.Gap(gap.Reason);
                    if(identity is null)Queue(gap);
                    else Status="Soul verisi doğrulama bekliyor; son bilinen sayaçlar korundu";
                    return;
                }
                ClearScene();
                // Identity itself may have been lost. Keep later gains unassigned until self info arrives.
                tracker.Gap(gap.Reason); identity = null; identityFresh = false; pendingActor = null;
                ClearPending("Veri kaybından önceki kimliği belirsiz kayıtlar"); Queue(gap);
                Status = "Veri kaybı — yeni kimlik ve collection gerekiyor"; return;
            case OwnActor actor when actor.StreamId == stream:
                // A queued new collection is a login boundary; an old binding must not discard it.
                var priorActor = pendingActor ?? identity?.ActorId ?? (pending.OfType<CollectionReceived>().Any() ? null : binding?.ActorId);
                if (priorActor is { } prior && prior != actor.ActorId)
                { ClearScene(); ClearPending("Karakter actor değişti"); tracker.Gap("Karakter değişimi"); identity = null; identityFresh = false; Status = "Yeni karakter kimliği bekleniyor"; }
                pendingActor = actor.ActorId;
                return;
            case OwnIdentity own when own.StreamId == stream:
                log($"[COLLECTION TEST] Güncel kimlik geldi: frame={own.FrameId}, actor={own.ActorId}.");
                if (pendingActor is { } awaited && awaited != own.ActorId) ClearPending("Beklenen actor ile kimlik uyuşmadı");
                if (identity is { } old && old.ActorId != own.ActorId)
                    ClearPending("Karakter kimliği değişti");
                if (binding is null || binding.Scope != own.Scope || binding.ActorId != own.ActorId)
                {
                    if (binding is not null) ClearScene();
                    tracker.Select(own.Scope);
                }
                identity = own; binding = own; pendingActor = own.ActorId; identityFresh = true;
                var queued = pending.ToArray(); pending.Clear();
                foreach (var item in queued) Apply(item);
                Status = "Karakter algılandı: " + own.Scope.Character + " / " + own.Scope.Server;
                return;
            case CollectionReceived full when full.StreamId == stream:
                CollectionsReceived++;
                CollectionStatus=$"Collection alındı: frame={full.FrameId}, pet={full.Entries.Count}; kimlik güncel={identityFresh}.";
                log("[COLLECTION RX] "+CollectionStatus);
                if (!identityFresh)
                {
                    tracker.Gap("Collection için güncel karakter kimliği bekleniyor"); identity = null; pendingActor = null;
                    ClearPending("Yeni collection öncesindeki kimliği belirsiz kayıtlar"); Queue(full); return;
                }
                Apply(full); return;
            case SoulReceived soul when soul.StreamId == stream:
                if (identity is null) Queue(soul); else Apply(soul);
                return;
            case SoulBatchReceived batch when batch.StreamId==stream:
                if(identity is null)Queue(batch);else Apply(batch);
                return;
            case NpcObserved npc when npc.StreamId == stream:
                if (npcs.Count > 4096) ClearScene();
                npcs[npc.ActorId] = npc.NpcCode;
                if (ownTargets.Contains(npc.ActorId)) TrackTarget(npc.ActorId);
                return;
            case TargetObserved target when target.StreamId == stream && identity is { } self && target.ActorId == self.ActorId:
                if (ownTargets.Count > 512) ownTargets.Clear();
                ownTargets.Add(target.TargetId); TrackTarget(target.TargetId); return;
            case EntityGone gone when gone.StreamId == stream:
                npcs.Remove(gone.ActorId); ownTargets.Remove(gone.ActorId); return;
        }
    }
    private void ClearScene() { npcs.Clear(); ownTargets.Clear(); }
    private void TrackTarget(uint target)
    {
        if (identity is not null && catalog is not null && npcs.TryGetValue(target, out var npc) && catalog.PetByNpc.TryGetValue(npc, out var id))
            tracker.ObservePetAttack(id);
    }
    private void Apply(LiveMessage message)
    {
        if (identity is null) return;
        switch (message)
        {
            case CollectionReceived full:
                if (full.IgnoredTrailingBytes > 0)
                    log($"Collection pet listeleri doğrulandı; sondaki {full.IgnoredTrailingBytes} bayt ek alan soul hesabına katılmadı.");
                var prior=tracker.Current.Snapshot;
                var oldTotals=tracker.Current.LastKnownTotals.ToDictionary(pair=>pair.Key,pair=>pair.Value);
                tracker.Synchronize(new(identity.Scope, full.StreamId, full.FrameId, full.Entries, full.OmittedEntriesAreZero)); identityFresh = false;
                if(!ReferenceEquals(prior,tracker.Current.Snapshot))
                {
                    CollectionsApplied++;
                    CollectionStatus=$"Tam collection uygulandı: frame={full.FrameId}, pet={full.Entries.Count}; kesinlik={tracker.Quality}.";
                    log("[COLLECTION APPLY] "+CollectionStatus);
                    foreach(var pair in tracker.Current.LastKnownTotals.Where(pair=>!oldTotals.TryGetValue(pair.Key,out var before)||before!=pair.Value).Take(12))
                        log($"[COLLECTION DIFF] Pet ID {pair.Key}: {(oldTotals.TryGetValue(pair.Key,out var old)?old.ToString():"bilinmiyor")} -> {pair.Value}.");
                }
                else {CollectionStatus=$"Eski/tekrar collection uygulanmadı: frame={full.FrameId}.";log("[COLLECTION SKIP] "+CollectionStatus);}
                break;
            case SoulReceived soul:
                tracker.Gain(new(identity.Scope, soul.StreamId, soul.FrameId, soul.PetId, soul.Amount)); break;
            case SoulBatchReceived batch:
                var eventsBeforeBatch=tracker.Session.Events.Count;
                tracker.GainBatch(batch.Entries.Select((entry,index)=>new SoulEvent(identity.Scope,batch.StreamId,batch.FrameId,entry.PetId,entry.Amount,index)).ToArray());
                log($"[SOUL BATCH {(tracker.Session.Events.Count>eventsBeforeBatch?"APPLY":"SKIP")}] frame={batch.FrameId}; kayıt={batch.Entries.Count}; "+string.Join("; ",batch.Entries.Select(entry=>$"Pet ID {entry.PetId} +{entry.Amount}")));
                break;
            case IntegrityLost gap: tracker.Gap(gap.Reason); break;
        }
    }
    private void Queue(LiveMessage message)
    {
        if (pending.Count >= 512)
        { ClearPending("Kimlik bekleme tamponu doldu"); pending.Add(new IntegrityLost("Kimlik beklerken veri kaybı")); }
        pending.Add(message);
        if (message is SoulReceived soul) log($"Kimlik bekleyen soul: Pet ID {soul.PetId}, +{soul.Amount}; başka karaktere yazılmadı.");
        if(message is SoulBatchReceived batch)log($"Kimlik bekleyen toplu soul: frame={batch.FrameId}, kayıt={batch.Entries.Count}; başka karaktere yazılmadı.");
        Status = "Karakter kimliği bekleniyor; oyunda karaktere tekrar giriş gerekebilir";
    }
    private void ClearPending(string reason)
    {
        if (pending.Count > 0) log($"{reason}: {pending.Count} eşlenemeyen kayıt sayaçlara uygulanamadı; tam collection gerekli.");
        pending.Clear();
    }
}
