/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IXncfPreviewService.cs
    文件功能描述：XNCF 独立预览进程契约


    创建标识：Senparc - 20260801

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Preview
{
    public sealed class XncfPreviewStartOptions
    {
        public string SolutionFilePath { get; init; }

        public string ModuleProjectName { get; init; }

        public int Port { get; init; }

        public int StartupTimeoutSeconds { get; init; } = 120;

        public string EnvironmentName { get; init; } = XncfPreviewService.DefaultEnvironmentName;
    }

    public sealed class XncfPreviewSessionInfo
    {
        public string SessionId { get; init; }

        public string ModuleProjectName { get; init; }

        public string Url { get; init; }

        public int ProcessId { get; init; }

        public DateTimeOffset StartedAt { get; init; }

        public bool IsRunning { get; init; }

        public string EnvironmentName { get; init; }

        public string RecentOutput { get; init; }
    }

    public interface IXncfPreviewService
    {
        Task<XncfPreviewSessionInfo> StartAsync(
            XncfPreviewStartOptions options,
            Action<string> log = null,
            CancellationToken cancellationToken = default);

        Task<bool> StopAsync(
            string sessionId,
            Action<string> log = null,
            CancellationToken cancellationToken = default);

        IReadOnlyList<XncfPreviewSessionInfo> GetSessions(bool includeOutput = false);
    }
}
