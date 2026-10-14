using System.Buffers.Binary;
using System.Net;
using System.Text;
using K4os.Compression.LZ4;
using PetTracker.Capture;
using PetTracker.Capture.Protocol;
using PetTracker.Core;

namespace PetTracker.Tests;

public sealed class CaptureTests
{
    private static byte[] U32(uint value) { var b = new byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(b, value); return b; }
    private static byte[] Varint(uint value)
    {
        var bytes = new List<byte>();
        do { var b = (byte)(value & 127); value >>= 7; bytes.Add((byte)(b | (value != 0 ? 128 : 0))); } while (value != 0);
        return bytes.ToArray();
    }
    private static byte[] Collection(int level = 0, int souls = 4) => [0, 0, 0, 0, 1, .. U32(1002), .. U32(1002), .. U32((uint)level), 1, .. U32(1002), .. U32((uint)souls)];
    private static byte[] Soul(int id = 1002, int amount = 1) => [1, .. U32((uint)id), .. U32((uint)amount)];
    private static byte[] Identity(string name = "Karakter", uint actor = 15, ushort server = 2305)
    {
        var text = Encoding.UTF8.GetBytes(name);
        return [.. Varint(actor), 0, 0, 0, 0, 1, (byte)text.Length, .. text, (byte)server, (byte)(server >> 8), .. U32(5)];
    }
    private static byte[] Frame(ushort opcode, byte[] body) => [.. Varint((uint)(body.Length + 6)), (byte)(opcode >> 8), (byte)opcode, .. body];
    private static byte[] Bundle(byte[] inner)
    {
        var compressed = new byte[LZ4Codec.MaximumOutputSize(inner.Length)];
        var len = LZ4Codec.Encode(inner, compressed);
        return Frame(0xffff, [.. U32((uint)inner.Length), .. compressed[..len]]);
    }
    private static (Tracker Tracker, LiveSession Session, List<string> Logs) Setup()
    {
        var logs = new List<string>(); var t = new Tracker(new([new(1002, "Pet", "Fera")]), new(), logs.Add);
        return (t, new(t, logs.Add), logs);
    }
    [Fact]
    public void ValidCollectionAndSoulDecodeNumericId()
    {
        var c = Assert.IsType<CollectionReceived>(PetProtocol.Decode("s", 1, 0x0090, Collection(2, 46)));
        Assert.Equal(new CollectionEntry(1002, 2, 46), c.Entries.Single());
        Assert.True(c.OmittedEntriesAreZero);
        var soul = Assert.IsType<SoulReceived>(PetProtocol.Decode("s", 2, 0x0d90, Soul(99999, 2)));
        Assert.Equal(99999, soul.PetId); Assert.Equal(2, soul.Amount);
    }
    [Fact]
    public void ShardsAttacksAndOtherPlayerIdentityAreIgnored()
    {
        foreach (var opcode in new ushort[] { 0x1190, 0x0438, 0x0238, 0x048d, 0x4536 })
            Assert.Null(PetProtocol.Decode("s", 1, opcode, Soul()));
    }
    [Fact]
    public void MalformedCollectionCannotBecomeAuthoritative()
    {
        var data = Collection();
        for (var length = 0; length < data.Length; length++)
            Assert.IsType<IntegrityLost>(PetProtocol.Decode("s", 1, 0x0090, data[..length]));
        Assert.IsType<IntegrityLost>(PetProtocol.Decode("s", 1, 0x0090, Collection(1, 25)));
        Assert.Equal(1, Assert.IsType<CollectionReceived>(PetProtocol.Decode("s", 1, 0x0090, [.. data, 1])).IgnoredTrailingBytes);
        Assert.IsType<IntegrityLost>(PetProtocol.Decode("s", 1, 0x0d90, Soul(amount: 0)));
    }
    [Fact]
    public void InvalidVarintOverflowIsRejected()
    {
        var at = 0;
        Assert.False(Wire.TryVarint(new byte[] { 255, 255, 255, 255, 16 }, ref at, out _));
        Assert.Equal(0, at);
    }
    [Fact]
    public void CompletePetListsAcceptExtraFieldsWithoutCountingThem()
    {
        var bytes = Collection().Concat(new byte[] { 3, 0, 1 }).ToArray();
        var entries = PetProtocol.ReadCollection(bytes, out var reason, out var trailing);
        Assert.Equal(new CollectionEntry(1002, 0, 4), Assert.Single(entries!));
        Assert.Equal(3, trailing); Assert.Equal("", reason);
        Assert.NotNull(PetProtocol.ReadCollection(Collection(), out reason)); Assert.Equal("", reason);
    }
    [Fact]
    public void GlobalSizedListsWith1536ByteSuffixReconcileWithoutInventingProgress()
    {
        var body = new List<byte>(new byte[4]); body.AddRange(Varint(96));
        for (var i = 0; i < 96; i++)
        { body.AddRange(U32((uint)(1000 + i))); body.AddRange(U32((uint)(1000 + i))); body.AddRange(U32((uint)(i % 4))); }
        body.AddRange(Varint(103));
        for (var i = 0; i < 103; i++)
        { body.AddRange(U32((uint)(1000 + i))); body.AddRange(U32((uint)(i < 96 && i % 4 == 3 ? 0 : 1))); }
        var petSection = body.ToArray(); body.AddRange(Enumerable.Repeat((byte)0xFE, 1536));
        var full = Assert.IsType<CollectionReceived>(PetProtocol.Decode("s", 2, 0x0090, body.ToArray()));
        Assert.Equal(103, full.Entries.Count); Assert.Equal(1536, full.IgnoredTrailingBytes);
        Assert.Null(PetProtocol.ReadCollection(petSection[..^1]));
        var (tracker, session, _) = Setup(); session.Accept(new StreamBegan("s"));
        session.Accept(full); session.Accept(new OwnIdentity("s", 3, 150, new("Synthetic", "2312")));
        Assert.Equal(DataQuality.Exact, tracker.Quality); Assert.Equal(31L, tracker.KnownTotal(1002));
        Assert.Equal(1L, tracker.KnownTotal(1102)); Assert.Empty(tracker.Session.Events);
    }
    [Fact]
    public void OptionalDiagnosticTapSeesOnlyCollectionBody()
    {
        var bodies = new List<byte[]>();
        var stream = new ServerStream("s", _ => { }, 0, body => bodies.Add(body.ToArray()));
        var bytes = Frame(0x3336, Identity()).Concat(Frame(0x0090, Collection())).Concat(Frame(0x0d90, Soul())).ToArray();
        stream.Push(1, bytes, 0);
        Assert.Equal(Collection(), Assert.Single(bodies));
    }
    [Fact]
    public void SelfIdentityRequiresValidServerAndClass()
    {
        var id = Assert.IsType<OwnIdentity>(PetProtocol.Decode("s", 1, 0x3336, Identity("Kahraman")));
        Assert.Equal(new CharacterScope("Kahraman", "2305"), id.Scope);
        Assert.IsType<IntegrityLost>(PetProtocol.Decode("s", 1, 0x3336, Identity(server: 12)));
        Assert.IsType<IntegrityLost>(PetProtocol.Decode("s", 1, 0x3336, Identity("$Demo")));
    }
    [Fact]
    public void SplitAndRetransmittedTcpFramesCountTwoRealGains()
    {
        var (t, session, _) = Setup(); var stream = new ServerStream("s", session.Accept, 100);
        var bytes = new byte[] { }.Concat(Frame(0x3336, Identity())).Concat(Frame(0x0090, Collection())).Concat(Frame(0x0d90, Soul())).Concat(Frame(0x0d90, Soul())).ToArray();
        stream.Push(101, bytes[..7], 0); stream.Push(108, bytes[7..], 1); stream.Push(101, bytes, 2);
        Assert.Equal(6L, t.KnownTotal(1002)); Assert.Equal(2L, t.Session.Souls[1002]); Assert.Equal(DataQuality.Exact, t.Quality);
    }
    [Fact]
    public void OutOfOrderSegmentsReassembleWithoutGap()
    {
        var messages = new List<LiveMessage>(); var stream = new ServerStream("s", messages.Add, 10);
        var frame = Frame(0x0d90, Soul());
        stream.Push(16, frame[5..], 0); stream.Push(11, frame[..5], 100);
        Assert.Single(messages.OfType<SoulReceived>()); Assert.Empty(messages.OfType<IntegrityLost>());
    }
    [Fact]
    public void TcpGapMakesCountersUncertainAndRequiresNewIdentity()
    {
        var (t, session, _) = Setup(); var stream = new ServerStream("s", session.Accept, 100);
        var start = Frame(0x3336, Identity()).Concat(Frame(0x0090, Collection())).ToArray(); stream.Push(101, start, 0);
        var afterGap = Frame(0x0036, new byte[8]).Concat(Frame(0x0d90, Soul())).ToArray();
        var sequence = (uint)(101 + start.Length + 10);
        stream.Push(sequence, afterGap, 1); stream.Push(sequence, afterGap, 2000);
        Assert.Equal(DataQuality.Uncertain, t.Quality); Assert.Equal(4L, t.KnownTotal(1002)); Assert.False(session.IdentityKnown);
        Assert.Empty(t.Session.Events);
        stream.Push(sequence + (uint)afterGap.Length, Frame(0x3336, Identity()), 2100);
        Assert.Single(t.Session.Events); Assert.Equal(5L, t.KnownTotal(1002));
        Assert.Equal(DataQuality.Uncertain, t.Quality);
    }
    [Fact]
    public void CompressedBundleDeliversOrderedDistinctFrames()
    {
        var (t, session, _) = Setup(); var stream = new ServerStream("s", session.Accept, 1);
        var inner = Frame(0x3336, Identity()).Concat(Frame(0x0090, Collection())).Concat(Frame(0x0d90, Soul())).Concat(Frame(0x0d90, Soul())).ToArray();
        var compressed = Bundle(inner); stream.Push(2, compressed, 0);
        Assert.Equal(6L, t.KnownTotal(1002)); Assert.Equal(2, t.Session.Events.Count);
        Assert.NotEqual(t.Session.Events[0].FrameId, t.Session.Events[1].FrameId);
    }
    [Fact]
    public void ActualAshenSpiderGainAfterSparseLoginCountsExactlyByPetId()
    {
        var t = new Tracker(new([new(1114, "Ashen Spider", "Fera")]), new(), _ => { });
        var session = new LiveSession(t, _ => { }); var stream = new ServerStream("s", session.Accept, 1);
        var bytes = Frame(0x0090, Collection()).Concat(Frame(0x3336, Identity()))
            .Concat(Frame(0x0d90, Soul(1114))).Concat(Frame(0x0d90, Soul(1114))).ToArray();
        stream.Push(2, bytes, 0);
        Assert.Equal(DataQuality.Exact, t.Quality); Assert.Equal(2L, t.KnownTotal(1114));
        Assert.Equal(2L, t.Session.Souls[1114]); Assert.Equal([1114], t.VisibleIds);
    }
    [Fact]
    public void BadBundleFlagsLoss()
    {
        var messages = new List<LiveMessage>(); var stream = new ServerStream("s", messages.Add, 0);
        stream.Push(1, Frame(0xffff, [.. U32(0), 1]), 0);
        Assert.Contains(messages, m => m is IntegrityLost);
    }
    [Fact]
    public void MidStreamStartsHuntingAndDoesNotInventFrames()
    {
        var messages = new List<LiveMessage>(); var stream = new ServerStream("s", messages.Add);
        var bytes = Frame(0x0d90, Soul()).Concat(Frame(0x0036, new byte[8])).Concat(Frame(0x0d90, Soul())).ToArray();
        stream.Push(1, bytes, 0); Assert.Single(messages.OfType<SoulReceived>());
    }
    [Fact]
    public void CollectionBeforeIdentityQueuesAndThenReconciles()
    {
        var (t, session, _) = Setup(); session.Accept(new StreamBegan("s"));
        session.Accept(new CollectionReceived("s", 1, [new(1002, 0, 4)]));
        session.Accept(new SoulReceived("s", 2, 1002, 1)); Assert.False(t.HasCurrent);
        session.Accept(new OwnIdentity("s", 3, 15, new("Karakter", "2305")));
        Assert.Equal(5L, t.KnownTotal(1002)); Assert.Single(t.Session.Events); Assert.Equal(DataQuality.Exact, t.Quality);
    }
    [Fact]
    public void CollectionWithStaleIdentityWaitsForNewCharacter()
    {
        var (t, session, _) = Setup(); var first = new CharacterScope("Birinci", "2305"); var second = new CharacterScope("Ikinci", "2305");
        session.Accept(new StreamBegan("s")); session.Accept(new OwnIdentity("s", 1, 15, first));
        session.Accept(new CollectionReceived("s", 2, [new(1002, 0, 4)])); t.Pin(1002, true);
        session.Accept(new CollectionReceived("s", 3, [new(1002, 2, 46)]));
        Assert.Equal(4L, t.KnownTotal(1002)); Assert.Equal(DataQuality.Uncertain, t.Quality);
        session.Accept(new OwnActor("s", 25));
        session.Accept(new OwnIdentity("s", 4, 25, second));
        Assert.Equal(76L, t.KnownTotal(1002)); Assert.Empty(t.VisibleIds);
        t.Select(first); Assert.Equal(4L, t.KnownTotal(1002)); Assert.Contains(1002, t.VisibleIds);
    }
    [Fact]
    public void ActorChangeSuspendsBindingAndDoesNotAttributeToOldCharacter()
    {
        var (t, session, _) = Setup(); session.Accept(new StreamBegan("s"));
        session.Accept(new OwnIdentity("s", 1, 15, new("Birinci", "2305"))); session.Accept(new CollectionReceived("s", 2, [new(1002, 0, 4)]));
        session.Accept(new OwnActor("s", 99)); session.Accept(new SoulReceived("s", 3, 1002, 1));
        Assert.Equal(4L, t.KnownTotal(1002)); Assert.False(session.IdentityKnown);
        session.Accept(new OwnIdentity("s", 4, 99, new("Ikinci", "2305")));
        Assert.Null(t.KnownTotal(1002)); Assert.Single(t.Session.Events);
    }
    [Fact]
    public void GapKeepsSessionActivityForConfirmedSameCharacter()
    {
        var (t, session, _) = Setup(); var scope = new CharacterScope("Birinci", "2305");
        session.Accept(new StreamBegan("s")); session.Accept(new OwnIdentity("s", 1, 15, scope));
        session.Accept(new CollectionReceived("s", 2, [new(1002, 0, 1)])); session.Accept(new SoulReceived("s", 3, 1002, 1));
        session.Accept(new IntegrityLost("drop")); session.Accept(new SoulReceived("s", 4, 1002, 1));
        session.Accept(new OwnIdentity("s", 5, 15, scope));
        Assert.Equal(3L, t.KnownTotal(1002)); Assert.Equal(2L, t.Session.Souls[1002]); Assert.Equal(DataQuality.Uncertain, t.Quality);
    }
    [Fact]
    public void WrongStreamCannotChangeCounters()
    {
        var (t, session, _) = Setup(); session.Accept(new StreamBegan("s"));
        session.Accept(new OwnIdentity("s", 1, 15, new("Birinci", "2305")));
        session.Accept(new CollectionReceived("s", 2, [new(1002, 0, 1)]));
        session.Accept(new SoulReceived("other", 3, 1002, 1)); Assert.Equal(1L, t.KnownTotal(1002));
    }
    [Fact]
    public void ActorChangeAfterGapDoesNotMoveOldUnassignedGainsToNewCharacter()
    {
        var (t, session, logs) = Setup(); session.Accept(new StreamBegan("s"));
        session.Accept(new OwnIdentity("s", 1, 150, new("Birinci", "2305")));
        session.Accept(new CollectionReceived("s", 2, [new(1002, 0, 1)]));
        session.Accept(new IntegrityLost("drop")); session.Accept(new SoulReceived("s", 3, 1002, 1));
        session.Accept(new OwnActor("s", 990));
        session.Accept(new OwnIdentity("s", 4, 990, new("Ikinci", "2305")));
        Assert.Empty(t.Session.Events); Assert.Null(t.KnownTotal(1002));
        Assert.Contains(logs, m => m.Contains("eşlenemeyen kayıt"));
    }
    [Fact]
    public void IncomingFilterAndDirectionExcludeOutgoingAndOtherConnections()
    {
        var remote = IPAddress.Parse("203.0.113.10"); var local = IPAddress.Parse("192.0.2.4");
        var c = new GameConnection(1, 1, local, 50000, remote, 13328);
        Assert.True(c.IsIncoming(remote, 13328, local, 50000)); Assert.False(c.IsIncoming(local, 50000, remote, 13328));
        Assert.False(c.IsIncoming(remote, 13328, local, 50001));
        Assert.Contains("src host 203.0.113.10", c.IncomingFilter); Assert.Contains("dst port 50000", c.IncomingFilter);
    }
    [Fact]
    public void UnassignedQueueOverflowIsLoggedAndRequiresResync()
    {
        var (t, session, logs) = Setup(); session.Accept(new StreamBegan("s"));
        for (var i = 0; i < 520; i++) session.Accept(new SoulReceived("s", i, 1002, 1));
        Assert.Contains(logs, m => m.Contains("tamponu doldu")); Assert.False(t.HasCurrent);
        session.Accept(new OwnIdentity("s", 521, 15, new("Birinci", "2305")));
        Assert.Equal(DataQuality.Uncertain, t.Quality); Assert.Null(t.KnownTotal(1002));
    }
}
