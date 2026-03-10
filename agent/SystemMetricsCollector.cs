using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Web.Administration;

namespace IIS_Site_Manager.Agent;

public sealed class SystemMetricsCollector : IDisposable
{
    readonly object _sync = new();
    readonly PerformanceCounter? _cpuCounter;
    readonly PerformanceCounter[] _networkCounters;

    public SystemMetricsCollector()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue();
        }
        catch
        {
            _cpuCounter = null;
        }

        try
        {
            var category = new PerformanceCounterCategory("Network Interface");
            var names = category.GetInstanceNames().Where(n => !n.Contains("Loopback", StringComparison.OrdinalIgnoreCase));
            _networkCounters = names
                .Select(name => new PerformanceCounter("Network Interface", "Bytes Total/sec", name))
                .ToArray();

            foreach (var counter in _networkCounters)
                _ = counter.NextValue();
        }
        catch
        {
            _networkCounters = [];
        }
    }

    public MetricsSnapshot Collect()
    {
        lock (_sync)
        {
            var cpu = GetCpuPercent();
            var memory = GetMemoryPercent();
            var bytesTotal = GetBytesPerSecond();
            var iisSiteCount = GetIisSiteCount();
            return new MetricsSnapshot(cpu, memory, bytesTotal, iisSiteCount, true);
        }
    }

    double GetCpuPercent()
    {
        var wmiCpu = GetCpuViaWmi();
        if (wmiCpu >= 0) return wmiCpu;

        if (_cpuCounter is null) return 0;
        try
        {
            return Math.Round(_cpuCounter.NextValue(), 2);
        }
        catch
        {
            return 0;
        }
    }

    static double GetCpuViaWmi()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT PercentProcessorTime FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name='_Total'");
            foreach (var item in searcher.Get())
            {
                if (item["PercentProcessorTime"] is not null)
                    return Math.Round(Convert.ToDouble(item["PercentProcessorTime"]), 2);
            }
        }
        catch
        {
            // fall through
        }

        return -1;
    }

    static double GetMemoryPercent()
    {
        var mem = new MemoryStatusEx();
        mem.Length = (uint)Marshal.SizeOf(mem);
        if (!GlobalMemoryStatusEx(ref mem) || mem.TotalPhys == 0) return 0;

        var used = mem.TotalPhys - mem.AvailPhys;
        return Math.Round((double)used / mem.TotalPhys * 100, 2);
    }

    double GetBytesPerSecond()
    {
        if (_networkCounters.Length == 0) return 0;

        var total = 0d;
        foreach (var counter in _networkCounters)
        {
            try
            {
                total += counter.NextValue();
            }
            catch
            {
                // ignore one bad interface and keep others
            }
        }

        return Math.Round(total, 2);
    }

    static int GetIisSiteCount()
    {
        try
        {
            using var manager = new ServerManager();
            return manager.Sites.Count;
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose()
    {
        _cpuCounter?.Dispose();
        foreach (var counter in _networkCounters)
            counter.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);
}
