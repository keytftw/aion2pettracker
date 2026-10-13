using PetTracker.Core;

namespace PetTracker.Capture;

public abstract record LiveMessage;
public sealed record StreamBegan(string StreamId) : LiveMessage;
public sealed record StreamEnded(string Reason) : LiveMessage;
public sealed record CaptureNotice(string Text) : LiveMessage;
public sealed record IntegrityLost(string Reason) : LiveMessage;
public sealed record OwnIdentity(string StreamId, long FrameId, uint ActorId, CharacterScope Scope) : LiveMessage;
public sealed record OwnActor(string StreamId, uint ActorId) : LiveMessage;
public sealed record CollectionReceived(string StreamId, long FrameId, List<CollectionEntry> Entries, int IgnoredTrailingBytes = 0, bool OmittedEntriesAreZero = false) : LiveMessage;
public sealed record SoulReceived(string StreamId, long FrameId, int PetId, int Amount) : LiveMessage;
public sealed record NpcObserved(string StreamId, uint ActorId, int NpcCode) : LiveMessage;
public sealed record TargetObserved(string StreamId, uint ActorId, uint TargetId) : LiveMessage;
public sealed record EntityGone(string StreamId, uint ActorId) : LiveMessage;
