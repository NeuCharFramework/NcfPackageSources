/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FineTuningWorkerSupervisor.cs
    File description: Related implementation.

    Created by: Senparc - 20261106

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

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

    /// <summary>
    /// Initializes the managed fine-tuning worker supervisor.
    /// </summary>
    /// <param name="tenantScopeFactory">Factory used for tenant-scoped worker discovery.</param>
    /// <param name="secrets">Worker secret store.</param>
    /// <param name="runtime">Runtime enablement settings.</param>
    /// <param name="options">Fine-tuning worker options.</param>
    /// <param name="logger">Supervisor logger.</param>
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

    /// <summary>
    /// Applies runtime settings and starts background auto-start processing.
    /// </summary>
    /// <param name="cancellationToken">Hosted-service startup cancellation token.</param>
    /// <returns>A completed task after scheduling auto-start.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _runtime.ApplyStartup();
        _ = AutoStartAsync(_shutdown.Token);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cancels auto-start and stops all currently managed worker processes.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel shutdown waits.</param>
    /// <returns>A task that completes after stop requests are issued.</returns>
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

    /// <summary>
    /// Starts one managed worker after validating its launch profile, secret, loopback binding,
    /// peer ports, directories, and Python launch plan.
    /// </summary>
    /// <param name="worker">The persisted worker configuration.</param>
    /// <param name="peers">Other worker configurations used for collision checks.</param>
    /// <param name="cancellationToken">Token used to cancel startup.</param>
    /// <returns>The managed worker status after startup.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the worker is not managed or launch validation/startup fails.</exception>
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

    /// <summary>
    /// Stops a managed worker session when one is registered for the alias.
    /// </summary>
    /// <param name="alias">The worker alias.</param>
    /// <param name="cancellationToken">Token used to cancel the process wait.</param>
    /// <returns>A task that completes after the stop request.</returns>
    public async Task StopWorkerAsync(string alias, CancellationToken cancellationToken)
    {
        if (!_sessions.TryGetValue(alias, out var session))
            return;
        await session.StopAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Stopped managed fine-tuning worker {Alias} pid={Pid}.", alias, session.ProcessId);
        Debug.WriteLine($"[AIKernel] Stopped managed fine-tuning worker '{alias}' pid={session.ProcessId}.");
    }

    /// <summary>
    /// Builds a status snapshot from persisted worker configuration and any live managed session.
    /// </summary>
    /// <param name="worker">The persisted worker configuration.</param>
    /// <returns>The current managed-worker status.</returns>
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

    /// <summary>
    /// Disposes supervisor cancellation state and managed worker processes.
    /// </summary>
    public void Dispose()
    {
        _shutdown.Dispose();
        foreach (var session in _sessions.Values)
            session.DisposeProcess();
    }

    /// <summary>
    /// Discovers enabled auto-start workers across tenants and starts them best-effort.
    /// </summary>
    /// <param name="cancellationToken">Token used to stop discovery/startup.</param>
    /// <returns>A task that completes after discovery and attempted starts.</returns>
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

        /// <summary>
        /// Initializes an in-memory managed-worker process session.
        /// </summary>
        /// <param name="alias">The Worker alias.</param>
        /// <param name="apiKey">The secret used for redaction.</param>
        /// <param name="launch">The validated launch plan.</param>
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

        /// <summary>
        /// Starts the configured Python worker process and attaches bounded output readers.
        /// </summary>
        /// <exception cref="NcfExceptionBase">Thrown when the process cannot start.</exception>
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

        /// <summary>
        /// Stops the managed process tree and waits up to the bounded shutdown timeout.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the wait.</param>
        /// <returns>A task that completes after the stop attempt.</returns>
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

        /// <summary>
        /// Copies current process, launch, error, and bounded log state into a status DTO.
        /// </summary>
        /// <param name="status">The status DTO to populate.</param>
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

        /// <summary>
        /// Terminates and disposes the managed process when present.
        /// </summary>
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

        /// <summary>
        /// Disposes the managed process session.
        /// </summary>
        public void Dispose() => DisposeProcess();

        /// <summary>
        /// Appends one redacted process log line while retaining only the bounded tail.
        /// </summary>
        /// <param name="channel">The source channel label.</param>
        /// <param name="line">The output line.</param>
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
