using System.Net;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace PetTracker.Capture;

// No SendPacket, game input, raw packet recording, injection or memory access.
// A single process-owned IPv4/13328 connection is accepted; only its server direction enters the decoder.
public sealed class PassiveCapture(Action<LiveMessage> emit, Action<ReadOnlyMemory<byte>>? collectionTap = null) : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly CancellationTokenSource cancellation = new();
    private Task? worker;
    private LibPcapLiveDevice? device;
    private GameConnection? connection;
    private ServerStream? pipeline;
    private uint? synSequence;
    private long drops;
    private string? lastNotice;
    private bool statsUnavailable;
    public static bool NpcapPresent => File.Exists(Path.Combine(Environment.SystemDirectory, "Npcap", "wpcap.dll")) || File.Exists(Path.Combine(Environment.SystemDirectory, "wpcap.dll"));
    public bool Active=>worker is not null&&!worker.IsCompleted;
    public bool Listening { get { lock(gate)return connection is not null&&device is not null; } }
    public void Start()
    {
        if (worker is not null) throw new InvalidOperationException("Canlı okuma zaten başladı");
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Canlı okuma yalnızca Windows içindir");
        worker = Task.Run(Run);
    }
    public void RequestStop() => cancellation.Cancel();
    private async Task Run()
    {
        try
        {
            if (!NpcapPresent) throw new InvalidOperationException("Npcap bulunamadı. Resmi npcap.com sayfasından kendiniz kurun.");
            while (!cancellation.IsCancellationRequested)
            {
                var connections = GameConnections.Find();
                var next = connections.Count == 1 ? connections[0] : null;
                if (next != connection)
                {
                    CloseDevice();
                    if (next is not null) OpenDevice(next);
                }
                if (next is null) Notice(connections.Count > 1 ? "Birden fazla oyun bağlantısı var; karakter karışmaması için okuma bekliyor" : "Aion 2 IPv4 oyun bağlantısı bekleniyor (13328)");
                else CheckDrops();
                await Task.Delay(1000, cancellation.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception ex)
        {
            emit(new IntegrityLost("Canlı okuma hatası: " + ex.Message));
            Notice("Canlı okuma durdu: " + ex.Message + " (Npcap erişimi/bağdaştırıcı izinlerini kontrol edin)");
        }
        finally { CloseDevice(); emit(new StreamEnded("Canlı okuma durduruldu; sayaçlar son bilinen durumda")); }
    }
    private void OpenDevice(GameConnection next)
    {
        var all = CaptureDeviceList.Instance.OfType<LibPcapLiveDevice>().ToArray();
        var chosen = all.FirstOrDefault(d => IPAddress.IsLoopback(next.LocalIp) || IPAddress.IsLoopback(next.RemoteIp)
            ? d.Name.Contains("Loopback", StringComparison.OrdinalIgnoreCase)
            : d.Addresses.Any(a => next.LocalIp.Equals(a.Addr?.ipAddress)));
        if (chosen is null) throw new InvalidOperationException("Oyun bağlantısının ağ bağdaştırıcısı bulunamadı. VPN/IPv6 bağlantısı bu sürümde desteklenmiyor.");
        device = chosen;
        chosen.Open(new DeviceConfiguration { Mode = DeviceModes.None, ReadTimeout = 100, BufferSize = 8 * 1024 * 1024 });
        chosen.Filter = next.IncomingFilter;
        lock (gate) { connection = next; pipeline = null; synSequence = null; drops = 0; statsUnavailable = false; }
        chosen.OnPacketArrival += PacketArrived;
        chosen.StartCapture();
        Notice("Pasif sunucu → istemci okuma başladı; oyunda karaktere giriş yapın");
    }
    private void PacketArrived(object sender, PacketCapture capture)
    {
        try
        {
            var raw = capture.GetPacket();
            var packet = Packet.ParsePacket(raw.LinkLayerType, raw.Data);
            var ip = packet.Extract<IPPacket>(); var tcp = packet.Extract<TcpPacket>();
            if (ip is null || tcp is null) return;
            lock (gate)
            {
                if (connection is null || !connection.IsIncoming(ip.SourceAddress, tcp.SourcePort, ip.DestinationAddress, tcp.DestinationPort)) return;
                if (tcp.Synchronize)
                {
                    // Duplicate SYN does not reset an existing stream. A different ISN is a new incarnation.
                    if (synSequence != tcp.SequenceNumber)
                    { synSequence = tcp.SequenceNumber; pipeline = new(Guid.NewGuid().ToString("N"), emit, synSequence, collectionTap); }
                }
                else pipeline ??= new(Guid.NewGuid().ToString("N"), emit, collectionTap: collectionTap);
                var payload = tcp.PayloadData ?? [];
                var time = (long)raw.Timeval.Seconds * 1000 + (long)raw.Timeval.MicroSeconds / 1000;
                if (payload.Length > 0) pipeline!.Push(unchecked(tcp.SequenceNumber + (tcp.Synchronize ? 1u : 0u)), payload, time);
                if (tcp.Finished || tcp.Reset)
                { emit(new StreamEnded("Oyun bağlantısı kapandı; yeni kimlik ve collection bekleniyor")); pipeline = null; synSequence = null; }
            }
        }
        catch (Exception ex)
        {
            lock (gate) { pipeline?.Invalidate("Paket ayrıştırma hatası: " + ex.Message); }
            emit(new IntegrityLost("Paket okunamadı: " + ex.Message));
        }
    }
    private void CheckDrops()
    {
        if (device is null) return;
        try
        {
            var stats = device.Statistics;
            var count = (long)stats.DroppedPackets + stats.InterfaceDroppedPackets;
            lock (gate)
            {
                if (count > drops) pipeline?.Invalidate($"Npcap {count - drops} paket düşürdü");
                drops = count;
            }
        }
        catch (Exception ex)
        {
            if (statsUnavailable) return;
            statsUnavailable = true;
            emit(new IntegrityLost("Npcap kayıp istatistiği okunamadı: " + ex.Message));
            RequestStop(); // Without health monitoring we cannot claim an exact live stream.
        }
    }
    private void CloseDevice()
    {
        var old = device; device = null;
        if (old is not null)
        {
            try { old.StopCapture(); } catch (Exception ex) { emit(new IntegrityLost("Okuma durdurma hatası: " + ex.Message)); }
            old.OnPacketArrival -= PacketArrived;
            try { old.Close(); } catch (Exception ex) { emit(new IntegrityLost("Bağdaştırıcı kapanış hatası: " + ex.Message)); }
            emit(new StreamEnded("Bağlantı/bağdaştırıcı değişti; sayaçlar doğrulama bekliyor"));
        }
        lock (gate) { connection = null; pipeline = null; synSequence = null; }
    }
    private void Notice(string text)
    { if (lastNotice == text) return; lastNotice = text; emit(new CaptureNotice(text)); }
    public async ValueTask DisposeAsync()
    {
        await cancellation.CancelAsync();
        if (worker is not null) await worker.ConfigureAwait(false);
        cancellation.Dispose();
    }
}
