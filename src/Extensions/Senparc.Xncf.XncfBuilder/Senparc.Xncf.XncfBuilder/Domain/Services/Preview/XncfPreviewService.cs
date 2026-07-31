/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfPreviewService.cs
    文件功能描述：在独立进程中构建、启动和切换 XNCF 预览实例


    创建标识：Senparc - 20260801

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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Preview
{
    public sealed class XncfPreviewService : IXncfPreviewService, IHostedService
    {
        public const string DefaultEnvironmentName = "XncfPreview";

        private const int MaxLogLines = 300;
        private readonly ILogger<XncfPreviewService> _logger;
        private readonly ConcurrentDictionary<string, PreviewProcessState> _sessions = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, string> _activeSessionIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _operationLock = new(1, 1);
        private readonly string _previewRoot;

        public XncfPreviewService(ILogger<XncfPreviewService> logger = null)
        {
            _logger = logger;
            _previewRoot = Path.Combine(Path.GetTempPath(), "Senparc.Ncf", "XncfPreview");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(_previewRoot);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                foreach (var state in _sessions.Values.ToArray())
                {
                    await StopStateAsync(state, deleteFiles: true, log: null, cancellationToken).ConfigureAwait(false);
                }

                _sessions.Clear();
                _activeSessionIds.Clear();
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public async Task<XncfPreviewSessionInfo> StartAsync(
            XncfPreviewStartOptions options,
            Action<string> log = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            ValidateOptions(options);

            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            PreviewProcessState newState = null;
            string publishDirectory = null;
            try
            {
                var paths = ResolveProjectPaths(options.SolutionFilePath, options.ModuleProjectName);
                var sessionId = CreateSessionId(options.ModuleProjectName);
                publishDirectory = Path.Combine(_previewRoot, sessionId, "app");
                Directory.CreateDirectory(publishDirectory);

                WriteLog(log, $"准备 XNCF 预览：{options.ModuleProjectName}");
                WriteLog(log, $"预览发布目录：{publishDirectory}");

                if (RequiresRestore(paths))
                {
                    WriteLog(log, "检测到新项目或包引用变化，正在执行一次必要的 dotnet restore。");
                    var restoreStartInfo = CreateRestoreStartInfo(paths.WebProjectFilePath);
                    var restoreResult = await RunCommandAsync(restoreStartInfo, log, cancellationToken).ConfigureAwait(false);
                    if (restoreResult.ExitCode != 0)
                    {
                        throw new InvalidOperationException(
                            $"预览宿主还原失败（退出码：{restoreResult.ExitCode}）。{GetOutputTail(restoreResult.Output)}");
                    }
                }

                var publishStartInfo = CreatePublishStartInfo(paths.WebProjectFilePath, publishDirectory);
                var publishResult = await RunCommandAsync(publishStartInfo, log, cancellationToken).ConfigureAwait(false);
                if (publishResult.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"预览宿主发布失败（退出码：{publishResult.ExitCode}）。{GetOutputTail(publishResult.Output)}");
                }

                var moduleAssemblyPath = Path.Combine(publishDirectory, $"{options.ModuleProjectName}.dll");
                if (!File.Exists(moduleAssemblyPath))
                {
                    throw new InvalidOperationException(
                        $"预览发布结果中没有找到 {options.ModuleProjectName}.dll。请确认 Senparc.Web 已引用该 XNCF 项目。");
                }

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

                var webStartInfo = CreateWebStartInfo(publishDirectory, port, environmentName);
                var process = new Process
                {
                    StartInfo = webStartInfo,
                    EnableRaisingEvents = true
                };

                newState = new PreviewProcessState(
                    sessionId,
                    options.ModuleProjectName,
                    url,
                    environmentName,
                    publishDirectory,
                    process,
                    log);

                AttachOutput(process, newState);

                if (!process.Start())
                {
                    throw new InvalidOperationException("无法启动 XNCF 预览进程。");
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                newState.ProcessId = process.Id;
                WriteLog(log, $"预览进程已启动，PID：{process.Id}，等待健康检查：{url}");

                await WaitForHealthyAsync(
                    newState,
                    TimeSpan.FromSeconds(options.StartupTimeoutSeconds),
                    cancellationToken).ConfigureAwait(false);
                newState.StartupLog = null;

                _sessions[sessionId] = newState;

                if (_activeSessionIds.TryGetValue(options.ModuleProjectName, out var oldSessionId)
                    && _sessions.TryGetValue(oldSessionId, out var oldState)
                    && !string.Equals(oldSessionId, sessionId, StringComparison.OrdinalIgnoreCase))
                {
                    WriteLog(log, $"新预览已就绪，正在停止旧预览：{oldSessionId}");
                    await StopStateAsync(oldState, deleteFiles: true, log, CancellationToken.None).ConfigureAwait(false);
                    _sessions.TryRemove(oldSessionId, out _);
                }

                _activeSessionIds[options.ModuleProjectName] = sessionId;
                WriteLog(log, $"XNCF 预览已就绪：{url}");
                return ToInfo(newState, includeOutput: true);
            }
            catch
            {
                if (newState != null)
                {
                    await StopStateAsync(newState, deleteFiles: true, log, CancellationToken.None).ConfigureAwait(false);
                }
                else if (!string.IsNullOrEmpty(publishDirectory))
                {
                    TryDeleteSessionDirectory(publishDirectory);
                }
                throw;
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public async Task<bool> StopAsync(
            string sessionId,
            Action<string> log = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return false;
            }

            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!_sessions.TryRemove(sessionId.Trim(), out var state))
                {
                    return false;
                }

                await StopStateAsync(state, deleteFiles: true, log, cancellationToken).ConfigureAwait(false);
                if (_activeSessionIds.TryGetValue(state.ModuleProjectName, out var activeSessionId)
                    && string.Equals(activeSessionId, state.SessionId, StringComparison.OrdinalIgnoreCase))
                {
                    _activeSessionIds.TryRemove(state.ModuleProjectName, out _);
                }

                return true;
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public IReadOnlyList<XncfPreviewSessionInfo> GetSessions(bool includeOutput = false)
        {
            return _sessions.Values
                .OrderByDescending(z => z.StartedAt)
                .Select(z => ToInfo(z, includeOutput))
                .ToArray();
        }

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

        internal static ProcessStartInfo CreateRestoreStartInfo(string webProjectFilePath)
        {
            var startInfo = CreateDotNetStartInfo(Path.GetDirectoryName(webProjectFilePath));
            startInfo.ArgumentList.Add("restore");
            startInfo.ArgumentList.Add(webProjectFilePath);
            startInfo.ArgumentList.Add("--disable-build-servers");
            startInfo.ArgumentList.Add("-m:1");
            return startInfo;
        }

        internal static bool RequiresRestore(XncfPreviewProjectPaths paths)
        {
            return ProjectRequiresRestore(paths.WebProjectFilePath)
                   || ProjectRequiresRestore(paths.ModuleProjectFilePath);
        }

        internal static ProcessStartInfo CreateWebStartInfo(string publishDirectory, int port, string environmentName)
        {
            var startInfo = CreateDotNetStartInfo(publishDirectory);
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

        private static bool ProjectRequiresRestore(string projectFilePath)
        {
            var assetsFilePath = Path.Combine(
                Path.GetDirectoryName(projectFilePath) ?? string.Empty,
                "obj",
                "project.assets.json");
            return !File.Exists(assetsFilePath)
                   || File.GetLastWriteTimeUtc(projectFilePath) > File.GetLastWriteTimeUtc(assetsFilePath);
        }

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

        private void AttachOutput(Process process, PreviewProcessState state)
        {
            process.OutputDataReceived += (_, args) => AppendProcessOutput(state, args.Data);
            process.ErrorDataReceived += (_, args) => AppendProcessOutput(state, args.Data);
            process.Exited += (_, _) =>
            {
                var exitCode = TryGetExitCode(process);
                AppendProcessOutput(state, $"预览进程已退出，ExitCode：{exitCode}");
                _logger?.LogInformation(
                    "XNCF preview process exited. Session: {SessionId}, ExitCode: {ExitCode}",
                    state.SessionId,
                    exitCode);
            };
        }

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
                if (state.Process.HasExited)
                {
                    throw new InvalidOperationException(
                        $"预览进程在健康检查完成前退出（ExitCode：{state.Process.ExitCode}）。{GetOutputTail(string.Join(Environment.NewLine, state.Output))}");
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

        private static async Task StopStateAsync(
            PreviewProcessState state,
            bool deleteFiles,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!state.Process.HasExited)
                {
                    WriteLog(log, $"正在停止预览进程：{state.ProcessId}");
                    TryKill(state.Process);
                    await state.Process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (InvalidOperationException)
            {
                // The process was never started or has already been disposed.
            }
            finally
            {
                state.Process.Dispose();
            }

            if (deleteFiles)
            {
                TryDeleteSessionDirectory(state.PublishDirectory);
            }
        }

        private static void TryDeleteSessionDirectory(string publishDirectory)
        {
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

        private static int GetAvailableLoopbackPort()
        {
            using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

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

        private static string CreateSessionId(string moduleProjectName)
        {
            var safeName = string.Concat(moduleProjectName.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-'));
            safeName = safeName[..Math.Min(safeName.Length, 48)];
            return $"{safeName}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        }

        private static XncfPreviewSessionInfo ToInfo(PreviewProcessState state, bool includeOutput)
        {
            var isRunning = false;
            try
            {
                isRunning = !state.Process.HasExited;
            }
            catch (InvalidOperationException)
            {
                // Process has not started or is no longer available.
            }

            return new XncfPreviewSessionInfo
            {
                SessionId = state.SessionId,
                ModuleProjectName = state.ModuleProjectName,
                Url = state.Url,
                ProcessId = state.ProcessId,
                StartedAt = state.StartedAt,
                IsRunning = isRunning,
                EnvironmentName = state.EnvironmentName,
                RecentOutput = includeOutput ? string.Join(Environment.NewLine, state.Output) : null
            };
        }

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

        private static void WriteLog(Action<string> log, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                log?.Invoke(message);
            }
        }

        internal sealed record XncfPreviewProjectPaths(
            string SolutionDirectory,
            string WebProjectFilePath,
            string ModuleProjectFilePath);

        private sealed record CommandResult(int ExitCode, string Output);

        private sealed class PreviewProcessState
        {
            public PreviewProcessState(
                string sessionId,
                string moduleProjectName,
                string url,
                string environmentName,
                string publishDirectory,
                Process process,
                Action<string> startupLog)
            {
                SessionId = sessionId;
                ModuleProjectName = moduleProjectName;
                Url = url;
                EnvironmentName = environmentName;
                PublishDirectory = publishDirectory;
                Process = process;
                StartupLog = startupLog;
                StartedAt = DateTimeOffset.Now;
            }

            public string SessionId { get; }

            public string ModuleProjectName { get; }

            public string Url { get; }

            public string EnvironmentName { get; }

            public string PublishDirectory { get; }

            public Process Process { get; }

            public Action<string> StartupLog { get; set; }

            public ConcurrentQueue<string> Output { get; } = new();

            public DateTimeOffset StartedAt { get; }

            public int ProcessId { get; set; }
        }
    }
}
