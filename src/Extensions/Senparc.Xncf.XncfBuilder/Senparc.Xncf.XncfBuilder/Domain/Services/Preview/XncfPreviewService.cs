/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfPreviewService.cs
    File description: Related implementation.

    Created by: Senparc - 20260801

    Modified by: Senparc - 20260804
    Change description: v0.39.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.40.0-preview10 Updated module behavior and related support.

    Modified by: Senparc - 20260815
    Change description: v0.41.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.41.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.41.7 Updated module behavior and related support.

    Modified by: Senparc - 20261110
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v0.42.0.0 add MCP endpoint gateway and tool invocation contracts

----------------------------------------------------------------*/

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Senparc.Ncf.Core.MultiTenant;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Preview
{
    public sealed class XncfPreviewService : IXncfPreviewService, IHostedService
    {
        public const string DefaultEnvironmentName = "XncfPreview";

        private const int MaxLogLines = 300;
        private const int MaxPersistedOutputChars = 64 * 1024;
        private const int MaxRetainedTerminalSessions = 50;
        private static readonly HashSet<string> SourceFingerprintExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            ".idea",
            ".vs",
            ".vscode",
            "bin",
            "node_modules",
            "obj",
            "packages"
        };
        private static readonly HashSet<string> PreviewEnvironmentAllowList = new(StringComparer.OrdinalIgnoreCase)
        {
            "COMSPEC",
            "DOTNET_BUNDLE_EXTRACT_BASE_DIR",
            "DOTNET_ROOT",
            "DOTNET_ROOT_ARM64",
            "DOTNET_ROOT_X64",
            "HOME",
            "LANG",
            "LC_ALL",
            "LOCALAPPDATA",
            "PATH",
            "PATHEXT",
            "SystemRoot",
            "TEMP",
            "TMP",
            "TMPDIR",
            "USERPROFILE",
            "WINDIR"
        };
        private readonly ILogger<XncfPreviewService> _logger;
        private readonly IXncfPreviewStateStore _stateStore;
        private readonly ConcurrentDictionary<string, PreviewProcessState> _sessions = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, string> _activeSessionIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _operationLock = new(1, 1);
        private readonly object _persistenceStatusLock = new();
        private readonly string _previewRoot;
        private bool _persistenceAvailable;
        private string _persistenceStatusMessage;
        private string _persistenceErrorMessage;
        private DateTimeOffset _persistenceStatusUpdatedAt;

        /// <summary>
        /// Initializes the preview process manager and optional persisted-state store.
        /// </summary>
        /// <param name="logger">Optional logger for build and process diagnostics.</param>
        /// <param name="stateStore">Optional store for recovering preview state after restart.</param>
        public XncfPreviewService(
            ILogger<XncfPreviewService> logger = null,
            IXncfPreviewStateStore stateStore = null)
        {
            _logger = logger;
            _stateStore = stateStore;
            _previewRoot = Path.Combine(Path.GetTempPath(), "Senparc.Ncf", "XncfPreview");
            _persistenceAvailable = stateStore != null;
            _persistenceStatusMessage = stateStore == null
                ? "未配置预览状态数据库存储，当前仅使用内存状态。"
                : "正在初始化预览状态数据库存储。";
            _persistenceStatusUpdatedAt = DateTimeOffset.Now;
        }

        /// <summary>
        /// Creates the local preview root used for isolated publish output.
        /// </summary>
        /// <param name="cancellationToken">Token checked by the hosted-service lifecycle.</param>
        /// <returns>A completed task after root initialization.</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(_previewRoot);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Loads recent persisted preview sessions and marks interrupted sessions for recovery.
        /// Persistence failures disable the store without stopping the host.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel state loading.</param>
        internal async Task InitializePersistenceAsync(CancellationToken cancellationToken)
        {
            if (_stateStore == null)
            {
                return;
            }

            try
            {
                var persistedSessions = await _stateStore.LoadRecentAndInterruptAsync(
                        MaxRetainedTerminalSessions,
                        DateTimeOffset.Now,
                        cancellationToken)
                    .ConfigureAwait(false);
                foreach (var snapshot in persistedSessions)
                {
                    _sessions.TryAdd(snapshot.SessionId, RestoreState(snapshot));
                }

                SetPersistenceAvailable();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                DisablePersistence(
                    "预览状态表尚未就绪，主站将继续运行，当前预览状态仅保存在内存中。应用 migration 后请重启主站。",
                    ex);
            }
        }

        /// <summary>
        /// Stops active preview processes, persists terminal state where possible,
        /// and clears in-memory session indexes during host shutdown.
        /// </summary>
        /// <param name="cancellationToken">Token controlling bounded shutdown cleanup.</param>
        /// <returns>A task that completes after cleanup or cancellation.</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            RequestHostStopForActiveSessions();

            var lockTaken = false;
            try
            {
                lockTaken = await TryAcquireOperationLockForShutdownAsync(cancellationToken).ConfigureAwait(false);
                if (!lockTaken)
                {
                    _logger?.LogInformation(
                        "XNCF preview cleanup did not acquire the operation lock before host shutdown. " +
                        "Active previews have already received the stop signal; host shutdown will not wait for the in-flight operation.");
                    return;
                }

                // An in-flight request can have created a session while shutdown was waiting for the lock.
                RequestHostStopForActiveSessions();
                using var cleanupCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cleanupCancellation.CancelAfter(TimeSpan.FromSeconds(2));

                foreach (var state in _sessions.Values.ToArray())
                {
                    cleanupCancellation.Token.ThrowIfCancellationRequested();
                    if (GetStage(state).IsTerminal())
                    {
                        continue;
                    }

                    await SetStageAndPersistAsync(
                        state,
                        XncfPreviewStage.Stopping,
                        "主站正在关闭预览进程。",
                        null,
                        cleanupCancellation.Token).ConfigureAwait(false);
                    await StopStateAsync(state, deleteFiles: true, log: null, cleanupCancellation.Token).ConfigureAwait(false);
                    await SetStageAndPersistAsync(
                        state,
                        XncfPreviewStage.Stopped,
                        "预览进程已随主站停止。",
                        null,
                        cleanupCancellation.Token).ConfigureAwait(false);
                }

                _sessions.Clear();
                _activeSessionIds.Clear();
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("XNCF preview cleanup ended early so the host can finish shutting down.");
            }
            finally
            {
                if (lockTaken)
                {
                    _operationLock.Release();
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the operation lock within the normal or short shutdown wait window.
        /// </summary>
        /// <param name="cancellationToken">Token used for the normal wait.</param>
        /// <returns><see langword="true"/> when the lock is acquired.</returns>
        private async Task<bool> TryAcquireOperationLockForShutdownAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                    return true;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                }
            }

            return await _operationLock.WaitAsync(TimeSpan.FromMilliseconds(200)).ConfigureAwait(false);
        }

        /// <summary>
        /// Builds, publishes, verifies, and starts an isolated XNCF preview host.
        /// The source fingerprint and published assembly hash are recorded for diagnostics.
        /// </summary>
        /// <param name="options">Preview project, solution, environment, and runtime options.</param>
        /// <param name="log">Optional callback receiving process output.</param>
        /// <param name="cancellationToken">Token used to cancel preparation or startup.</param>
        /// <param name="ambientTenant">Optional tenant context captured for the preview session.</param>
        /// <returns>The current preview session information.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when project paths, restore, publish, verification, or health checks fail.</exception>
        public async Task<XncfPreviewSessionInfo> StartAsync(
            XncfPreviewStartOptions options,
            Action<string> log = null,
            CancellationToken cancellationToken = default,
            RequestTenantInfo? ambientTenant = null)
        {
            ArgumentNullException.ThrowIfNull(options);
            ValidateOptions(options);

            var sessionId = CreateSessionId(options.ModuleProjectName);
            var newState = new PreviewProcessState(
                sessionId,
                options.ModuleProjectName,
                options.SolutionFilePath,
                log);
            // Preserve the ambient tenant identity so recovery and diagnostics remain
            // associated with the tenant that created the preview session.
            newState.TenantId = ambientTenant?.MatchSuccess == true ? ambientTenant.Id : 0;
            _sessions[sessionId] = newState;
            SetStage(newState, XncfPreviewStage.Queued, "预览任务已进入队列。", null);
            await PersistStateAsync(newState, cancellationToken).ConfigureAwait(false);
            TrimTerminalHistory();

            var lockTaken = false;
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                newState.StopCancellation.Token);
            try
            {
                await _operationLock.WaitAsync(linkedCancellation.Token).ConfigureAwait(false);
                lockTaken = true;

                await SetStageAndPersistAsync(
                    newState,
                    XncfPreviewStage.PreparingSource,
                    "正在解析模块目录并锁定待构建源码快照。",
                    null,
                    linkedCancellation.Token).ConfigureAwait(false);
                var paths = ResolveProjectPaths(options.SolutionFilePath, options.ModuleProjectName);
                var publishDirectory = Path.Combine(_previewRoot, sessionId, "app");
                Directory.CreateDirectory(publishDirectory);
                var moduleDirectory = Path.GetDirectoryName(paths.ModuleProjectFilePath)
                    ?? throw new InvalidOperationException("无法获取 XNCF 项目目录。");
                var sourceFingerprint = ComputeSourceFingerprint(moduleDirectory);

                lock (newState.SyncRoot)
                {
                    newState.PublishDirectory = publishDirectory;
                    newState.SourceFingerprint = sourceFingerprint;
                }

                AppendProcessOutput(newState, $"准备 XNCF 预览：{options.ModuleProjectName}");
                AppendProcessOutput(newState, $"预览发布目录：{publishDirectory}");
                AppendProcessOutput(newState, $"构建输入源码指纹：{sourceFingerprint}");

                await SetStageAndPersistAsync(
                    newState,
                    XncfPreviewStage.Validating,
                    "源码快照已锁定，正在校验 Senparc.Web 的项目引用。",
                    null,
                    linkedCancellation.Token).ConfigureAwait(false);
                ValidateHostProjectReference(paths);

                if (RequiresRestore(paths))
                {
                    await SetStageAndPersistAsync(
                        newState,
                        XncfPreviewStage.Restoring,
                        "检测到新项目或包引用变化，正在执行必要的 dotnet restore。",
                        null,
                        linkedCancellation.Token).ConfigureAwait(false);
                    var restoreStartInfo = CreateRestoreStartInfo(paths.WebProjectFilePath);
                    var restoreResult = await RunCommandAsync(
                        restoreStartInfo,
                        message => AppendProcessOutput(newState, message),
                        linkedCancellation.Token).ConfigureAwait(false);
                    if (restoreResult.ExitCode != 0)
                    {
                        throw new InvalidOperationException(
                            $"预览宿主还原失败（退出码：{restoreResult.ExitCode}）。{GetOutputTail(restoreResult.Output)}");
                    }
                }

                await SetStageAndPersistAsync(
                    newState,
                    XncfPreviewStage.Building,
                    "正在隔离发布预览宿主。",
                    null,
                    linkedCancellation.Token).ConfigureAwait(false);
                var publishStartInfo = CreatePublishStartInfo(paths.WebProjectFilePath, publishDirectory);
                var publishResult = await RunCommandAsync(
                    publishStartInfo,
                    message => AppendProcessOutput(newState, message),
                    linkedCancellation.Token).ConfigureAwait(false);
                if (publishResult.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"预览宿主发布失败（退出码：{publishResult.ExitCode}）。{GetOutputTail(publishResult.Output)}");
                }

                await SetStageAndPersistAsync(
                    newState,
                    XncfPreviewStage.Verifying,
                    "正在校验发布产物与源码一致性。",
                    null,
                    linkedCancellation.Token).ConfigureAwait(false);
                var moduleAssemblyPath = Path.Combine(publishDirectory, $"{options.ModuleProjectName}.dll");
                if (!File.Exists(moduleAssemblyPath))
                {
                    throw new InvalidOperationException(
                        $"预览发布结果中没有找到 {options.ModuleProjectName}.dll。请确认 Senparc.Web 已引用该 XNCF 项目。");
                }

                var sourceFingerprintAfterPublish = ComputeSourceFingerprint(moduleDirectory);
                if (!string.Equals(sourceFingerprint, sourceFingerprintAfterPublish, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "XNCF 源码在预览构建过程中发生变化，已拒绝启动不确定的构建结果；请在代码写入完成后重新预览。");
                }

                var moduleAssemblySha256 = ComputeFileSha256(moduleAssemblyPath);
                lock (newState.SyncRoot)
                {
                    newState.ModuleAssemblySha256 = moduleAssemblySha256;
                }
                AppendProcessOutput(newState, $"预览模块 DLL SHA-256：{moduleAssemblySha256}");

                var webAssemblyPath = Path.Combine(publishDirectory, "Senparc.Web.dll");
                if (!File.Exists(webAssemblyPath))
                {
                    throw new InvalidOperationException("预览发布结果中没有找到 Senparc.Web.dll。");
                }

                var port = options.Port == 0 ? GetAvailableLoopbackPort() : options.Port;
                EnsurePortAvailable(port);
                var url = $"http://127.0.0.1:{port}";
                var environmentName = string.IsNullOrWhiteSpace(options.EnvironmentName)
                    ? DefaultEnvironmentName
                    : options.EnvironmentName.Trim();

                lock (newState.SyncRoot)
                {
                    newState.Url = url;
                    newState.EnvironmentName = environmentName;
                }

                await SetStageAndPersistAsync(
                    newState,
                    XncfPreviewStage.Starting,
                    "正在启动新的隔离预览进程。",
                    null,
                    linkedCancellation.Token).ConfigureAwait(false);
                var webStartInfo = CreateWebStartInfo(publishDirectory, port, environmentName);
                var process = new Process
                {
                    StartInfo = webStartInfo,
                    EnableRaisingEvents = true
                };

                lock (newState.SyncRoot)
                {
                    newState.Process = process;
                }

                AttachOutput(process, newState);

                if (!process.Start())
                {
                    throw new InvalidOperationException("无法启动 XNCF 预览进程。");
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                lock (newState.SyncRoot)
                {
                    newState.ProcessId = process.Id;
                    newState.ProcessStartedAt = DateTimeOffset.Now;
                }
                await SetStageAndPersistAsync(
                    newState,
                    XncfPreviewStage.HealthChecking,
                    $"进程 {process.Id} 已启动，正在执行健康检查。",
                    null,
                    linkedCancellation.Token).ConfigureAwait(false);

                await WaitForHealthyAsync(
                    newState,
                    TimeSpan.FromSeconds(options.StartupTimeoutSeconds),
                    linkedCancellation.Token).ConfigureAwait(false);

                var sourceFingerprintWhenReady = ComputeSourceFingerprint(moduleDirectory);
                if (!string.Equals(sourceFingerprint, sourceFingerprintWhenReady, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "XNCF 源码在预览启动过程中发生变化，已停止本次预览；请完成代码写入后重新执行。");
                }

                lock (newState.SyncRoot)
                {
                    newState.HealthyAt = DateTimeOffset.Now;
                }
                await SetStageAndPersistAsync(
                    newState,
                    XncfPreviewStage.Replacing,
                    "新预览已通过健康检查，正在原子替换同模块旧预览。",
                    null,
                    linkedCancellation.Token).ConfigureAwait(false);

                if (_activeSessionIds.TryGetValue(options.ModuleProjectName, out var oldSessionId)
                    && _sessions.TryGetValue(oldSessionId, out var oldState)
                    && !string.Equals(oldSessionId, sessionId, StringComparison.OrdinalIgnoreCase)
                    && !GetStage(oldState).IsTerminal())
                {
                    await SetStageAndPersistAsync(
                        oldState,
                        XncfPreviewStage.Stopping,
                        $"已由新预览 {sessionId} 替换，正在停止旧进程。",
                        null,
                        CancellationToken.None).ConfigureAwait(false);
                    await StopStateAsync(oldState, deleteFiles: true, log: null, CancellationToken.None).ConfigureAwait(false);
                    await SetStageAndPersistAsync(
                        oldState,
                        XncfPreviewStage.Replaced,
                        $"已由新预览 {sessionId} 替换。",
                        null,
                        CancellationToken.None).ConfigureAwait(false);
                }

                _activeSessionIds[options.ModuleProjectName] = sessionId;
                await SetStageAndPersistAsync(
                    newState,
                    XncfPreviewStage.Running,
                    $"XNCF 预览已就绪：{url}",
                    null,
                    CancellationToken.None).ConfigureAwait(false);
                TrimTerminalHistory();
                return ToInfo(newState, includeOutput: true);
            }
            catch (OperationCanceledException)
            {
                SetStage(newState, XncfPreviewStage.Cancelled, "预览任务已取消。", null);
                await StopStateAsync(newState, deleteFiles: true, log: null, CancellationToken.None).ConfigureAwait(false);
                await TryPersistStateAsync(newState).ConfigureAwait(false);
                TrimTerminalHistory();
                throw;
            }
            catch (Exception ex)
            {
                SetStage(newState, XncfPreviewStage.Failed, "预览任务执行失败。", ex.Message);
                await StopStateAsync(newState, deleteFiles: true, log: null, CancellationToken.None).ConfigureAwait(false);
                await TryPersistStateAsync(newState).ConfigureAwait(false);
                TrimTerminalHistory();
                throw;
            }
            finally
            {
                newState.StartupLog = null;
                if (lockTaken)
                {
                    _operationLock.Release();
                }
            }
        }

        /// <summary>
        /// Stops one preview session, terminates its host process, and optionally deletes its files.
        /// </summary>
        /// <param name="sessionId">The preview session identifier.</param>
        /// <param name="deleteFiles">Whether the publish directory should be removed.</param>
        /// <param name="cancellationToken">Token used to cancel shutdown.</param>
        /// <returns><see langword="true"/> when a session was found and stop processing completed.</returns>
        public async Task<bool> StopAsync(
            string sessionId,
            Action<string> log = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return false;
            }

            if (!_sessions.TryGetValue(sessionId.Trim(), out var state))
            {
                return false;
            }

            var initialStage = GetStage(state);
            if (initialStage.IsTerminal())
            {
                return false;
            }

            if (initialStage is not XncfPreviewStage.Running and not XncfPreviewStage.Stopping)
            {
                state.StopCancellation.Cancel();
            }

            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var currentStage = GetStage(state);
                if (currentStage.IsTerminal())
                {
                    return true;
                }

                await SetStageAndPersistAsync(
                    state,
                    XncfPreviewStage.Stopping,
                    "正在停止预览进程。",
                    null,
                    CancellationToken.None).ConfigureAwait(false);
                // Once stop has been accepted, finish process cleanup even if the HTTP caller disconnects.
                await StopStateAsync(state, deleteFiles: true, log, CancellationToken.None).ConfigureAwait(false);
                await SetStageAndPersistAsync(
                    state,
                    XncfPreviewStage.Stopped,
                    "预览已由用户停止。",
                    null,
                    CancellationToken.None).ConfigureAwait(false);
                if (_activeSessionIds.TryGetValue(state.ModuleProjectName, out var activeSessionId)
                    && string.Equals(activeSessionId, state.SessionId, StringComparison.OrdinalIgnoreCase))
                {
                    _activeSessionIds.TryRemove(state.ModuleProjectName, out _);
                }

                TrimTerminalHistory();
                return true;
            }
            finally
            {
                _operationLock.Release();
            }
        }

        /// <summary>
        /// Gets current and retained terminal preview sessions.
        /// </summary>
        /// <param name="includeOutput">Whether bounded process output should be included.</param>
        /// <returns>A snapshot ordered by session update time.</returns>
        public IReadOnlyList<XncfPreviewSessionInfo> GetSessions(bool includeOutput = false)
        {
            return _sessions.Values
                .OrderByDescending(z => z.StartedAt)
                .Select(z => ToInfo(z, includeOutput))
                .ToArray();
        }

        /// <summary>
        /// Gets one preview session by identifier.
        /// </summary>
        /// <param name="sessionId">The preview session identifier.</param>
        /// <param name="includeOutput">Whether bounded process output should be included.</param>
        /// <returns>The session information, or <see langword="null"/> when not found.</returns>
        public XncfPreviewSessionInfo GetSession(string sessionId, bool includeOutput = false)
        {
            return !string.IsNullOrWhiteSpace(sessionId)
                   && _sessions.TryGetValue(sessionId.Trim(), out var state)
                ? ToInfo(state, includeOutput)
                : null;
        }

        /// <summary>
        /// Returns the current persisted-state availability and last error information.
        /// </summary>
        /// <returns>Persistence status metadata.</returns>
        public XncfPreviewPersistenceInfo GetPersistenceStatus()
        {
            lock (_persistenceStatusLock)
            {
                return new XncfPreviewPersistenceInfo
                {
                    IsAvailable = _persistenceAvailable,
                    StatusMessage = _persistenceStatusMessage,
                    ErrorMessage = _persistenceErrorMessage,
                    UpdatedAt = _persistenceStatusUpdatedAt
                };
            }
        }

        /// <summary>
        /// Resolves the module project, web host project, and solution paths used by preview publishing.
        /// </summary>
        /// <param name="solutionFilePath">The solution file path.</param>
        /// <param name="moduleProjectName">The XNCF module project name.</param>
        /// <returns>The resolved preview project paths.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the solution or expected projects cannot be resolved.</exception>
        internal static XncfPreviewProjectPaths ResolveProjectPaths(string solutionFilePath, string moduleProjectName)
        {
            if (string.IsNullOrWhiteSpace(solutionFilePath)
                || !string.Equals(Path.GetExtension(solutionFilePath), ".sln", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(solutionFilePath))
            {
                throw new FileNotFoundException("未找到用于预览的解决方案文件。", solutionFilePath);
            }

            if (string.IsNullOrWhiteSpace(moduleProjectName)
                || moduleProjectName is "." or ".."
                || !string.Equals(moduleProjectName, Path.GetFileName(moduleProjectName), StringComparison.Ordinal)
                || moduleProjectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || moduleProjectName.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_')))
            {
                throw new ArgumentException("XNCF 项目名称无效，必须提供完整项目名称，例如 Senparc.Xncf.Sample。", nameof(moduleProjectName));
            }

            var solutionDirectory = Path.GetDirectoryName(Path.GetFullPath(solutionFilePath))
                ?? throw new InvalidOperationException("无法获取解决方案目录。");
            var webProjectFilePath = Path.Combine(solutionDirectory, "Senparc.Web", "Senparc.Web.csproj");
            var moduleProjectFilePath = Path.Combine(solutionDirectory, moduleProjectName, $"{moduleProjectName}.csproj");

            if (!File.Exists(webProjectFilePath))
            {
                throw new FileNotFoundException("未找到预览宿主 Senparc.Web.csproj。", webProjectFilePath);
            }

            if (!File.Exists(moduleProjectFilePath))
            {
                throw new FileNotFoundException("未找到需要预览的 XNCF 项目文件。", moduleProjectFilePath);
            }

            return new XncfPreviewProjectPaths(solutionDirectory, webProjectFilePath, moduleProjectFilePath);
        }

        /// <summary>
        /// Verifies that the web host project references the requested XNCF module project.
        /// </summary>
        /// <param name="paths">The resolved preview project paths.</param>
        /// <exception cref="InvalidOperationException">Thrown when the project reference is missing.</exception>
        internal static void ValidateHostProjectReference(XncfPreviewProjectPaths paths)
        {
            ArgumentNullException.ThrowIfNull(paths);

            var webProjectDirectory = Path.GetDirectoryName(paths.WebProjectFilePath)
                ?? throw new InvalidOperationException("无法获取 Senparc.Web 项目目录。");
            var expectedModuleProject = Path.GetFullPath(paths.ModuleProjectFilePath);
            var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            var projectDocument = XDocument.Load(paths.WebProjectFilePath, LoadOptions.PreserveWhitespace);
            var hasSourceReference = projectDocument
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "ProjectReference", StringComparison.Ordinal))
                .Select(element => element.Attribute("Include")?.Value)
                .Where(include => !string.IsNullOrWhiteSpace(include) && !include.Contains("$(", StringComparison.Ordinal))
                .Select(include => include
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar))
                .Select(include => Path.GetFullPath(Path.Combine(webProjectDirectory, include)))
                .Any(referencePath => string.Equals(referencePath, expectedModuleProject, comparison));

            if (!hasSourceReference)
            {
                throw new InvalidOperationException(
                    $"Senparc.Web.csproj 未直接引用 {Path.GetFileName(paths.ModuleProjectFilePath)}。" +
                    "为避免预览误用 NuGet 或旧 DLL，请先添加指向当前 XNCF 源码项目的 ProjectReference。");
            }
        }

        /// <summary>
        /// Creates the dotnet publish process configuration for the preview host.
        /// </summary>
        /// <param name="webProjectFilePath">The host project file.</param>
        /// <param name="publishDirectory">The isolated publish directory.</param>
        /// <returns>The configured process start information.</returns>
        internal static ProcessStartInfo CreatePublishStartInfo(string webProjectFilePath, string publishDirectory)
        {
            var startInfo = CreateDotNetStartInfo(Path.GetDirectoryName(webProjectFilePath));
            startInfo.ArgumentList.Add("publish");
            startInfo.ArgumentList.Add(webProjectFilePath);
            startInfo.ArgumentList.Add("--no-restore");
            startInfo.ArgumentList.Add("--no-self-contained");
            startInfo.ArgumentList.Add("--configuration");
            startInfo.ArgumentList.Add("Debug");
            startInfo.ArgumentList.Add("--output");
            startInfo.ArgumentList.Add(publishDirectory);
            startInfo.ArgumentList.Add("--disable-build-servers");
            startInfo.ArgumentList.Add("-m:1");
            startInfo.ArgumentList.Add("/p:UseAppHost=false");
            return startInfo;
        }

        /// <summary>
        /// Creates the dotnet restore process configuration for the preview host.
        /// </summary>
        /// <param name="webProjectFilePath">The host project file.</param>
        /// <returns>The configured process start information.</returns>
        internal static ProcessStartInfo CreateRestoreStartInfo(string webProjectFilePath)
        {
            var startInfo = CreateDotNetStartInfo(Path.GetDirectoryName(webProjectFilePath));
            startInfo.ArgumentList.Add("restore");
            startInfo.ArgumentList.Add(webProjectFilePath);
            startInfo.ArgumentList.Add("--disable-build-servers");
            startInfo.ArgumentList.Add("-m:1");
            return startInfo;
        }

        /// <summary>
        /// Determines whether source project/package metadata requires a restore before publishing.
        /// </summary>
        /// <param name="paths">The resolved preview project paths.</param>
        /// <returns><see langword="true"/> when restore inputs are newer than the relevant assets.</returns>
        internal static bool RequiresRestore(XncfPreviewProjectPaths paths)
        {
            return ProjectRequiresRestore(paths.WebProjectFilePath)
                   || ProjectRequiresRestore(paths.ModuleProjectFilePath);
        }

        /// <summary>
        /// Creates the dotnet host process configuration for the published preview.
        /// </summary>
        /// <param name="publishDirectory">The directory containing the published host.</param>
        /// <param name="port">The loopback port exposed by the host.</param>
        /// <param name="environmentName">The ASP.NET Core environment name.</param>
        /// <returns>The configured process start information.</returns>
        internal static ProcessStartInfo CreateWebStartInfo(string publishDirectory, int port, string environmentName)
        {
            var startInfo = CreateDotNetStartInfo(publishDirectory);
            SanitizePreviewEnvironment(startInfo.Environment);
            startInfo.ArgumentList.Add("Senparc.Web.dll");
            startInfo.ArgumentList.Add($"--urls=http://127.0.0.1:{port}");
            startInfo.ArgumentList.Add($"--environment={environmentName}");
            startInfo.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = environmentName;
            startInfo.Environment["DOTNET_ENVIRONMENT"] = environmentName;
            // File-system watchers can block Host creation on mounted development volumes.
            // Preview configuration is immutable after publication, so reloading is unnecessary.
            startInfo.Environment["DOTNET_hostBuilder__reloadConfigOnChange"] = "false";
            startInfo.Environment["DOTNET_USE_POLLING_FILE_WATCHER"] = "1";
            startInfo.Environment["NCF_XNCF_PREVIEW"] = "1";

            if (string.Equals(environmentName, DefaultEnvironmentName, StringComparison.OrdinalIgnoreCase))
            {
                // Default preview instances use a database and cache located in their own publish directory.
                startInfo.Environment["SenparcCoreSetting__DatabaseName"] = "Local";
                startInfo.Environment["SenparcCoreSetting__DatabaseType"] = "Sqlite";
                startInfo.Environment["SenparcCoreSetting__CacheType"] = "Local";
                startInfo.Environment["SenparcCoreSetting__IsTestSite"] = "true";
                startInfo.Environment["SenparcSetting__Cache_Redis_Configuration"] = "#{Cache_Redis_Configuration}#";
                startInfo.Environment["SenparcSetting__Cache_Memcached_Configuration"] = "#{Cache_Memcached_Configuration}#";
            }

            return startInfo;
        }

        /// <summary>
        /// Removes environment variables that are not allowed to flow into a preview process.
        /// </summary>
        /// <param name="environment">The environment dictionary to sanitize.</param>
        internal static void SanitizePreviewEnvironment(IDictionary<string, string> environment)
        {
            var allowedValues = environment
                .Where(item => PreviewEnvironmentAllowList.Contains(item.Key))
                .ToArray();
            environment.Clear();
            foreach (var item in allowedValues)
            {
                environment[item.Key] = item.Value;
            }
        }

        /// <summary>
        /// Creates common dotnet process-start settings for preview commands.
        /// </summary>
        /// <param name="workingDirectory">The process working directory.</param>
        /// <returns>The base process-start configuration.</returns>
        private static ProcessStartInfo CreateDotNetStartInfo(string workingDirectory)
        {
            return new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
        }

        /// <summary>
        /// Determines whether a project file or NuGet metadata is newer than its assets file.
        /// </summary>
        /// <param name="projectFilePath">The project file path.</param>
        /// <returns><see langword="true"/> when restore appears necessary.</returns>
        private static bool ProjectRequiresRestore(string projectFilePath)
        {
            var assetsFilePath = Path.Combine(
                Path.GetDirectoryName(projectFilePath) ?? string.Empty,
                "obj",
                "project.assets.json");
            return !File.Exists(assetsFilePath)
                   || File.GetLastWriteTimeUtc(projectFilePath) > File.GetLastWriteTimeUtc(assetsFilePath);
        }

        /// <summary>
        /// Validates preview startup paths, names, ports, and environment settings.
        /// </summary>
        /// <param name="options">The startup options to validate.</param>
        /// <exception cref="InvalidOperationException">Thrown when an option is invalid.</exception>
        private static void ValidateOptions(XncfPreviewStartOptions options)
        {
            if (options.Port is > 0 and < 1024 or > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(options.Port), "预览端口必须为 0，或位于 1024 到 65535 之间。");
            }

            if (options.StartupTimeoutSeconds is < 10 or > 600)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options.StartupTimeoutSeconds),
                    "预览启动超时时间必须位于 10 到 600 秒之间。");
            }

            if (!string.IsNullOrWhiteSpace(options.EnvironmentName)
                && options.EnvironmentName.Length > 50)
            {
                throw new ArgumentException("预览环境名称不能超过 50 个字符。", nameof(options.EnvironmentName));
            }

            if (!string.IsNullOrWhiteSpace(options.EnvironmentName)
                && options.EnvironmentName.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_')))
            {
                throw new ArgumentException("预览环境名称只能包含字母、数字、点、连字符和下划线。", nameof(options.EnvironmentName));
            }
        }

        /// <summary>
        /// Runs a preview command, captures output, and returns its exit code and output text.
        /// </summary>
        /// <param name="startInfo">The process-start configuration.</param>
        /// <param name="log">Optional output callback.</param>
        /// <param name="cancellationToken">Token used to stop the process.</param>
        /// <returns>A task containing command result information.</returns>
        private async Task<CommandResult> RunCommandAsync(
            ProcessStartInfo startInfo,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                throw new InvalidOperationException("无法启动 dotnet publish 进程。");
            }

            var standardOutputTask = ReadOutputAsync(process.StandardOutput, log, cancellationToken);
            var standardErrorTask = ReadOutputAsync(process.StandardError, log, cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                throw;
            }

            var output = string.Join(Environment.NewLine, new[]
            {
                await standardOutputTask.ConfigureAwait(false),
                await standardErrorTask.ConfigureAwait(false)
            }.Where(z => !string.IsNullOrWhiteSpace(z)));

            return new CommandResult(process.ExitCode, output);
        }

        /// <summary>
        /// Reads a process output stream to completion while forwarding each line to a callback.
        /// </summary>
        /// <param name="reader">The process output reader.</param>
        /// <param name="log">Optional line callback.</param>
        /// <returns>A task containing the complete output text.</returns>
        private static async Task<string> ReadOutputAsync(
            StreamReader reader,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            var output = new StringBuilder();
            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
            {
                output.AppendLine(line);
                WriteLog(log, line);
            }
            return output.ToString();
        }

        /// <summary>
        /// Attaches asynchronous stdout/stderr handlers to a preview process.
        /// </summary>
        /// <param name="process">The process whose output is observed.</param>
        /// <param name="state">The preview state receiving output lines.</param>
        private void AttachOutput(Process process, PreviewProcessState state)
        {
            process.OutputDataReceived += (_, args) => AppendProcessOutput(state, args.Data);
            process.ErrorDataReceived += (_, args) => AppendProcessOutput(state, args.Data);
            process.Exited += async (_, _) =>
            {
                var exitCode = TryGetExitCode(process);
                lock (state.SyncRoot)
                {
                    state.ExitCode = exitCode;
                    state.StoppedAt ??= DateTimeOffset.Now;
                }
                AppendProcessOutput(state, $"预览进程已退出，ExitCode：{exitCode}");
                _logger?.LogInformation(
                    "XNCF preview process exited. Session: {SessionId}, ExitCode: {ExitCode}",
                    state.SessionId,
                    exitCode);

                var stage = GetStage(state);
                if (!stage.IsTerminal() && stage != XncfPreviewStage.Stopping)
                {
                    SetStage(
                        state,
                        XncfPreviewStage.Failed,
                        "预览进程意外退出。",
                        $"ExitCode: {exitCode}");
                    if (_activeSessionIds.TryGetValue(state.ModuleProjectName, out var activeSessionId)
                        && string.Equals(activeSessionId, state.SessionId, StringComparison.OrdinalIgnoreCase))
                    {
                        _activeSessionIds.TryRemove(state.ModuleProjectName, out _);
                    }
                    TryDeleteSessionDirectory(state.PublishDirectory);
                    TrimTerminalHistory();
                    await TryPersistStateAsync(state).ConfigureAwait(false);
                }
            };
        }

        /// <summary>
        /// Appends one process-output line to bounded session history.
        /// </summary>
        /// <param name="state">The preview state receiving the line.</param>
        /// <param name="line">The output line.</param>
        private static void AppendProcessOutput(PreviewProcessState state, string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            state.Output.Enqueue($"[{DateTimeOffset.Now:HH:mm:ss}] {line}");
            while (state.Output.Count > MaxLogLines)
            {
                state.Output.TryDequeue(out _);
            }

            WriteLog(state.StartupLog, line);
        }

        /// <summary>
        /// Polls the preview health endpoint until it responds successfully or the timeout expires.
        /// </summary>
        /// <param name="state">The preview process state.</param>
        /// <param name="cancellationToken">Token used to stop health polling.</param>
        /// <returns>A task that completes when the host is healthy.</returns>
        /// <exception cref="InvalidOperationException">Thrown when health cannot be established.</exception>
        private static async Task WaitForHealthyAsync(
            PreviewProcessState state,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };
            var deadline = DateTimeOffset.UtcNow.Add(timeout);

            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var process = state.Process
                    ?? throw new InvalidOperationException("预览进程尚未创建。");
                if (process.HasExited)
                {
                    throw new InvalidOperationException(
                        $"预览进程在健康检查完成前退出（ExitCode：{process.ExitCode}）。{GetOutputTail(string.Join(Environment.NewLine, state.Output))}");
                }

                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, state.Url);
                    using var response = await httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken).ConfigureAwait(false);
                    if ((int)response.StatusCode < 500)
                    {
                        return;
                    }

                    await Task.Delay(300, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    await Task.Delay(300, cancellationToken).ConfigureAwait(false);
                }
            }

            throw new TimeoutException(
                $"预览进程未在 {timeout.TotalSeconds:0} 秒内通过健康检查。{GetOutputTail(string.Join(Environment.NewLine, state.Output))}");
        }

        /// <summary>
        /// Stops a preview process, waits for exit, and optionally removes its publish directory.
        /// </summary>
        /// <param name="state">The preview state to stop.</param>
        /// <param name="deleteFiles">Whether the publish directory should be deleted.</param>
        /// <param name="log">Optional output callback.</param>
        /// <param name="cancellationToken">Token used to bound shutdown.</param>
        /// <returns>A task that completes after process cleanup.</returns>
        private static async Task StopStateAsync(
            PreviewProcessState state,
            bool deleteFiles,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            Process process;
            string publishDirectory;
            lock (state.SyncRoot)
            {
                process = state.Process;
                publishDirectory = state.PublishDirectory;
            }

            try
            {
                if (process != null && !process.HasExited)
                {
                    WriteLog(log, $"正在停止预览进程：{state.ProcessId}");
                    TryKill(process);
                    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (InvalidOperationException)
            {
                // The process was never started or has already been disposed.
            }
            finally
            {
                if (process != null)
                {
                    lock (state.SyncRoot)
                    {
                        state.ExitCode = TryGetExitCode(process);
                        state.StoppedAt ??= DateTimeOffset.Now;
                    }
                }
                process?.Dispose();
            }

            if (deleteFiles)
            {
                TryDeleteSessionDirectory(publishDirectory);
            }
        }

        /// <summary>
        /// Best-effort deletes a preview publish directory and suppresses cleanup failures.
        /// </summary>
        /// <param name="publishDirectory">The directory to delete.</param>
        private static void TryDeleteSessionDirectory(string publishDirectory)
        {
            if (string.IsNullOrWhiteSpace(publishDirectory))
            {
                return;
            }

            try
            {
                var sessionDirectory = Directory.GetParent(publishDirectory)?.FullName;
                if (!string.IsNullOrEmpty(sessionDirectory) && Directory.Exists(sessionDirectory))
                {
                    Directory.Delete(sessionDirectory, recursive: true);
                }
            }
            catch
            {
                // A file can remain in use briefly after process termination; the OS temporary directory retains it.
            }
        }

        /// <summary>
        /// Gets an operating-system-assigned available loopback TCP port.
        /// </summary>
        /// <returns>The available port number.</returns>
        private static int GetAvailableLoopbackPort()
        {
            using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>
        /// Verifies that a requested loopback port can be bound.
        /// </summary>
        /// <param name="port">The port to test.</param>
        /// <exception cref="InvalidOperationException">Thrown when the port is unavailable.</exception>
        private static void EnsurePortAvailable(int port)
        {
            try
            {
                using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
            }
            catch (SocketException ex)
            {
                throw new InvalidOperationException($"预览端口 {port} 已被占用。", ex);
            }
        }

        /// <summary>
        /// Computes a deterministic SHA-256 fingerprint over source files relevant to preview publishing.
        /// </summary>
        /// <param name="moduleDirectory">The module source directory.</param>
        /// <returns>The lowercase hexadecimal source fingerprint.</returns>
        internal static string ComputeSourceFingerprint(string moduleDirectory)
        {
            if (string.IsNullOrWhiteSpace(moduleDirectory) || !Directory.Exists(moduleDirectory))
            {
                throw new DirectoryNotFoundException($"未找到用于计算源码指纹的 XNCF 模块目录：{moduleDirectory}");
            }

            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var root = Path.GetFullPath(moduleDirectory);
            foreach (var filePath in EnumerateSourceFiles(root)
                         .OrderBy(path => Path.GetRelativePath(root, path), StringComparer.Ordinal))
            {
                var relativePath = Path.GetRelativePath(root, filePath).Replace('\\', '/');
                var relativePathBytes = Encoding.UTF8.GetBytes(relativePath);
                hash.AppendData(BitConverter.GetBytes(relativePathBytes.Length));
                hash.AppendData(relativePathBytes);

                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var buffer = new byte[81920];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    hash.AppendData(buffer, 0, bytesRead);
                }
            }

            return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        }

        /// <summary>
        /// Enumerates source files below a directory while excluding build, VCS, and package directories.
        /// </summary>
        /// <param name="rootDirectory">The directory to enumerate.</param>
        /// <returns>Normalized source file paths.</returns>
        private static IEnumerable<string> EnumerateSourceFiles(string rootDirectory)
        {
            var pending = new Stack<string>();
            pending.Push(rootDirectory);

            while (pending.Count > 0)
            {
                var currentDirectory = pending.Pop();
                foreach (var filePath in Directory.EnumerateFiles(currentDirectory))
                {
                    if (!File.GetAttributes(filePath).HasFlag(FileAttributes.ReparsePoint))
                    {
                        yield return filePath;
                    }
                }

                foreach (var directoryPath in Directory.EnumerateDirectories(currentDirectory))
                {
                    var directoryInfo = new DirectoryInfo(directoryPath);
                    if (SourceFingerprintExcludedDirectories.Contains(directoryInfo.Name)
                        || directoryInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        continue;
                    }

                    pending.Push(directoryInfo.FullName);
                }
            }
        }

        /// <summary>
        /// Computes the SHA-256 digest of one file.
        /// </summary>
        /// <param name="filePath">The file path to hash.</param>
        /// <returns>The lowercase hexadecimal SHA-256 digest.</returns>
        private static string ComputeFileSha256(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }

        /// <summary>
        /// Creates a bounded unique preview session identifier.
        /// </summary>
        /// <param name="moduleProjectName">The module project name used as a prefix.</param>
        /// <returns>A session identifier safe for paths and logs.</returns>
        private static string CreateSessionId(string moduleProjectName)
        {
            var safeName = string.Concat(moduleProjectName.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-'));
            safeName = safeName[..Math.Min(safeName.Length, 48)];
            return $"{safeName}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Converts internal preview state into a thread-safe public session snapshot.
        /// </summary>
        /// <param name="state">The internal preview state.</param>
        /// <param name="includeOutput">Whether recent process output is included.</param>
        /// <returns>The public session information.</returns>
        private static XncfPreviewSessionInfo ToInfo(PreviewProcessState state, bool includeOutput)
        {
            string url;
            int processId;
            int progressPercent;
            DateTimeOffset startedAt;
            string environmentName;
            string sourceFingerprint;
            string moduleAssemblySha256;
            XncfPreviewStage stage;
            string statusMessage;
            DateTimeOffset updatedAt;
            DateTimeOffset? completedAt;
            string errorMessage;
            XncfPreviewHostStatus hostStatus;
            string hostStatusMessage;
            DateTimeOffset? processStartedAt;
            DateTimeOffset? healthyAt;
            DateTimeOffset? stoppedAt;
            int? exitCode;
            Process process;

            lock (state.SyncRoot)
            {
                url = state.Url;
                processId = state.ProcessId;
                progressPercent = state.ProgressPercent;
                startedAt = state.StartedAt;
                environmentName = state.EnvironmentName;
                sourceFingerprint = state.SourceFingerprint;
                moduleAssemblySha256 = state.ModuleAssemblySha256;
                stage = state.Stage;
                statusMessage = state.StatusMessage;
                updatedAt = state.UpdatedAt;
                completedAt = state.CompletedAt;
                errorMessage = state.ErrorMessage;
                hostStatus = state.HostStatus;
                hostStatusMessage = state.HostStatusMessage;
                processStartedAt = state.ProcessStartedAt;
                healthyAt = state.HealthyAt;
                stoppedAt = state.StoppedAt;
                exitCode = state.ExitCode;
                process = state.Process;
            }

            var isRunning = process != null;
            try
            {
                isRunning = process != null && !process.HasExited;
            }
            catch (InvalidOperationException)
            {
                // Process has not started or is no longer available.
            }

            return new XncfPreviewSessionInfo
            {
                SessionId = state.SessionId,
                ModuleProjectName = state.ModuleProjectName,
                SolutionFilePath = state.SolutionFilePath,
                Url = url,
                ProcessId = processId,
                StartedAt = startedAt,
                IsRunning = isRunning,
                Stage = stage,
                ProgressPercent = progressPercent,
                StatusMessage = statusMessage,
                UpdatedAt = updatedAt,
                CompletedAt = completedAt,
                ErrorMessage = errorMessage,
                IsTerminal = stage.IsTerminal(),
                CanStop = stage.CanStop(),
                EnvironmentName = environmentName,
                SourceFingerprint = sourceFingerprint,
                ModuleAssemblySha256 = moduleAssemblySha256,
                RecentOutput = includeOutput ? string.Join(Environment.NewLine, state.Output) : null,
                HostStatus = hostStatus,
                HostStatusMessage = hostStatusMessage,
                ProcessStartedAt = processStartedAt,
                HealthyAt = healthyAt,
                StoppedAt = stoppedAt,
                ExitCode = exitCode,
                PublishDirectory = state.PublishDirectory
            };
        }

        /// <summary>
        /// Reads a preview stage under the session synchronization lock.
        /// </summary>
        /// <param name="state">The preview state.</param>
        /// <returns>The current stage.</returns>
        private static XncfPreviewStage GetStage(PreviewProcessState state)
        {
            lock (state.SyncRoot)
            {
                return state.Stage;
            }
        }

        /// <summary>
        /// Updates stage, progress, host status, timestamps, and bounded output diagnostics.
        /// </summary>
        /// <param name="state">The preview state to update.</param>
        /// <param name="stage">The new lifecycle stage.</param>
        /// <param name="statusMessage">The user-facing status message.</param>
        /// <param name="errorMessage">The optional error message.</param>
        private static void SetStage(
            PreviewProcessState state,
            XncfPreviewStage stage,
            string statusMessage,
            string errorMessage)
        {
            var now = DateTimeOffset.Now;
            lock (state.SyncRoot)
            {
                if (stage is not XncfPreviewStage.Failed
                    and not XncfPreviewStage.Cancelled
                    and not XncfPreviewStage.Interrupted)
                {
                    state.ProgressPercent = stage.GetProgressPercent();
                }
                state.Stage = stage;
                state.StatusMessage = statusMessage;
                state.ErrorMessage = errorMessage;
                state.UpdatedAt = now;
                state.CompletedAt = stage.IsTerminal() ? now : null;

                if (state.Process != null || state.HostStatus != XncfPreviewHostStatus.NotCreated)
                {
                    state.HostStatus = stage switch
                    {
                        XncfPreviewStage.Starting => XncfPreviewHostStatus.Starting,
                        XncfPreviewStage.HealthChecking => XncfPreviewHostStatus.HealthChecking,
                        XncfPreviewStage.Replacing or XncfPreviewStage.Running => XncfPreviewHostStatus.Healthy,
                        XncfPreviewStage.Stopping => XncfPreviewHostStatus.Stopping,
                        XncfPreviewStage.Stopped or XncfPreviewStage.Cancelled => XncfPreviewHostStatus.Stopped,
                        XncfPreviewStage.Replaced => XncfPreviewHostStatus.Replaced,
                        XncfPreviewStage.Failed => XncfPreviewHostStatus.Failed,
                        XncfPreviewStage.Interrupted => XncfPreviewHostStatus.Interrupted,
                        _ => state.HostStatus
                    };
                    state.HostStatusMessage = statusMessage;
                }
            }

            AppendProcessOutput(state, $"[{stage}] {statusMessage}");
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                AppendProcessOutput(state, errorMessage);
            }
        }

        /// <summary>
        /// Updates a preview stage and persists the resulting state when persistence is available.
        /// </summary>
        /// <param name="state">The preview state to update.</param>
        /// <param name="stage">The new lifecycle stage.</param>
        /// <param name="statusMessage">The user-facing status message.</param>
        /// <param name="errorMessage">The optional error message.</param>
        /// <param name="cancellationToken">Token used for persistence.</param>
        private async Task SetStageAndPersistAsync(
            PreviewProcessState state,
            XncfPreviewStage stage,
            string statusMessage,
            string errorMessage,
            CancellationToken cancellationToken)
        {
            SetStage(state, stage, statusMessage, errorMessage);
            await PersistStateAsync(state, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Persists a state snapshot when the optional state store is available;
        /// persistence failures downgrade to in-memory operation.
        /// </summary>
        /// <param name="state">The preview state to persist.</param>
        /// <param name="cancellationToken">Token used for the persistence operation.</param>
        private async Task PersistStateAsync(
            PreviewProcessState state,
            CancellationToken cancellationToken)
        {
            if (_stateStore == null || !IsPersistenceAvailable())
            {
                return;
            }

            try
            {
                await _stateStore.SaveAsync(CreatePersistenceSnapshot(state), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                DisablePersistence(
                    "预览状态数据库写入失败，主站和预览任务将继续运行，后续状态暂存于内存。恢复数据库后请重启主站。",
                    ex);
            }
        }

        /// <summary>
        /// Gets whether preview state persistence is currently available.
        /// </summary>
        /// <returns><see langword="true"/> when the state store may be used.</returns>
        private bool IsPersistenceAvailable()
        {
            lock (_persistenceStatusLock)
            {
                return _persistenceAvailable;
            }
        }

        /// <summary>
        /// Marks preview persistence as healthy and clears the previous error status.
        /// </summary>
        private void SetPersistenceAvailable()
        {
            lock (_persistenceStatusLock)
            {
                _persistenceAvailable = true;
                _persistenceStatusMessage = "预览任务与 Host 状态数据库持久化已就绪。";
                _persistenceErrorMessage = null;
                _persistenceStatusUpdatedAt = DateTimeOffset.Now;
            }
        }

        /// <summary>
        /// Disables preview persistence after a store failure while preserving in-memory operation.
        /// </summary>
        /// <param name="statusMessage">The status exposed to callers.</param>
        /// <param name="exception">The failure used for diagnostics.</param>
        private void DisablePersistence(string statusMessage, Exception exception)
        {
            lock (_persistenceStatusLock)
            {
                _persistenceAvailable = false;
                _persistenceStatusMessage = statusMessage;
                _persistenceErrorMessage = exception?.GetBaseException().Message;
                _persistenceStatusUpdatedAt = DateTimeOffset.Now;
            }

            _logger?.LogWarning(
                exception,
                "XNCF preview persistence is unavailable. The host will continue with in-memory state.");
        }

        /// <summary>
        /// Best-effort persists a state snapshot for asynchronous output/event paths.
        /// </summary>
        /// <param name="state">The preview state to persist.</param>
        private async Task TryPersistStateAsync(PreviewProcessState state)
        {
            try
            {
                await PersistStateAsync(state, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Failed to persist XNCF preview state. Session: {SessionId}, Stage: {Stage}",
                    state.SessionId,
                    GetStage(state));
            }
        }

        /// <summary>
        /// Copies synchronized preview state into a bounded persistence snapshot.
        /// </summary>
        /// <param name="state">The internal preview state.</param>
        /// <returns>The persistence snapshot.</returns>
        private static XncfPreviewPersistenceSnapshot CreatePersistenceSnapshot(PreviewProcessState state)
        {
            XncfPreviewPersistenceSnapshot snapshot;
            lock (state.SyncRoot)
            {
                var output = string.Join(Environment.NewLine, state.Output);
                if (output.Length > MaxPersistedOutputChars)
                {
                    output = output[^MaxPersistedOutputChars..];
                }

                snapshot = new XncfPreviewPersistenceSnapshot
                {
                    TenantId = state.TenantId,
                    SessionId = state.SessionId,
                    ModuleProjectName = state.ModuleProjectName,
                    SolutionFilePath = state.SolutionFilePath,
                    Stage = state.Stage,
                    ProgressPercent = state.ProgressPercent,
                    StatusMessage = state.StatusMessage,
                    ErrorMessage = state.ErrorMessage,
                    SourceFingerprint = state.SourceFingerprint,
                    ModuleAssemblySha256 = state.ModuleAssemblySha256,
                    RecentOutput = output,
                    StartedAt = state.StartedAt,
                    UpdatedAt = state.UpdatedAt,
                    CompletedAt = state.CompletedAt,
                    HasHost = state.Process != null || state.HostStatus != XncfPreviewHostStatus.NotCreated,
                    Url = state.Url,
                    ProcessId = state.ProcessId,
                    EnvironmentName = state.EnvironmentName,
                    PublishDirectory = state.PublishDirectory,
                    HostStatus = state.HostStatus,
                    HostStatusMessage = state.HostStatusMessage,
                    ProcessStartedAt = state.ProcessStartedAt,
                    HealthyAt = state.HealthyAt,
                    StoppedAt = state.StoppedAt,
                    ExitCode = state.ExitCode
                };
            }

            return snapshot;
        }

        /// <summary>
        /// Reconstructs an in-memory preview state from persisted snapshot data.
        /// </summary>
        /// <param name="snapshot">The persisted snapshot.</param>
        /// <returns>The restored preview state.</returns>
        private static PreviewProcessState RestoreState(XncfPreviewPersistenceSnapshot snapshot)
        {
            var state = new PreviewProcessState(
                snapshot.SessionId,
                snapshot.ModuleProjectName,
                snapshot.SolutionFilePath,
                startupLog: null,
                startedAt: snapshot.StartedAt);
            lock (state.SyncRoot)
            {
                state.TenantId = snapshot.TenantId;
                state.Stage = snapshot.Stage;
                state.ProgressPercent = snapshot.ProgressPercent;
                state.StatusMessage = snapshot.StatusMessage;
                state.ErrorMessage = snapshot.ErrorMessage;
                state.SourceFingerprint = snapshot.SourceFingerprint;
                state.ModuleAssemblySha256 = snapshot.ModuleAssemblySha256;
                state.UpdatedAt = snapshot.UpdatedAt;
                state.CompletedAt = snapshot.CompletedAt;
                state.Url = snapshot.Url;
                state.ProcessId = snapshot.ProcessId;
                state.EnvironmentName = snapshot.EnvironmentName;
                state.PublishDirectory = snapshot.PublishDirectory;
                state.HostStatus = snapshot.HostStatus;
                state.HostStatusMessage = snapshot.HostStatusMessage;
                state.ProcessStartedAt = snapshot.ProcessStartedAt;
                state.HealthyAt = snapshot.HealthyAt;
                state.StoppedAt = snapshot.StoppedAt;
                state.ExitCode = snapshot.ExitCode;
            }

            if (!string.IsNullOrWhiteSpace(snapshot.RecentOutput))
            {
                foreach (var line in snapshot.RecentOutput.Split(
                             new[] { "\r\n", "\n" },
                             StringSplitOptions.RemoveEmptyEntries))
                {
                    state.Output.Enqueue(line);
                }
            }

            return state;
        }

        /// <summary>
        /// Removes terminal sessions beyond the configured retained-history limit.
        /// </summary>
        private void TrimTerminalHistory()
        {
            var expired = _sessions.Values
                .Where(state => GetStage(state).IsTerminal())
                .OrderByDescending(state => state.CompletedAt ?? state.StartedAt)
                .Skip(MaxRetainedTerminalSessions)
                .ToArray();

            foreach (var state in expired)
            {
                _sessions.TryRemove(state.SessionId, out _);
            }
        }

        /// <summary>
        /// Returns a bounded tail of command output for exception messages.
        /// </summary>
        /// <param name="output">The complete command output.</param>
        /// <returns>A bounded diagnostic tail.</returns>
        private static string GetOutputTail(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return "请查看 XncfBuilder 日志。";
            }

            const int maxLength = 2000;
            var trimmed = output.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[^maxLength..];
        }

        /// <summary>
        /// Reads a process exit code without propagating process-state exceptions.
        /// </summary>
        /// <param name="process">The process to inspect.</param>
        /// <returns>The exit code, or <c>-1</c> when unavailable.</returns>
        private static int TryGetExitCode(Process process)
        {
            try
            {
                return process.ExitCode;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Best-effort terminates a process tree when the process is still running.
        /// </summary>
        /// <param name="process">The process to terminate.</param>
        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // The process may have exited between the checks.
            }
        }

        /// <summary>
        /// Signals all active preview sessions to stop and terminates their host processes.
        /// </summary>
        private void RequestHostStopForActiveSessions()
        {
            foreach (var state in _sessions.Values.ToArray())
            {
                if (GetStage(state).IsTerminal())
                {
                    continue;
                }

                state.StopCancellation.Cancel();
                SetStage(state, XncfPreviewStage.Stopping, "主站正在关闭预览进程。", null);

                Process process;
                lock (state.SyncRoot)
                {
                    process = state.Process;
                }

                if (process != null)
                {
                    TryKill(process);
                }
            }
        }

        /// <summary>
        /// Forwards a non-empty message to an optional preview log callback.
        /// </summary>
        /// <param name="log">The optional callback.</param>
        /// <param name="message">The message to forward.</param>
        private static void WriteLog(Action<string> log, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                log?.Invoke(message);
            }
        }

        /// <summary>
        /// Contains the resolved solution, web-host, and XNCF module project paths.
        /// </summary>
        internal sealed record XncfPreviewProjectPaths(
            string SolutionDirectory,
            string WebProjectFilePath,
            string ModuleProjectFilePath);

        /// <summary>
        /// Contains a preview command exit code and captured output.
        /// </summary>
        private sealed record CommandResult(int ExitCode, string Output);

        private sealed class PreviewProcessState
        {
            public PreviewProcessState(
                string sessionId,
                string moduleProjectName,
                string solutionFilePath,
                Action<string> startupLog,
                DateTimeOffset? startedAt = null)
            {
                SessionId = sessionId;
                ModuleProjectName = moduleProjectName;
                SolutionFilePath = solutionFilePath;
                StartupLog = startupLog;
                StartedAt = startedAt ?? DateTimeOffset.Now;
                UpdatedAt = StartedAt;
                Stage = XncfPreviewStage.Queued;
                ProgressPercent = XncfPreviewStage.Queued.GetProgressPercent();
            }

            public object SyncRoot { get; } = new();

            public string SessionId { get; }

            public string ModuleProjectName { get; }

            public string SolutionFilePath { get; }

            /// <summary>
            /// Gets or sets the tenant identifier associated with the preview session.
            /// </summary>
            public int TenantId { get; set; }

            public string Url { get; set; }

            public string EnvironmentName { get; set; }

            public string PublishDirectory { get; set; }

            public string SourceFingerprint { get; set; }

            public string ModuleAssemblySha256 { get; set; }

            public Process Process { get; set; }

            public XncfPreviewStage Stage { get; set; }

            public int ProgressPercent { get; set; }

            public string StatusMessage { get; set; }

            public string ErrorMessage { get; set; }

            public DateTimeOffset UpdatedAt { get; set; }

            public DateTimeOffset? CompletedAt { get; set; }

            public Action<string> StartupLog { get; set; }

            public CancellationTokenSource StopCancellation { get; } = new();

            public ConcurrentQueue<string> Output { get; } = new();

            public DateTimeOffset StartedAt { get; }

            public int ProcessId { get; set; }

            public XncfPreviewHostStatus HostStatus { get; set; }

            public string HostStatusMessage { get; set; }

            public DateTimeOffset? ProcessStartedAt { get; set; }

            public DateTimeOffset? HealthyAt { get; set; }

            public DateTimeOffset? StoppedAt { get; set; }

            public int? ExitCode { get; set; }
        }
    }
}
