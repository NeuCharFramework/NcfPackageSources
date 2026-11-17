/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: ResourceMonitorModels.cs
    File description: Related implementation.
    Legacy comment translated to English.

    Created by: Senparc - 20260926
    Change description: v0.12.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Admin home resource monitor - per-Web-process CPU/memory lines + nonlinear 24h time-axis chart

----------------------------------------------------------------*/

using System.Collections.Generic;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public sealed class ResourceMonitorPoint
    {
        ///Legacy comment translated to English.
        public long T { get; set; }
        ///Legacy comment translated to English.
        public double? Cpu { get; set; }
        ///Legacy comment translated to English.
        public double? Pcpu { get; set; }
        ///Legacy comment translated to English.
        public double? Mem { get; set; }
        ///Legacy comment translated to English.
        public double? Pmem { get; set; }
        ///Legacy comment translated to English.
        public double? Rx { get; set; }
        ///Legacy comment translated to English.
        public double? Tx { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public sealed class ResourceMonitorWindow
    {
        ///Legacy comment translated to English.
        public string Key { get; set; } = string.Empty;
        ///Legacy comment translated to English.
        public int ResolutionSeconds { get; set; }
        ///Legacy comment translated to English.
        public List<ResourceMonitorPoint> Points { get; set; } = new();
    }

    /// <summary>
    /// GetResourceMonitor interfaceresponse。
    /// </summary>
    public sealed class ResourceMonitorData
    {
        ///Legacy comment translated to English.
        public long Now { get; set; }
        ///Legacy comment translated to English.
        public int SampleIntervalSeconds { get; set; }
        ///Legacy comment translated to English.
        public List<ResourceMonitorWindow> Windows { get; set; } = new();
    }
}
