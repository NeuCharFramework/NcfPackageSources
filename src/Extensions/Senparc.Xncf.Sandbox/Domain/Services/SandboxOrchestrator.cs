/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxOrchestrator.cs
    文件功能描述：沙箱会话编排、配额与 TTL 回收

    创建标识：Senparc - 20260808

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Models.DatabaseModel;
using Senparc.Xncf.Sandbox.Domain.Services.Runtime;

namespace Senparc.Xncf.Sandbox.Domain.Services;

public sealed class SandboxOrchestrator : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEnumerable<ISandboxRuntime> _runtimes;
    private readonly ILogger<SandboxOrchestrator> _logger;
    private readonly SandboxQuotaPolicy _quota;
    private readonly string _workspaceRoot;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Timer? _ttlTimer;

    public SandboxOrchestrator(
        IServiceScopeFactory scopeFactory,
        IEnumerable<ISandboxRuntime> runtimes,
        ILogger<SandboxOrchestrator> logger,
        SandboxQuotaPolicy? quota = null)
    {
        _scopeFactory = scopeFactory;
        _runtimes = runtimes;
        _logger = logger;
        _quota = quota ?? new SandboxQuotaPolicy();
        _workspaceRoot = Path.Combine(Path.GetTempPath(), "Senparc.Ncf", "Sandbox");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_workspaceRoot);
        await ReconcileOrphansAsync(cancellationToken).ConfigureAwait(false);
        _ttlTimer = new Timer(
            async _ =>
            {
                try
                {
                    await SweepExpiredAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Sandbox TTL sweep failed.");
                }
            },
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _ttlTimer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _ttlTimer?.Dispose();
        _gate.Dispose();
    }

    public IReadOnlyCollection<SandboxTemplateDefinition> ListTemplates() => SandboxTemplateCatalog.All;

    public async Task<SandboxSessionInfo> CreateAsync(
        int ownerUserId,
        string templateKey,
        SandboxRuntimeKind? preferredRuntime = null,
        CancellationToken cancellationToken = default)
    {
        if (!SandboxTemplateCatalog.TryGet(templateKey, out var template))
        {
            throw new InvalidOperationException($"未知模板：{templateKey}");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();

            var userCount = await sessionService.CountActiveByUserAsync(ownerUserId).ConfigureAwait(false);
            if (userCount >= _quota.MaxSessionsPerUser)
            {
                throw new InvalidOperationException($"已达到每用户并发上限（{_quota.MaxSessionsPerUser}）。请先销毁空闲沙箱。");
            }

            var globalCount = await sessionService.CountActiveGlobalAsync().ConfigureAwait(false);
            if (globalCount >= _quota.MaxGlobalSessions)
            {
                throw new InvalidOperationException($"已达到全局并发上限（{_quota.MaxGlobalSessions}）。");
            }

            var runtimeKind = preferredRuntime ?? template.PreferredRuntime;
            var runtime = await ResolveRuntimeAsync(runtimeKind, cancellationToken).ConfigureAwait(false);

            var sessionId = Guid.NewGuid().ToString("N");
            var ttl = template.DefaultTtl > _quota.MaxTtl ? _quota.MaxTtl : template.DefaultTtl;
            var cpu = Math.Min(template.DefaultCpuLimit, 2d);
            var memory = Math.Min(template.DefaultMemoryMb, 2048);
            var expires = DateTime.UtcNow.Add(ttl);

            var entity = new SandboxSession(sessionId, ownerUserId, template.Key, runtime.Kind, cpu, memory, expires);
            await sessionService.SaveObjectAsync(entity).ConfigureAwait(false);

            try
            {
                if (!template.Interactive)
                {
                    // Exec-only templates still create a tracked placeholder session for quota/demo;
                    // real code runs via ExecAsync without keeping a long-lived container.
                    entity.MarkRunning(
                        runtimeHandle: $"exec-placeholder:{sessionId}",
                        hostPort: null,
                        accessUrl: null,
                        accessToken: null,
                        message: "Exec 模板已登记。请调用 Exec 运行代码；空闲将按 TTL 回收登记。");
                    await sessionService.SaveObjectAsync(entity).ConfigureAwait(false);
                    return entity.ToInfo();
                }

                var workspace = Path.Combine(_workspaceRoot, sessionId);
                var created = await runtime.CreateInteractiveAsync(
                        new SandboxCreateRuntimeRequest
                        {
                            SessionId = sessionId,
                            Template = template,
                            CpuLimit = cpu,
                            MemoryMb = memory,
                            WorkspaceDirectory = workspace
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                // 对外只暴露代理路径；HostPort/Token 仅服务端代理使用。
                var accessUrl = SandboxJupyterPaths.GetLabEntryUrl(sessionId);
                entity.MarkRunning(created.RuntimeHandle, created.HostPort, accessUrl, created.AccessToken, created.Message);
                await sessionService.SaveObjectAsync(entity).ConfigureAwait(false);
                _logger.LogInformation(
                    "Sandbox interactive session ready: SessionId={SessionId} AccessUrl={AccessUrl} HostPort={HostPort}",
                    sessionId,
                    accessUrl,
                    created.HostPort);
                return entity.ToInfo();
            }
            catch (Exception ex)
            {
                entity.MarkFailed(ex.Message);
                await sessionService.SaveObjectAsync(entity).ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SandboxExecResult> ExecAsync(
        string sessionId,
        string code,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        var entity = await sessionService.GetBySessionIdAsync(sessionId).ConfigureAwait(false)
                     ?? throw new InvalidOperationException($"会话不存在：{sessionId}");

        if (!SandboxTemplateCatalog.TryGet(entity.TemplateKey, out var template) || template.Interactive)
        {
            throw new InvalidOperationException("当前会话模板不支持 Exec。");
        }

        var normalizedCode = SandboxExecCodeDefaults.Normalize(entity.TemplateKey, code);
        if (!string.Equals(normalizedCode, code, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Sandbox Exec code normalized for template {Template}: using language default sample (was Python-style placeholder).",
                entity.TemplateKey);
        }

        var runtime = await ResolveRuntimeAsync(entity.RuntimeKind, cancellationToken).ConfigureAwait(false);
        var result = await runtime.ExecAsync(
                new SandboxExecRequest
                {
                    SessionId = sessionId,
                    Template = template,
                    Code = normalizedCode,
                    CpuLimit = entity.CpuLimit,
                    MemoryMb = entity.MemoryMb
                },
                cancellationToken)
            .ConfigureAwait(false);

        entity.Touch();
        await sessionService.SaveObjectAsync(entity).ConfigureAwait(false);
        return result;
    }

    public async Task<IReadOnlyList<SandboxSessionInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        var list = await sessionService.GetObjectListAsync(
                0,
                100,
                _ => true,
                z => z.AddTime,
                Senparc.Ncf.Core.Enums.OrderingType.Descending)
            .ConfigureAwait(false);
        return list.Select(z => z.ToInfo()).ToArray();
    }

    public async Task<SandboxSessionInfo?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        var entity = await sessionService.GetBySessionIdAsync(sessionId).ConfigureAwait(false);
        return entity?.ToInfo();
    }

    /// <summary>
    /// 供反向代理解析上游；勿下发给浏览器。
    /// </summary>
    public async Task<SandboxJupyterProxyTarget?> TryGetJupyterProxyTargetAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        using var scope = _scopeFactory.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        var normalizedId = sessionId.Trim().ToLowerInvariant();
        var entity = await sessionService.GetBySessionIdAsync(normalizedId).ConfigureAwait(false);
        if (entity == null
            || entity.Status != SandboxSessionStatus.Running
            || !entity.HostPort.HasValue
            || entity.HostPort.Value <= 0
            || string.IsNullOrWhiteSpace(entity.AccessToken))
        {
            return null;
        }

        if (!SandboxTemplateCatalog.TryGet(entity.TemplateKey, out var template) || !template.Interactive)
        {
            return null;
        }

        // 代理高频请求不 Touch/写库，避免 Lab 静态资源打爆 DB；TTL 仍按创建时 ExpiresAtUtc。
        return new SandboxJupyterProxyTarget
        {
            SessionId = entity.SessionId,
            HostPort = entity.HostPort.Value,
            AccessToken = entity.AccessToken
        };
    }

    public async Task DestroyAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
            var entity = await sessionService.GetBySessionIdAsync(sessionId).ConfigureAwait(false);
            if (entity == null)
            {
                return;
            }

            entity.MarkStopping();
            await sessionService.SaveObjectAsync(entity).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(entity.RuntimeHandle)
                && !entity.RuntimeHandle.StartsWith("exec-placeholder:", StringComparison.OrdinalIgnoreCase))
            {
                var runtime = _runtimes.FirstOrDefault(z => z.Kind == entity.RuntimeKind);
                if (runtime != null)
                {
                    await runtime.DestroyAsync(entity.RuntimeHandle, cancellationToken).ConfigureAwait(false);
                }
            }

            TryDeleteWorkspace(sessionId);
            entity.MarkStopped("Destroyed by user/admin.");
            await sessionService.SaveObjectAsync(entity).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<ISandboxRuntime> ResolveRuntimeAsync(SandboxRuntimeKind kind, CancellationToken cancellationToken)
    {
        var runtime = _runtimes.FirstOrDefault(z => z.Kind == kind)
                      ?? throw new InvalidOperationException($"未注册运行时：{kind}");

        if (!await runtime.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            if (kind == SandboxRuntimeKind.Docker)
            {
                throw new InvalidOperationException(
                    "Docker 运行时不可用。请安装并启动 Docker（或 Podman 兼容 docker CLI），且不要降级为裸进程执行不可信代码。");
            }

            throw new InvalidOperationException($"运行时 {kind} 当前不可用。Wasm Provider 仍为 Stub，请改用 Docker。");
        }

        return runtime;
    }

    private async Task SweepExpiredAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        List<SandboxSession> expired;
        try
        {
            expired = await sessionService.GetExpiredRunningAsync(DateTime.UtcNow).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Sandbox persistence not ready for TTL sweep.");
            return;
        }

        foreach (var entity in expired)
        {
            try
            {
                await DestroyExpiredCoreAsync(sessionService, entity, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to expire sandbox session {SessionId}", entity.SessionId);
            }
        }
    }

    private async Task DestroyExpiredCoreAsync(
        SandboxSessionService sessionService,
        SandboxSession entity,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(entity.RuntimeHandle)
            && !entity.RuntimeHandle.StartsWith("exec-placeholder:", StringComparison.OrdinalIgnoreCase))
        {
            var runtime = _runtimes.FirstOrDefault(z => z.Kind == entity.RuntimeKind);
            if (runtime != null)
            {
                await runtime.DestroyAsync(entity.RuntimeHandle, cancellationToken).ConfigureAwait(false);
            }
        }

        TryDeleteWorkspace(entity.SessionId);
        entity.MarkExpired();
        await sessionService.SaveObjectAsync(entity).ConfigureAwait(false);
        _logger.LogInformation("Expired sandbox session {SessionId}", entity.SessionId);
    }

    private async Task ReconcileOrphansAsync(CancellationToken cancellationToken)
    {
        foreach (var runtime in _runtimes)
        {
            if (!await runtime.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
                var active = await sessionService.GetActiveSessionsAsync().ConfigureAwait(false);
                var known = new HashSet<string>(
                    active.Select(z => z.RuntimeHandle).Where(z => !string.IsNullOrWhiteSpace(z))!,
                    StringComparer.OrdinalIgnoreCase);

                var handles = await runtime.ListOrphanHandlesAsync(cancellationToken).ConfigureAwait(false);
                foreach (var handle in handles)
                {
                    if (known.Contains(handle))
                    {
                        continue;
                    }

                    _logger.LogWarning("Removing orphan sandbox runtime handle {Handle}", handle);
                    await runtime.DestroyAsync(handle, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Orphan reconcile skipped for {Kind}", runtime.Kind);
            }
        }
    }

    private void TryDeleteWorkspace(string sessionId)
    {
        try
        {
            var dir = Path.Combine(_workspaceRoot, sessionId);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Workspace cleanup failed for {SessionId}", sessionId);
        }
    }
}
