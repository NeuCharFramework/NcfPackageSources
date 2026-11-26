/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: HostMetricsCollector.cs
    File description: Related implementation.

    Created by: Senparc - 20260806

    Modified by: Senparc - 20260808
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public sealed class HostMetricsSnapshot
    {
        public DateTimeOffset SampledAt { get; set; }
        public string HostName { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public double? CpuUsagePercent { get; set; }
        public long MemoryTotalBytes { get; set; }
        public long MemoryUsedBytes { get; set; }
        public long MemoryAvailableBytes { get; set; }
        public double? MemoryUsagePercent { get; set; }
        public double? NetworkReceiveBytesPerSecond { get; set; }
        public double? NetworkSendBytesPerSecond { get; set; }
        public long NetworkReceiveTotalBytes { get; set; }
        public long NetworkSendTotalBytes { get; set; }
        public int NetworkInterfaceCount { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public double? ProcessCpuUsagePercent { get; set; }
        public long ProcessWorkingSetBytes { get; set; }
        public long ApplicationUptimeSeconds { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public sealed class HostMetricsCollector
    {
        private readonly object _syncRoot = new();
        private CpuCounterSnapshot? _previousCpu;
        private NetworkCounterSnapshot? _previousNetwork;
        private ProcessCounterSnapshot? _previousProcess;

        /// <summary>
        /// Collects a synchronized snapshot of host CPU, physical memory, network,
        /// and current-process metrics. Rate-based values are populated only after
        /// a previous sample has established a valid counter baseline.
        /// </summary>
        /// <returns>A snapshot containing supported metrics and non-fatal sampling warnings.</returns>
        public HostMetricsSnapshot Collect()
        {
            lock (_syncRoot)
            {
                var snapshot = new HostMetricsSnapshot
                {
                    SampledAt = DateTimeOffset.UtcNow,
                    HostName = Environment.MachineName,
                    OperatingSystem = RuntimeInformation.OSDescription
                };

                CollectCpu(snapshot);
                CollectMemory(snapshot);
                CollectNetwork(snapshot);
                CollectProcess(snapshot);

                return snapshot;
            }
        }

        /// <summary>
        /// Adds host CPU utilization calculated from the platform counter delta.
        /// Unsupported platforms, counter resets, and sampling failures are reported
        /// as warnings on the supplied snapshot instead of escaping to the caller.
        /// </summary>
        /// <param name="snapshot">The snapshot receiving CPU values and warnings.</param>
        private void CollectCpu(HostMetricsSnapshot snapshot)
        {
            try
            {
                if (!TryReadCpuCounters(out var current))
                {
                    snapshot.Warnings.Add("当前操作系统暂不支持 Host CPU 采样。");
                    return;
                }

                if (_previousCpu.HasValue)
                {
                    var previous = _previousCpu.Value;
                    if (current.Total >= previous.Total && current.Idle >= previous.Idle)
                    {
                        var totalDelta = current.Total - previous.Total;
                        var idleDelta = current.Idle - previous.Idle;
                        if (totalDelta > 0 && idleDelta <= totalDelta)
                        {
                            snapshot.CpuUsagePercent = ClampPercentage(
                                (totalDelta - idleDelta) * 100d / totalDelta);
                        }
                    }
                }

                _previousCpu = current;
            }
            catch (Exception ex)
            {
                snapshot.Warnings.Add($"Host CPU 采样失败：{ex.Message}");
            }
        }

        /// <summary>
        /// Adds physical-memory totals and utilization to a snapshot using the
        /// platform-specific memory reader selected by <see cref="TryReadMemory"/>.
        /// </summary>
        /// <param name="snapshot">The snapshot receiving memory values and warnings.</param>
        private static void CollectMemory(HostMetricsSnapshot snapshot)
        {
            try
            {
                if (!TryReadMemory(out var totalBytes, out var availableBytes) || totalBytes <= 0)
                {
                    snapshot.Warnings.Add("当前操作系统暂不支持 Host 物理内存采样。");
                    return;
                }

                availableBytes = Math.Clamp(availableBytes, 0, totalBytes);
                snapshot.MemoryTotalBytes = totalBytes;
                snapshot.MemoryAvailableBytes = availableBytes;
                snapshot.MemoryUsedBytes = totalBytes - availableBytes;
                snapshot.MemoryUsagePercent = ClampPercentage(snapshot.MemoryUsedBytes * 100d / totalBytes);
            }
            catch (Exception ex)
            {
                snapshot.Warnings.Add($"Host 内存采样失败：{ex.Message}");
            }
        }

        /// <summary>
        /// Adds aggregate network-interface counters and calculates receive/send
        /// rates from the preceding sample when counters are monotonic.
        /// </summary>
        /// <param name="snapshot">The snapshot receiving network values and warnings.</param>
        private void CollectNetwork(HostMetricsSnapshot snapshot)
        {
            try
            {
                var current = ReadNetworkCounters();
                snapshot.NetworkReceiveTotalBytes = current.ReceivedBytes;
                snapshot.NetworkSendTotalBytes = current.SentBytes;
                snapshot.NetworkInterfaceCount = current.InterfaceCount;

                if (_previousNetwork.HasValue)
                {
                    var previous = _previousNetwork.Value;
                    var elapsedSeconds = (current.Timestamp - previous.Timestamp) / (double)Stopwatch.Frequency;
                    if (elapsedSeconds > 0 &&
                        current.ReceivedBytes >= previous.ReceivedBytes &&
                        current.SentBytes >= previous.SentBytes)
                    {
                        snapshot.NetworkReceiveBytesPerSecond =
                            (current.ReceivedBytes - previous.ReceivedBytes) / elapsedSeconds;
                        snapshot.NetworkSendBytesPerSecond =
                            (current.SentBytes - previous.SentBytes) / elapsedSeconds;
                    }
                }

                _previousNetwork = current;
            }
            catch (Exception ex)
            {
                snapshot.Warnings.Add($"Host 网络采样失败：{ex.Message}");
            }
        }

        /// <summary>
        /// Adds current-process CPU utilization, working-set size, and uptime.
        /// Process sampling failures are retained as warnings on the snapshot.
        /// </summary>
        /// <param name="snapshot">The snapshot receiving process values and warnings.</param>
        private void CollectProcess(HostMetricsSnapshot snapshot)
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                var current = new ProcessCounterSnapshot(
                    Stopwatch.GetTimestamp(),
                    process.TotalProcessorTime.Ticks);

                if (_previousProcess.HasValue)
                {
                    var previous = _previousProcess.Value;
                    var elapsedSeconds = (current.Timestamp - previous.Timestamp) / (double)Stopwatch.Frequency;
                    var processorTimeTicks = current.ProcessorTimeTicks - previous.ProcessorTimeTicks;
                    if (elapsedSeconds > 0 && processorTimeTicks >= 0)
                    {
                        var processorSeconds = TimeSpan.FromTicks(processorTimeTicks).TotalSeconds;
                        snapshot.ProcessCpuUsagePercent = ClampPercentage(
                            processorSeconds * 100d /
                            (elapsedSeconds * Math.Max(1, Environment.ProcessorCount)));
                    }
                }

                _previousProcess = current;
                snapshot.ProcessWorkingSetBytes = Math.Max(0, process.WorkingSet64);
                snapshot.ApplicationUptimeSeconds = Math.Max(
                    0,
                    (long)(DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalSeconds);
            }
            catch (Exception ex)
            {
                snapshot.Warnings.Add($"当前 Web 进程采样失败：{ex.Message}");
            }
        }

        /// <summary>
        /// Selects and invokes the CPU counter reader for the current operating system.
        /// </summary>
        /// <param name="snapshot">Receives the total and idle counter values when supported.</param>
        /// <returns><see langword="true"/> when a usable counter sample was read.</returns>
        private static bool TryReadCpuCounters(out CpuCounterSnapshot snapshot)
        {
            if (OperatingSystem.IsWindows())
            {
                return TryReadWindowsCpuCounters(out snapshot);
            }

            if (OperatingSystem.IsLinux())
            {
                return TryReadLinuxCpuCounters(out snapshot);
            }

            if (OperatingSystem.IsMacOS())
            {
                return TryReadMacCpuCounters(out snapshot);
            }

            snapshot = default;
            return false;
        }

        /// <summary>
        /// Reads Windows system CPU times and converts them to aggregate total/idle counters.
        /// </summary>
        /// <param name="snapshot">Receives the Windows counter values.</param>
        /// <returns><see langword="true"/> when the native call succeeds.</returns>
        private static bool TryReadWindowsCpuCounters(out CpuCounterSnapshot snapshot)
        {
            if (!GetSystemTimes(out var idle, out var kernel, out var user))
            {
                snapshot = default;
                return false;
            }

            var idleValue = idle.ToUInt64();
            snapshot = new CpuCounterSnapshot(
                kernel.ToUInt64() + user.ToUInt64(),
                idleValue);
            return true;
        }

        /// <summary>
        /// Parses the aggregate CPU line from Linux <c>/proc/stat</c>.
        /// </summary>
        /// <param name="snapshot">Receives the parsed total and idle jiffy counters.</param>
        /// <returns><see langword="true"/> when the expected line and numeric fields exist.</returns>
        private static bool TryReadLinuxCpuCounters(out CpuCounterSnapshot snapshot)
        {
            var line = File.ReadLines("/proc/stat").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("cpu ", StringComparison.Ordinal))
            {
                snapshot = default;
                return false;
            }

            var values = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .Take(8)
                .Select(value => ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : 0UL)
                .ToArray();

            if (values.Length < 4)
            {
                snapshot = default;
                return false;
            }

            var total = values.Aggregate(0UL, (sum, value) => sum + value);
            var idle = values[3] + (values.Length > 4 ? values[4] : 0UL);
            snapshot = new CpuCounterSnapshot(total, idle);
            return true;
        }

        /// <summary>
        /// Reads the aggregate CPU load structure from macOS Mach host statistics.
        /// </summary>
        /// <param name="snapshot">Receives the converted total and idle counters.</param>
        /// <returns><see langword="true"/> when the native Mach call succeeds.</returns>
        private static bool TryReadMacCpuCounters(out CpuCounterSnapshot snapshot)
        {
            var info = new MacHostCpuLoadInfo();
            var count = MacHostCpuLoadInfoCount;
            var result = HostStatistics64(MachHostSelf(), MacHostCpuLoadInfoFlavor, ref info, ref count);
            if (result != 0)
            {
                snapshot = default;
                return false;
            }

            snapshot = new CpuCounterSnapshot(
                (ulong)info.User + info.System + info.Idle + info.Nice,
                info.Idle);
            return true;
        }

        /// <summary>
        /// Selects the operating-system-specific physical-memory reader.
        /// </summary>
        /// <param name="totalBytes">Receives total physical memory in bytes.</param>
        /// <param name="availableBytes">Receives available physical memory in bytes.</param>
        /// <returns><see langword="true"/> when the platform provides a valid sample.</returns>
        private static bool TryReadMemory(out long totalBytes, out long availableBytes)
        {
            if (OperatingSystem.IsWindows())
            {
                return TryReadWindowsMemory(out totalBytes, out availableBytes);
            }

            if (OperatingSystem.IsLinux())
            {
                return TryReadLinuxMemory(out totalBytes, out availableBytes);
            }

            if (OperatingSystem.IsMacOS())
            {
                return TryReadMacMemory(out totalBytes, out availableBytes);
            }

            totalBytes = 0;
            availableBytes = 0;
            return false;
        }

        /// <summary>
        /// Reads Windows physical-memory values through <c>GlobalMemoryStatusEx</c>.
        /// </summary>
        /// <param name="totalBytes">Receives total physical memory in bytes.</param>
        /// <param name="availableBytes">Receives available physical memory in bytes.</param>
        /// <returns><see langword="true"/> when the native API reports memory values.</returns>
        private static bool TryReadWindowsMemory(out long totalBytes, out long availableBytes)
        {
            var status = new WindowsMemoryStatus
            {
                Length = (uint)Marshal.SizeOf<WindowsMemoryStatus>()
            };

            if (!GlobalMemoryStatusEx(ref status))
            {
                totalBytes = 0;
                availableBytes = 0;
                return false;
            }

            totalBytes = ToInt64(status.TotalPhysical);
            availableBytes = ToInt64(status.AvailablePhysical);
            return true;
        }

        /// <summary>
        /// Reads <c>MemTotal</c> and <c>MemAvailable</c> from Linux <c>/proc/meminfo</c>.
        /// </summary>
        /// <param name="totalBytes">Receives total physical memory in bytes.</param>
        /// <param name="availableBytes">Receives available physical memory in bytes.</param>
        /// <returns><see langword="true"/> when a positive total is parsed.</returns>
        private static bool TryReadLinuxMemory(out long totalBytes, out long availableBytes)
        {
            long totalKb = 0;
            long availableKb = 0;

            foreach (var line in File.ReadLines("/proc/meminfo"))
            {
                if (line.StartsWith("MemTotal:", StringComparison.Ordinal))
                {
                    totalKb = ParseLinuxMemoryKb(line);
                }
                else if (line.StartsWith("MemAvailable:", StringComparison.Ordinal))
                {
                    availableKb = ParseLinuxMemoryKb(line);
                }

                if (totalKb > 0 && availableKb > 0)
                {
                    break;
                }
            }

            totalBytes = totalKb > 0 ? checked(totalKb * 1024) : 0;
            availableBytes = availableKb > 0 ? checked(availableKb * 1024) : 0;
            return totalBytes > 0;
        }

        /// <summary>
        /// Extracts the invariant-culture numeric kilobyte value from one Linux
        /// memory-information line.
        /// </summary>
        /// <param name="line">A line such as <c>MemTotal: 123 kB</c>.</param>
        /// <returns>The parsed kilobyte count, or zero for malformed input.</returns>
        private static long ParseLinuxMemoryKb(string line)
        {
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 &&
                   long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
        }

        /// <summary>
        /// Reads macOS physical-memory size and derives available memory from
        /// Mach virtual-memory page counters.
        /// </summary>
        /// <param name="totalBytes">Receives total physical memory in bytes.</param>
        /// <param name="availableBytes">Receives estimated available memory in bytes.</param>
        /// <returns><see langword="true"/> when all required native calls succeed.</returns>
        private static bool TryReadMacMemory(out long totalBytes, out long availableBytes)
        {
            nuint length = (nuint)sizeof(ulong);
            if (SysctlByName("hw.memsize", out var total, ref length, IntPtr.Zero, 0) != 0)
            {
                totalBytes = 0;
                availableBytes = 0;
                return false;
            }

            var host = MachHostSelf();
            var vmInfo = new MacVmStatistics64();
            var count = (uint)(Marshal.SizeOf<MacVmStatistics64>() / sizeof(int));
            if (HostStatistics64(host, MacHostVmInfo64Flavor, ref vmInfo, ref count) != 0 ||
                HostPageSize(host, out var pageSize) != 0)
            {
                totalBytes = 0;
                availableBytes = 0;
                return false;
            }

            var availablePages = (ulong)vmInfo.FreeCount +
                                 vmInfo.InactiveCount +
                                 vmInfo.SpeculativeCount +
                                 vmInfo.PurgeableCount;
            var available = availablePages * pageSize;
            totalBytes = ToInt64(total);
            availableBytes = ToInt64(Math.Min(total, available));
            return totalBytes > 0;
        }

        /// <summary>
        /// Aggregates byte counters from active non-loopback, non-tunnel interfaces.
        /// Interfaces that reject statistics queries are skipped.
        /// </summary>
        /// <returns>A timestamped aggregate network-counter snapshot.</returns>
        private static NetworkCounterSnapshot ReadNetworkCounters()
        {
            long receivedBytes = 0;
            long sentBytes = 0;
            var interfaceCount = 0;

            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                try
                {
                    var statistics = networkInterface.GetIPStatistics();
                    receivedBytes = SafeAdd(receivedBytes, Math.Max(0, statistics.BytesReceived));
                    sentBytes = SafeAdd(sentBytes, Math.Max(0, statistics.BytesSent));
                    interfaceCount++;
                }
                catch (NetworkInformationException)
                {
                    //Legacy comment translated to English.
                }
            }

            return new NetworkCounterSnapshot(
                Stopwatch.GetTimestamp(),
                receivedBytes,
                sentBytes,
                interfaceCount);
        }

        /// <summary>
        /// Adds two non-negative counters while saturating at <see cref="long.MaxValue"/>.
        /// </summary>
        /// <param name="left">The accumulated counter value.</param>
        /// <param name="right">The next counter value.</param>
        /// <returns>The exact sum when representable, otherwise <see cref="long.MaxValue"/>.</returns>
        private static long SafeAdd(long left, long right)
        {
            return left > long.MaxValue - right ? long.MaxValue : left + right;
        }

        /// <summary>
        /// Converts an unsigned native byte count to a signed value without overflow.
        /// </summary>
        /// <param name="value">The native unsigned value.</param>
        /// <returns>The signed value, saturated at <see cref="long.MaxValue"/>.</returns>
        private static long ToInt64(ulong value)
        {
            return value > long.MaxValue ? long.MaxValue : (long)value;
        }

        /// <summary>
        /// Restricts a percentage to the inclusive range 0-100 and rounds it to
        /// two decimal places for stable API output.
        /// </summary>
        /// <param name="value">The calculated percentage.</param>
        /// <returns>The normalized percentage.</returns>
        private static double ClampPercentage(double value)
        {
            return Math.Round(Math.Clamp(value, 0d, 100d), 2);
        }

        private readonly record struct CpuCounterSnapshot(ulong Total, ulong Idle);

        private readonly record struct NetworkCounterSnapshot(
            long Timestamp,
            long ReceivedBytes,
            long SentBytes,
            int InterfaceCount);

        private readonly record struct ProcessCounterSnapshot(
            long Timestamp,
            long ProcessorTimeTicks);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeFileTime
        {
            public uint Low;
            public uint High;

            /// <summary>
            /// Combines the native high and low 32-bit fields into one counter value.
            /// </summary>
            /// <returns>The unsigned 64-bit representation of the native time value.</returns>
            public readonly ulong ToUInt64()
            {
                return ((ulong)High << 32) | Low;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowsMemoryStatus
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhysical;
            public ulong AvailablePhysical;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtual;
            public ulong AvailableVirtual;
            public ulong AvailableExtendedVirtual;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MacHostCpuLoadInfo
        {
            public uint User;
            public uint System;
            public uint Idle;
            public uint Nice;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MacVmStatistics64
        {
            public uint FreeCount;
            public uint ActiveCount;
            public uint InactiveCount;
            public uint WireCount;
            public ulong ZeroFillCount;
            public ulong Reactivations;
            public ulong PageIns;
            public ulong PageOuts;
            public ulong Faults;
            public ulong CopyOnWriteFaults;
            public ulong Lookups;
            public ulong Hits;
            public ulong Purges;
            public uint PurgeableCount;
            public uint SpeculativeCount;
            public ulong Decompressions;
            public ulong Compressions;
            public ulong SwapIns;
            public ulong SwapOuts;
            public uint CompressorPageCount;
            public uint ThrottledCount;
            public uint ExternalPageCount;
            public uint InternalPageCount;
            public ulong TotalUncompressedPagesInCompressor;
        }

        private const string WindowsKernelLibrary = "kernel32.dll";
        private const string MacSystemLibrary = "/usr/lib/libSystem.B.dylib";
        private const int MacHostCpuLoadInfoFlavor = 3;
        private const int MacHostVmInfo64Flavor = 4;
        private const uint MacHostCpuLoadInfoCount = 4;

        [DllImport(WindowsKernelLibrary, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSystemTimes(
            out NativeFileTime idleTime,
            out NativeFileTime kernelTime,
            out NativeFileTime userTime);

        [DllImport(WindowsKernelLibrary, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref WindowsMemoryStatus buffer);

        [DllImport(MacSystemLibrary, EntryPoint = "mach_host_self")]
        private static extern uint MachHostSelf();

        [DllImport(MacSystemLibrary, EntryPoint = "host_statistics64")]
        private static extern int HostStatistics64(
            uint host,
            int flavor,
            ref MacHostCpuLoadInfo info,
            ref uint count);

        [DllImport(MacSystemLibrary, EntryPoint = "host_statistics64")]
        private static extern int HostStatistics64(
            uint host,
            int flavor,
            ref MacVmStatistics64 info,
            ref uint count);

        [DllImport(MacSystemLibrary, EntryPoint = "host_page_size")]
        private static extern int HostPageSize(uint host, out uint pageSize);

        [DllImport(MacSystemLibrary, EntryPoint = "sysctlbyname")]
        private static extern int SysctlByName(
            [MarshalAs(UnmanagedType.LPStr)] string name,
            out ulong oldValue,
            ref nuint oldLength,
            IntPtr newValue,
            nuint newLength);
    }
}
