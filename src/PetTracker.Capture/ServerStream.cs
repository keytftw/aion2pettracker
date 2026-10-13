using PetTracker.Capture.Protocol;

namespace PetTracker.Capture;

// One already process-owned, server-to-client TCP direction. Frame identity survives retransmissions.
public sealed class ServerStream
{
    private readonly FrameDecoder decoder;
    private readonly TcpReassembler tcp;
    private readonly Action<LiveMessage> emit;
    private long frame;
    private readonly Action<ReadOnlyMemory<byte>>? collectionTap;
    public string StreamId { get; }
    public ServerStream(string streamId, Action<LiveMessage> emit, uint? synSequence = null, Action<ReadOnlyMemory<byte>>? collectionTap = null)
    {
        StreamId = streamId; this.emit = emit; this.collectionTap = collectionTap;
        decoder = new(Handle, startsAligned: synSequence.HasValue);
        decoder.DataLost += () => emit(new IntegrityLost("Frame veya LZ4 paketi kaybı"));
        tcp = new(data => decoder.Feed(data.Span), () => { decoder.Reset(); emit(new IntegrityLost("TCP akışında eksik veri")); });
        if (synSequence is { } seq) tcp.Start(unchecked(seq + 1));
        emit(new StreamBegan(streamId));
    }
    public void Push(uint sequence, ReadOnlySpan<byte> data, long timeMs) => tcp.Push(sequence, data, timeMs);
    public void Invalidate(string reason)
    { decoder.Reset(); emit(new IntegrityLost(reason)); }
    private void Handle(ushort opcode, ReadOnlySpan<byte> body)
    {
        if (opcode == 0x0090 && collectionTap is not null) collectionTap(body.ToArray());
        var message = PetProtocol.Decode(StreamId, ++frame, opcode, body);
        if (message is not null) emit(message);
    }
}
