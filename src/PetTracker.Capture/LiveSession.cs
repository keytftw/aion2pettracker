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
                if (!identityFresh)
                {
                    tracker.Gap("Collection için güncel karakter kimliği bekleniyor"); identity = null; pendingActor = null;
                    ClearPending("Yeni collection öncesindeki kimliği belirsiz kayıtlar"); Queue(full); return;
                }
                Apply(full); return;
            case SoulReceived soul when soul.StreamId == stream:
                if (identity is null) Queue(soul); else Apply(soul);
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
                tracker.Synchronize(new(identity.Scope, full.StreamId, full.FrameId, full.Entries, full.OmittedEntriesAreZero)); identityFresh = false; break;
            case SoulReceived soul:
                tracker.Gain(new(identity.Scope, soul.StreamId, soul.FrameId, soul.PetId, soul.Amount)); break;
            case IntegrityLost gap: tracker.Gap(gap.Reason); break;
        }
    }
    private void Queue(LiveMessage message)
    {
        if (pending.Count >= 512)
        { ClearPending("Kimlik bekleme tamponu doldu"); pending.Add(new IntegrityLost("Kimlik beklerken veri kaybı")); }
        pending.Add(message);
        if (message is SoulReceived soul) log($"Kimlik bekleyen soul: Pet ID {soul.PetId}, +{soul.Amount}; başka karaktere yazılmadı.");
        Status = "Karakter kimliği bekleniyor; oyunda karaktere tekrar giriş gerekebilir";
    }
    private void ClearPending(string reason)
    {
        if (pending.Count > 0) log($"{reason}: {pending.Count} eşlenemeyen kayıt sayaçlara uygulanamadı; tam collection gerekli.");
        pending.Clear();
    }
}
