/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IXncfPreviewService.cs
    文件功能描述：XNCF 独立预览进程契约


    创建标识：Senparc - 20260801

    修改标识：Senparc - 20260804
    修改描述：v0.39.0-preview8 新增 XNCF 隔离预览持久化与跨数据库迁移支持

    修改标识：Senparc - 20260815
    修改描述：v0.41.0 增强隔离开发任务与 Sandbox 预览流程

    修改标识：Senparc - 20260822
    修改描述：v0.41.0 优化 XncfBuilder 预览任务与工作区服务

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Preview
{
    public enum XncfPreviewStage
    {
        Queued = 0,
        PreparingSource = 1,
        Validating = 2,
        Restoring = 3,
        Building = 4,
        Verifying = 5,
        Starting = 6,
        HealthChecking = 7,
        Replacing = 8,
        Running = 9,
        Stopping = 10,
        Stopped = 11,
        Replaced = 12,
        Failed = 13,
        Cancelled = 14,
        Interrupted = 15
    }

    public enum XncfPreviewHostStatus
    {
        NotCreated = 0,
        Starting = 1,
        HealthChecking = 2,
        Healthy = 3,
        Stopping = 4,
        Stopped = 5,
        Replaced = 6,
        Failed = 7,
        Interrupted = 8
    }

    public static class XncfPreviewStageExtensions
    {
        public static int GetProgressPercent(this XncfPreviewStage stage)
        {
            return stage switch
            {
                XncfPreviewStage.Queued => 0,
                XncfPreviewStage.PreparingSource => 5,
                XncfPreviewStage.Validating => 10,
                XncfPreviewStage.Restoring => 20,
                XncfPreviewStage.Building => 35,
                XncfPreviewStage.Verifying => 60,
                XncfPreviewStage.Starting => 70,
                XncfPreviewStage.HealthChecking => 80,
                XncfPreviewStage.Replacing => 95,
                _ => 100
            };
        }

        public static bool IsTerminal(this XncfPreviewStage stage)
        {
            return stage is XncfPreviewStage.Stopped
                or XncfPreviewStage.Replaced
                or XncfPreviewStage.Failed
                or XncfPreviewStage.Cancelled
                or XncfPreviewStage.Interrupted;
        }

        public static bool CanStop(this XncfPreviewStage stage)
        {
            return !stage.IsTerminal() && stage != XncfPreviewStage.Stopping;
        }
    }

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

        /// <summary>
        /// Absolute path of the solution used for this build. It is displayed for traceability;
        /// callers must never treat it as an authority to write source files.
        /// </summary>
        public string SolutionFilePath { get; init; }

        public string Url { get; init; }

        public int ProcessId { get; init; }

        public DateTimeOffset StartedAt { get; init; }

        public bool IsRunning { get; init; }

        public XncfPreviewStage Stage { get; init; }

        public int ProgressPercent { get; init; }

        public string StatusMessage { get; init; }

        public DateTimeOffset UpdatedAt { get; init; }

        public DateTimeOffset? CompletedAt { get; init; }

        public string ErrorMessage { get; init; }

        public bool IsTerminal { get; init; }

        public bool CanStop { get; init; }

        public string EnvironmentName { get; init; }

        public string SourceFingerprint { get; init; }

        public string ModuleAssemblySha256 { get; init; }

        public string RecentOutput { get; init; }

        public XncfPreviewHostStatus HostStatus { get; init; }

        public string HostStatusMessage { get; init; }

        public DateTimeOffset? ProcessStartedAt { get; init; }

        public DateTimeOffset? HealthyAt { get; init; }

        public DateTimeOffset? StoppedAt { get; init; }

        public int? ExitCode { get; init; }

        /// <summary>
        /// Per-session published output. The directory is removed after a stopped/replaced/failed
        /// preview, so this value can legitimately point to a path that no longer exists.
        /// </summary>
        public string PublishDirectory { get; init; }
    }

    public sealed class XncfPreviewPersistenceInfo
    {
        public bool IsAvailable { get; init; }

        public string StatusMessage { get; init; }

        public string ErrorMessage { get; init; }

        public DateTimeOffset UpdatedAt { get; init; }
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

        XncfPreviewSessionInfo GetSession(string sessionId, bool includeOutput = false);

        IReadOnlyList<XncfPreviewSessionInfo> GetSessions(bool includeOutput = false);

        XncfPreviewPersistenceInfo GetPersistenceStatus();
    }
}
