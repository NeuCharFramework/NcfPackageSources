/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FineTuningWorkerSupervisor.cs
    文件功能描述：从 NCF 启动、停止并观察本机微调 Worker


    创建标识：Senparc - 20261106

-----------------------------------------------------------

    修改标识：Senparc - 20261108
    修改描述：v0.18.0 新增微调 Worker 监督服务
----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Xncf.AIKernel.Domain.Models.FineTuning;
using Senparc.Xncf.AIKernel.Models;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public sealed class FineTuningWorkerSupervisor : IHostedService, IDisposable
{
    private const int LogLimit = 200;
    private readonly IBackgroundTenantScopeFactory _tenantScopeFactory;
    private readonly FineTuningSecretStore _secrets;
    private readonly FineTuningRuntimeSettings _runtime;
    private readonly FineTuningWorkerOptions _options;
    private readonly ILogger<FineTuningWorkerSupervisor> _logger;
    private readonly ConcurrentDictionary<string, Session> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource _shutdown = new();

    public FineTuningWorkerSupervisor(IBackgroundTenantScopeFactory tenantScopeFactory, FineTuningSecretStore secrets,
        FineTuningRuntimeSettings runtime, IOptions<FineTuningWorkerOptions> options,
        ILogger<FineTuningWorkerSupervisor> logger)
    {
        _tenantScopeFactory = tenantScopeFactory;
        _secrets = secrets;
        _runtime = runtime;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _runtime.ApplyStartup();
        _ = AutoStartAsync(_shutdown.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _shutdown.CancelAsync().ConfigureAwait(false);
        foreach (var alias in _sessions.Keys.ToArray())
        {
            try
            {
                await StopWorkerAsync(alias, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to stop managed fine-tuning worker {Alias} during shutdown.", alias);
            }
        }
    }

    public async Task<FineTuningManagedWorkerStatus> StartAsync(AIFineTuningWorker worker,
        IReadOnlyList<AIFineTuningWorker> peers, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_sessions.TryRemove(worker.Alias, out var existing))
        {
            if (existing.IsRunning)
            {
                _sessions[worker.Alias] = existing;
                Debug.WriteLine($"[AIKernel] Managed fine-tuning worker '{worker.Alias}' is already running as pid {existing.ProcessId}.");
                return GetStatus(worker);
            }

            existing.DisposeProcess();
        }

        if (!FineTuningLaunchProfile.TryFromJson(worker.LaunchProfileJson, out var profile) || !profile.Managed)
            throw new NcfExceptionBase("This fine-tuning worker is not configured to be started by NCF.");
        _runtime.SetEnabled(true);
        var apiKey = _secrets.Resolve(_options, worker.Alias);
        var launch = FineTuningWorkerLaunch.Create(worker.Alias, profile, apiKey,
            peers.Select(item => new FineTuningWorkerPeer(item.Alias, item.LaunchProfileJson)));
        var session = new Session(worker.Alias, apiKey, launch);
        _sessions[worker.Alias] = session;
        try
        {
            session.Start();
            _logger.LogInformation(
                "Started managed fine-tuning worker {Alias} pid={Pid} project={Project} models={Models} data={Data} bind={Host}:{Port}.",
                worker.Alias, session.ProcessId, launch.WorkingDirectory, launch.ModelsRoot, launch.DataRoot, launch.BindHost, launch.Port);
            Debug.WriteLine($"[AIKernel] Started managed fine-tuning worker '{worker.Alias}' pid={session.ProcessId} bind={launch.BindHost}:{launch.Port}.");
            try
            {
                await Task.Delay(800, _shutdown.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }

            if (!session.IsRunning)
            {
                var message = "The fine-tuning worker exited during startup.";
                var logs = session.Logs.TakeLast(5).ToArray();
                if (logs.Length > 0)
                    message += " " + string.Join(" | ", logs);
                session.Error = message;
                throw new NcfExceptionBase(message);
            }
        }
        catch
        {
            _sessions.TryRemove(worker.Alias, out _);
            session.DisposeProcess();
            throw;
        }

        return GetStatus(worker);
    }

    public async Task StopWorkerAsync(string alias, CancellationToken cancellationToken)
    {
        if (!_sessions.TryGetValue(alias, out var session))
            return;
        await session.StopAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Stopped managed fine-tuning worker {Alias} pid={Pid}.", alias, session.ProcessId);
        Debug.WriteLine($"[AIKernel] Stopped managed fine-tuning worker '{alias}' pid={session.ProcessId}.");
    }

    public FineTuningManagedWorkerStatus GetStatus(AIFineTuningWorker worker)
    {
        var status = new FineTuningManagedWorkerStatus
        {
            Alias = worker.Alias,
            State = "Stopped",
            Endpoint = worker.Endpoint
        };
        try
        {
            status.SecretPath = _secrets.PathFor(worker.Alias);
        }
        catch (NcfExceptionBase exception)
        {
            status.Error = exception.Message;
        }

        status.SecretConfigured = _secrets.TryRead(worker.Alias, out _)
            || _options.ConfiguredWorkerAliases.Contains(worker.Alias, StringComparer.OrdinalIgnoreCase);
        if (!FineTuningLaunchProfile.TryFromJson(worker.LaunchProfileJson, out var profile))
        {
            status.Error = "The stored launch profile is invalid.";
            return status;
        }

        status.BindHost = profile.BindHost;
        status.Port = profile.Port;
        status.ModelsRoot = profile.ModelsRoot;
        status.DataRoot = profile.DataRoot;
        status.PythonPath = profile.PythonPath;
        status.WorkingDirectory = profile.WorkerProjectPath;
        try
        {
            status.CommandPreview = FineTuningWorkerLaunch.Preview(profile, status.SecretPath);
        }
        catch (NcfExceptionBase exception)
        {
            status.Error = exception.Message;
        }

        if (_sessions.TryGetValue(worker.Alias, out var session))
            session.CopyTo(status);
        return status;
    }

    public void Dispose()
    {
        _shutdown.Dispose();
        foreach (var session in _sessions.Values)
            session.DisposeProcess();
    }

    private async Task AutoStartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!_options.Enabled)
            {
                Debug.WriteLine("[AIKernel] Skip managed fine-tuning auto-start because local fine-tuning is disabled.");
                return;
            }

            await _tenantScopeFactory.ForEachEnabledTenantAsync(async (services, token) =>
            {
                var workers = await services.GetRequiredService<AIFineTuningWorkerService>().GetAllAsync(token)
                    .ConfigureAwait(false);
                foreach (var worker in workers)
                {
                    if (!worker.Enabled || !FineTuningLaunchProfile.TryFromJson(worker.LaunchProfileJson, out var profile)
                        || !profile.Managed || !profile.AutoStart)
                        continue;
                    try
                    {
                        await StartAsync(worker, workers, token).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "Auto-start failed for managed fine-tuning worker {Alias}.", worker.Alias);
                        Debug.WriteLine($"[AIKernel] Auto-start failed for fine-tuning worker '{worker.Alias}': {exception.Message}");
                    }
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Managed fine-tuning worker auto-start failed.");
        }
    }

    private sealed class Session : IDisposable
    {
        private readonly object _gate = new();
        private readonly List<string> _logs = [];
        private readonly string _apiKey;
        private Process? _process;

        public Session(string alias, string apiKey, FineTuningWorkerLaunch launch)
        {
            Alias = alias;
            _apiKey = apiKey;
            Launch = launch;
            StartedUtc = DateTimeOffset.UtcNow;
        }

        public string Alias { get; }
        public FineTuningWorkerLaunch Launch { get; }
        public DateTimeOffset StartedUtc { get; }
        public string? Error { get; set; }
        public int? ProcessId => _process?.Id;
        public bool IsRunning => _process is { HasExited: false };
        public IEnumerable<string> Logs
        {
            get { lock (_gate) return _logs.ToArray(); }
        }

        public void Start()
        {
            var info = new ProcessStartInfo
            {
                FileName = Launch.PythonPath,
                WorkingDirectory = Launch.WorkingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in Launch.Arguments)
                info.ArgumentList.Add(argument);
            foreach (var pair in Launch.Variables)
                info.Environment[pair.Key] = pair.Value;

            var process = new Process { StartInfo = info, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, eventArgs) => Append("stdout", eventArgs.Data);
            process.ErrorDataReceived += (_, eventArgs) => Append("stderr", eventArgs.Data);
            if (!process.Start())
                throw new NcfExceptionBase("The fine-tuning worker process did not start.");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            _process = process;
            Append("supervisor", $"process started pid={process.Id}");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var process = _process;
            if (process == null || process.HasExited)
                return;
            try
            {
                Append("supervisor", "process stopping");
                process.Kill(entireProcessTree: true);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is InvalidOperationException or OperationCanceledException or System.ComponentModel.Win32Exception)
            {
                Error = exception.Message;
                Debug.WriteLine($"[AIKernel] Stopping fine-tuning worker '{Alias}' reported: {exception.Message}");
            }
        }

        public void CopyTo(FineTuningManagedWorkerStatus status)
        {
            var process = _process;
            status.ProcessId = process?.Id;
            status.StartedUtc = StartedUtc;
            status.PythonPath = Launch.PythonPath;
            status.WorkingDirectory = Launch.WorkingDirectory;
            status.Error = Error;
            lock (_gate)
                status.RecentLogs = _logs.ToArray();
            if (process == null)
            {
                status.State = "Faulted";
                return;
            }

            if (!process.HasExited)
            {
                status.State = "Running";
                return;
            }

            status.State = "Exited";
            try
            {
                status.ExitCode = process.ExitCode;
            }
            catch (InvalidOperationException)
            {
            }
        }

        public void DisposeProcess()
        {
            try
            {
                if (_process is { HasExited: false })
                    _process.Kill(entireProcessTree: true);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"[AIKernel] Disposing fine-tuning worker '{Alias}' reported: {exception.Message}");
            }
            finally
            {
                _process?.Dispose();
            }
        }

        public void Dispose() => DisposeProcess();

        private void Append(string channel, string? line)
        {
            if (string.IsNullOrEmpty(line))
                return;
            if (!string.IsNullOrEmpty(_apiKey))
                line = line.Replace(_apiKey, "[redacted]", StringComparison.Ordinal);
            lock (_gate)
            {
                _logs.Add(channel + ": " + line);
                if (_logs.Count > LogLimit)
                    _logs.RemoveRange(0, _logs.Count - LogLimit);
            }
        }
    }
}
