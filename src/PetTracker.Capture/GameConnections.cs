// SPDX-License-Identifier: GPL-3.0-only
// Adapted Windows TCP-table lookup from reference GameProcessLocator.cs, limited to port 13328.
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace PetTracker.Capture;

public sealed record GameConnection(int ProcessId, long ProcessStartTicks, IPAddress LocalIp, ushort LocalPort, IPAddress RemoteIp, ushort RemotePort)
{
    public bool IsIncoming(IPAddress source, ushort sourcePort, IPAddress destination, ushort destinationPort) =>
        source.Equals(RemoteIp) && sourcePort == RemotePort && destination.Equals(LocalIp) && destinationPort == LocalPort;
    public string IncomingFilter => $"ip and tcp and src host {RemoteIp} and src port {RemotePort} and dst host {LocalIp} and dst port {LocalPort}";
}
public static class GameConnections
{
    public static List<GameConnection> Find()
    {
        var pids = new Dictionary<int, long>();
        foreach (var name in new[] { "AION2", "AION2-Win64-Shipping" })
            foreach (var p in Process.GetProcessesByName(name))
                using (p) pids[p.Id] = p.StartTime.ToUniversalTime().Ticks;
        if (pids.Count == 0) return [];
        var size = 0;
        var error = GetExtendedTcpTable(IntPtr.Zero, ref size, false, 2, 5, 0);
        if (error != 122 && error != 0) throw new Win32Exception((int)error, "Windows TCP tablosu okunamadı");
        // Table may grow between calls: retry with the actual required size.
        for (var retry = 0; retry < 3; retry++)
        {
            var allocated = size;
            var buffer = Marshal.AllocHGlobal(allocated);
            try
            {
                error = GetExtendedTcpTable(buffer, ref size, false, 2, 5, 0);
                if (error == 122) continue;
                if (error != 0) throw new Win32Exception((int)error, "Windows TCP tablosu okunamadı");
                var count = Marshal.ReadInt32(buffer); var rowSize = Marshal.SizeOf<Row>();
                if (count < 0 || 4L + (long)count * rowSize > allocated) throw new InvalidDataException("TCP tablosu boyutu geçersiz");
                var result = new List<GameConnection>();
                for (var i = 0; i < count; i++)
                {
                    var row = Marshal.PtrToStructure<Row>(buffer + 4 + i * rowSize);
                    if (row.State != 5 || Port(row.RemotePort) != 13328 || !pids.TryGetValue((int)row.Pid, out var started)) continue;
                    result.Add(new((int)row.Pid, started, new(row.LocalAddress), Port(row.LocalPort), new(row.RemoteAddress), Port(row.RemotePort)));
                }
                return result;
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
        throw new IOException("Windows TCP tablosu sürekli değişti; tekrar deneyin");
    }
    private static ushort Port(uint value) => (ushort)((value & 255) << 8 | ((value >> 8) & 255));
    [StructLayout(LayoutKind.Sequential)]
    private struct Row { public uint State, LocalAddress, LocalPort, RemoteAddress, RemotePort, Pid; }
    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(IntPtr table, ref int size, [MarshalAs(UnmanagedType.Bool)] bool order, int family, int tableClass, uint reserved);
}
