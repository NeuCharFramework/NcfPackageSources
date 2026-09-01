/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxOrchestrator.cs
    文件功能描述：沙箱会话编排、配额与 TTL 回收

    创建标识：Senparc - 20260808

    修改标识：Senparc - 20260815
    修改描述：v0.2.0 增加 NCF 预览沙箱工作负载

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 增强 jupyter-csharp 模板与沙箱会话管理

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 支持创建与更新会话 TTL/永久保持策略

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 增强沙箱预览、Jupyter 工作区与会话生命周期管理

    修改标识：Senparc - 20260829
    修改描述：v0.3.0 强化沙箱工作区边界校验与会话路径隔离

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
        Func<string, CancellationToken, Task>? initializeWorkspace = null,
        SandboxNcfPreviewRuntimeOptions? ncfPreview = null,
        int? ttlMinutes = null,
        bool keepAlive = false,
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

            if (string.Equals(template.Key, SandboxTemplateKeys.NcfPreview, StringComparison.OrdinalIgnoreCase)
                && ncfPreview == null)
            {
                throw new InvalidOperationException("NCF 预览必须通过受控工作负载服务创建。");
            }

            var sessionId = Guid.NewGuid().ToString("N");
            var cpu = Math.Min(template.DefaultCpuLimit, 2d);
            var memory = Math.Min(template.DefaultMemoryMb, 2048);
            var expires = SandboxTtlPolicy.ResolveExpiresAtUtc(
                DateTime.UtcNow,
                template.DefaultTtl,
                _quota.MaxTtl,
                ttlMinutes,
                keepAlive);
            var ttlMessage = FormatTtlMessage(expires);

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
                        message: $"Exec 模板已登记。请调用 Exec 运行代码；{ttlMessage}");
                    await sessionService.SaveObjectAsync(entity).ConfigureAwait(false);
                    return entity.ToInfo();
                }

                var workspace = Path.Combine(_workspaceRoot, sessionId);
                if (initializeWorkspace != null)
                {
                    await initializeWorkspace(workspace, cancellationToken).ConfigureAwait(false);
                }
                var created = await runtime.CreateInteractiveAsync(
                        new SandboxCreateRuntimeRequest
                        {
                            SessionId = sessionId,
                            Template = template,
                            CpuLimit = cpu,
                            MemoryMb = memory,
                            WorkspaceDirectory = workspace,
                            NcfPreview = ncfPreview
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                // 列表链接直接使用 Docker 为该容器分配的本机端口；NCF 预览仍通过站点代理访问。
                var accessUrl = string.Equals(template.Key, SandboxTemplateKeys.NcfPreview, StringComparison.OrdinalIgnoreCase)
                    ? SandboxNcfPreviewPaths.GetEntryUrl(sessionId)
                    : SandboxJupyterPaths.GetDirectLabEntryUrl(
                        sessionId,
                        created.HostPort ?? throw new InvalidOperationException("Jupyter 容器未返回本机端口。"),
                        created.AccessToken ?? throw new InvalidOperationException("Jupyter 容器未返回访问令牌。"));
                entity.MarkRunning(
                    created.RuntimeHandle,
                    created.HostPort,
                    accessUrl,
                    created.AccessToken,
                    $"{created.Message} {ttlMessage}".Trim());
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

    public async Task<SandboxSessionInfo> UpdateTtlAsync(
        string sessionId,
        int? ttlMinutes,
        bool keepAlive,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new InvalidOperationException("SessionId 不能为空。");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
            var entity = await sessionService.GetBySessionIdAsync(sessionId.Trim()).ConfigureAwait(false)
                         ?? throw new InvalidOperationException("会话不存在。");

            if (entity.Status is not (SandboxSessionStatus.Creating or SandboxSessionStatus.Running))
            {
                throw new InvalidOperationException("只有创建中或运行中的会话可以修改 TTL。");
            }

            if (!SandboxTemplateCatalog.TryGet(entity.TemplateKey, out var template))
            {
                throw new InvalidOperationException($"会话模板不存在：{entity.TemplateKey}");
            }

            var expiresAtUtc = SandboxTtlPolicy.ResolveExpiresAtUtc(
                DateTime.UtcNow,
                template.DefaultTtl,
                _quota.MaxTtl,
                ttlMinutes,
                keepAlive);
            entity.SetExpiresAtUtc(expiresAtUtc);
            await sessionService.SaveObjectAsync(entity).ConfigureAwait(false);

            _logger.LogInformation(
                "Sandbox session TTL updated: SessionId={SessionId} Unlimited={Unlimited} ExpiresAtUtc={ExpiresAtUtc}",
                entity.SessionId,
                keepAlive,
                expiresAtUtc);
            return ToInfo(entity);
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

    public async Task<SandboxExecResult> ExecInteractiveAsync(
        string sessionId,
        string command,
        string? workingDirectory = null,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        var context = await GetInteractiveControlContextAsync(sessionService, sessionId, cancellationToken)
            .ConfigureAwait(false);

        var normalizedCommand = (command ?? string.Empty).Trim();
        if (normalizedCommand.Length == 0)
        {
            throw new InvalidOperationException("Lab 命令不能为空。");
        }

        if (normalizedCommand.Length > _quota.MaxInteractiveCommandCharacters)
        {
            throw new InvalidOperationException(
                $"Lab 命令不能超过 {_quota.MaxInteractiveCommandCharacters} 个字符。");
        }

        var timeout = timeoutSeconds <= 0 ? 30 : timeoutSeconds;
        if (timeout > _quota.MaxInteractiveCommandSeconds)
        {
            throw new InvalidOperationException(
                $"Lab 命令超时时间不能超过 {_quota.MaxInteractiveCommandSeconds} 秒。");
        }

        var relativeDirectory = SandboxWorkspacePaths.NormalizeRelativePath(
            workingDirectory,
            allowEmpty: true);
        var containerWorkingDirectory = SandboxWorkspacePaths.CombineContainerPath(
            context.Template.WorkspaceMountPath,
            relativeDirectory);

        var result = await context.Runtime.ExecInteractiveAsync(
                new SandboxInteractiveExecRequest
                {
                    SessionId = context.Entity.SessionId,
                    RuntimeHandle = context.Entity.RuntimeHandle!,
                    Command = normalizedCommand,
                    WorkingDirectory = containerWorkingDirectory,
                    Timeout = TimeSpan.FromSeconds(timeout),
                    MaxOutputCharacters = _quota.MaxInteractiveOutputCharacters
                },
                cancellationToken)
            .ConfigureAwait(false);

        context.Entity.Touch();
        await sessionService.SaveObjectAsync(context.Entity).ConfigureAwait(false);
        return result;
    }

    public async Task<SandboxWorkspaceFileInfo> UploadWorkspaceFileAsync(
        string sessionId,
        string relativePath,
        byte[] content,
        bool overwrite = true,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        var context = await GetInteractiveControlContextAsync(sessionService, sessionId, cancellationToken)
            .ConfigureAwait(false);

        content ??= Array.Empty<byte>();
        if (content.LongLength > _quota.MaxWorkspaceFileBytes)
        {
            throw new InvalidOperationException(
                $"上传文件不能超过 {_quota.MaxWorkspaceFileBytes} 字节。");
        }

        var normalizedPath = SandboxWorkspacePaths.NormalizeRelativePath(relativePath);
        var workspace = SandboxWorkspacePaths.GetSessionWorkspacePath(_workspaceRoot, context.Entity.SessionId);
        var target = SandboxWorkspacePaths.CombineHostPath(workspace, normalizedPath);
        EnsureWorkspacePathSafe(workspace, target);
        if (File.Exists(target) && !overwrite)
        {
            throw new InvalidOperationException($"工作区文件已存在：{normalizedPath}");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await File.WriteAllBytesAsync(target, content, cancellationToken).ConfigureAwait(false);
        var file = ToWorkspaceFileInfo(workspace, target);
        context.Entity.Touch();
        await sessionService.SaveObjectAsync(context.Entity).ConfigureAwait(false);
        return file;
    }

    public async Task<SandboxWorkspaceFileContent> ReadWorkspaceFileAsync(
        string sessionId,
        string relativePath,
        long maxBytes = 0,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        var context = await GetInteractiveControlContextAsync(sessionService, sessionId, cancellationToken)
            .ConfigureAwait(false);

        var normalizedPath = SandboxWorkspacePaths.NormalizeRelativePath(relativePath);
        var workspace = SandboxWorkspacePaths.GetSessionWorkspacePath(_workspaceRoot, context.Entity.SessionId);
        var target = SandboxWorkspacePaths.CombineHostPath(workspace, normalizedPath);
        EnsureWorkspacePathSafe(workspace, target);
        if (!File.Exists(target))
        {
            throw new FileNotFoundException("工作区文件不存在。", normalizedPath);
        }

        var info = new FileInfo(target);
        if (info.Length > _quota.MaxWorkspaceReadBytes)
        {
            throw new InvalidOperationException(
                $"读取文件不能超过 {_quota.MaxWorkspaceReadBytes} 字节。");
        }

        var requestedMaxBytes = maxBytes <= 0
            ? _quota.MaxWorkspaceReadBytes
            : Math.Min(maxBytes, _quota.MaxWorkspaceReadBytes);
        if (info.Length > requestedMaxBytes)
        {
            throw new InvalidOperationException(
                $"文件大小超过本次读取上限 {requestedMaxBytes} 字节。");
        }

        var content = await File.ReadAllBytesAsync(target, cancellationToken).ConfigureAwait(false);
        context.Entity.Touch();
        await sessionService.SaveObjectAsync(context.Entity).ConfigureAwait(false);
        return new SandboxWorkspaceFileContent
        {
            File = ToWorkspaceFileInfo(workspace, target),
            Content = content
        };
    }

    public async Task<IReadOnlyList<SandboxWorkspaceFileInfo>> ListWorkspaceFilesAsync(
        string sessionId,
        string? relativeDirectory = null,
        bool recursive = false,
        int maxItems = 0,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        var context = await GetInteractiveControlContextAsync(sessionService, sessionId, cancellationToken)
            .ConfigureAwait(false);

        var normalizedDirectory = SandboxWorkspacePaths.NormalizeRelativePath(
            relativeDirectory,
            allowEmpty: true);
        var workspace = SandboxWorkspacePaths.GetSessionWorkspacePath(_workspaceRoot, context.Entity.SessionId);
        var directory = normalizedDirectory.Length == 0
            ? workspace
            : SandboxWorkspacePaths.CombineHostPath(workspace, normalizedDirectory);
        EnsureWorkspacePathSafe(workspace, directory);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"工作区目录不存在：{normalizedDirectory}");
        }

        var limit = maxItems <= 0
            ? _quota.MaxWorkspaceListItems
            : Math.Min(maxItems, _quota.MaxWorkspaceListItems);
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = recursive,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.ReparsePoint
        };
        var files = Directory.EnumerateFiles(directory, "*", options)
            .Take(limit)
            .Select(path => ToWorkspaceFileInfo(workspace, path))
            .ToArray();

        context.Entity.Touch();
        await sessionService.SaveObjectAsync(context.Entity).ConfigureAwait(false);
        return files;
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
        return list.Select(ToInfo).ToArray();
    }

    public async Task<SandboxSessionInfo?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        var entity = await sessionService.GetBySessionIdAsync(sessionId).ConfigureAwait(false);
        return entity == null ? null : ToInfo(entity);
    }

    private static SandboxSessionInfo ToInfo(SandboxSession entity)
    {
        var info = entity.ToInfo();
        if (info.Status == SandboxSessionStatus.Running
            && (string.Equals(entity.TemplateKey, SandboxTemplateKeys.JupyterPython, StringComparison.OrdinalIgnoreCase)
            || string.Equals(entity.TemplateKey, SandboxTemplateKeys.JupyterCsharp, StringComparison.OrdinalIgnoreCase))
            )
        {
            if (entity.HostPort is > 0 and <= 65535 && !string.IsNullOrWhiteSpace(entity.AccessToken))
            {
                return new SandboxSessionInfo
                {
                    SessionId = info.SessionId,
                    OwnerUserId = info.OwnerUserId,
                    TemplateKey = info.TemplateKey,
                    RuntimeKind = info.RuntimeKind,
                    Status = info.Status,
                    AccessUrl = SandboxJupyterPaths.GetDirectLabEntryUrl(entity.SessionId, entity.HostPort.Value, entity.AccessToken),
                    StatusMessage = info.StatusMessage,
                    HostPort = info.HostPort,
                    CreatedAtUtc = info.CreatedAtUtc,
                    ExpiresAtUtc = info.ExpiresAtUtc,
                    IsTtlUnlimited = info.IsTtlUnlimited,
                    LastActivityAtUtc = info.LastActivityAtUtc
                };
            }
        }

        return info;
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

    /// <summary>
    /// Resolves the loopback target for an NCF preview. Unlike Jupyter, no bearer token is needed
    /// upstream; the proxy strips all caller credentials and requires the host Admin cookie.
    /// </summary>
    public async Task<SandboxNcfPreviewProxyTarget?> TryGetNcfPreviewProxyTargetAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        using var scope = _scopeFactory.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        var entity = await sessionService.GetBySessionIdAsync(sessionId.Trim().ToLowerInvariant()).ConfigureAwait(false);
        if (entity == null
            || entity.Status != SandboxSessionStatus.Running
            || !entity.HostPort.HasValue
            || entity.HostPort.Value <= 0
            || !string.Equals(entity.TemplateKey, SandboxTemplateKeys.NcfPreview, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new SandboxNcfPreviewProxyTarget
        {
            SessionId = entity.SessionId,
            HostPort = entity.HostPort.Value
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

    public async Task DeleteRecordAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new InvalidOperationException("SessionId 不能为空。");
        }

        var normalizedSessionId = sessionId.Trim();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
            var entity = await sessionService.GetBySessionIdAsync(normalizedSessionId).ConfigureAwait(false);
            if (entity == null)
            {
                return;
            }

            if (!entity.CanDeleteRecord())
            {
                throw new InvalidOperationException("只有已停止、已过期或已清理完成的失败会话才能删除记录，请先销毁运行环境。");
            }

            TryDeleteWorkspace(normalizedSessionId);
            await sessionService.DeletePermanentlyAsync(entity).ConfigureAwait(false);
            _logger.LogInformation("Deleted sandbox session record {SessionId}", normalizedSessionId);
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

    private async Task<InteractiveControlContext> GetInteractiveControlContextAsync(
        SandboxSessionService sessionService,
        string sessionId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new InvalidOperationException("SessionId 不能为空。");
        }

        var entity = await sessionService.GetBySessionIdAsync(sessionId.Trim()).ConfigureAwait(false)
                     ?? throw new InvalidOperationException($"会话不存在：{sessionId}");
        if (entity.Status != SandboxSessionStatus.Running
            || string.IsNullOrWhiteSpace(entity.RuntimeHandle)
            || entity.RuntimeHandle.StartsWith("exec-placeholder:", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("当前会话不是可控制的运行中 Lab。");
        }

        if (!SandboxTemplateCatalog.TryGet(entity.TemplateKey, out var template)
            || !template.Interactive
            || !template.SupportsInteractiveControl)
        {
            throw new InvalidOperationException(
                $"模板 {entity.TemplateKey} 不支持持久化 Lab 控制。");
        }

        var runtime = await ResolveRuntimeAsync(entity.RuntimeKind, cancellationToken).ConfigureAwait(false);
        return new InteractiveControlContext(entity, template, runtime);
    }

    private static void EnsureWorkspacePathSafe(string workspace, string target)
    {
        var normalizedWorkspace = Path.GetFullPath(workspace)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var normalizedTarget = Path.GetFullPath(target);
        if (!SandboxWorkspacePaths.IsWithinWorkspace(workspace, normalizedTarget))
        {
            throw new InvalidOperationException("工作区路径越界。");
        }

        var current = normalizedWorkspace.TrimEnd(Path.DirectorySeparatorChar);
        var relative = Path.GetRelativePath(current, normalizedTarget);
        foreach (var segment in relative.Split(
                     new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (Directory.Exists(current)
                && new DirectoryInfo(current).Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException("工作区不允许穿过符号链接或重解析点。");
            }

            if (File.Exists(current)
                && new FileInfo(current).Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException("工作区不允许操作符号链接或重解析点。");
            }
        }
    }

    private static SandboxWorkspaceFileInfo ToWorkspaceFileInfo(string workspace, string path)
    {
        var info = new FileInfo(path);
        return new SandboxWorkspaceFileInfo
        {
            RelativePath = Path.GetRelativePath(workspace, path).Replace('\\', '/'),
            Length = info.Length,
            LastWriteTimeUtc = info.LastWriteTimeUtc
        };
    }

    private sealed record InteractiveControlContext(
        SandboxSession Entity,
        SandboxTemplateDefinition Template,
        ISandboxRuntime Runtime);

    private async Task SweepExpiredAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
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
        finally
        {
            _gate.Release();
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
                var handles = await runtime.ListOrphanHandlesAsync(cancellationToken).ConfigureAwait(false);
                var allHandles = new HashSet<string>(
                    handles,
                    StringComparer.OrdinalIgnoreCase);
                var runningHandles = new HashSet<string>(
                    await runtime.ListRunningHandlesAsync(cancellationToken).ConfigureAwait(false),
                    StringComparer.OrdinalIgnoreCase);
                var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var entity in active.Where(z => z.RuntimeKind == runtime.Kind))
                {
                    var handle = entity.RuntimeHandle;
                    if (string.IsNullOrWhiteSpace(handle)
                        || handle.StartsWith("exec-placeholder:", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (ContainsRuntimeHandle(runningHandles, handle))
                    {
                        known.Add(handle);
                        continue;
                    }

                    var reason = allHandles.Contains(handle)
                        ? "Sandbox 容器已停止，应用启动时已同步为已停止。"
                        : "Sandbox 容器已不存在，应用启动时已同步为已停止。";
                    entity.MarkStopped(reason);
                    TryDeleteWorkspace(entity.SessionId);
                    await sessionService.SaveObjectAsync(entity).ConfigureAwait(false);
                    _logger.LogWarning(
                        "Sandbox session {SessionId} runtime handle {Handle} is {State}; marked stopped.",
                        entity.SessionId,
                        handle,
                        allHandles.Contains(handle) ? "stopped" : "missing");
                }

                foreach (var handle in handles)
                {
                    if (ContainsRuntimeHandle(known, handle))
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

    private static bool ContainsRuntimeHandle(IEnumerable<string> handles, string handle)
    {
        var normalizedHandle = handle.Trim();
        if (normalizedHandle.Length == 0)
        {
            return false;
        }

        return handles.Any(candidate =>
        {
            var normalizedCandidate = candidate.Trim();
            return normalizedCandidate.Equals(normalizedHandle, StringComparison.OrdinalIgnoreCase)
                   || normalizedCandidate.StartsWith(normalizedHandle, StringComparison.OrdinalIgnoreCase)
                   || normalizedHandle.StartsWith(normalizedCandidate, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string FormatTtlMessage(DateTime expiresAtUtc)
    {
        return SandboxTtlPolicy.IsUnlimited(expiresAtUtc)
            ? "TTL 已设为永久保持，须由管理员手动销毁。"
            : $"TTL 到期时间（UTC）：{expiresAtUtc:u}。";
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
