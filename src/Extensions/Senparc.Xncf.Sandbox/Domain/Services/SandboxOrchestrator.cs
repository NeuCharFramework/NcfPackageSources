/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxOrchestrator.cs
    File description: Sandbox session orchestration, quotas, and TTL cleanup

    Created by: Senparc - 20260808

    Modified by: Senparc - 20260815
    Change description: v0.2.0 add the NCF preview sandbox workload

    Modified by: Senparc - 20260817
    Change description: v0.2.0 enhance the jupyter-csharp template and sandbox session management

    Modified by: Senparc - 20260817
    Change description: v0.2.0 support session TTL and keep-alive policies

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspaces, and session lifecycle management

    Modified by: Senparc - 20260829
    Change description: v0.3.0 strengthen sandbox workspace boundary checks and session path isolation

    Modified by: Senparc - 20260918
    Change description: v0.3.3 support session aliases, additional port mappings, and interactive standard input

    Modified by: Senparc - 20261005
    Change description: v0.3.4 0.3.4 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261028
    Change description: In multi-tenant mode, restore request tenant context in the singleton orchestrator and iterate background TTL/orphan cleanup by tenant

    Modified by: Senparc - 20261103
    Change description: v0.4.0 add bulk file upload functionality to Sandbox Lab

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.MultiTenant;
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
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IBackgroundTenantScopeFactory? _tenantScopeFactory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();
    private Timer? _ttlTimer;
    private Task? _startupTask;

    public SandboxOrchestrator(
        IServiceScopeFactory scopeFactory,
        IEnumerable<ISandboxRuntime> runtimes,
        ILogger<SandboxOrchestrator> logger,
        SandboxQuotaPolicy? quota = null,
        IHttpContextAccessor? httpContextAccessor = null,
        IBackgroundTenantScopeFactory? tenantScopeFactory = null)
    {
        _scopeFactory = scopeFactory;
        _runtimes = runtimes;
        _logger = logger;
        _quota = quota ?? new SandboxQuotaPolicy();
        _workspaceRoot = Path.Combine(Path.GetTempPath(), "Senparc.Ncf", "Sandbox");
        _httpContextAccessor = httpContextAccessor;
        _tenantScopeFactory = tenantScopeFactory;
    }

    /// <summary>
    /// Capture the current request tenant snapshot; return no match outside a request context to remain fail-closed.
    /// </summary>
    private RequestTenantSnapshot CaptureCurrentTenant()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        return httpContext == null
            ? RequestTenantSnapshot.Unmatched
            : RequestTenantSnapshot.Capture(httpContext.RequestServices);
    }

    /// <summary>
    /// Create a scope that restores the current request tenant context for request-triggered sandbox operations.
    /// </summary>
    private IServiceScope CreateTenantScope()
    {
        var scope = _scopeFactory.CreateScope();
        CaptureCurrentTenant().Restore(scope.ServiceProvider);
        return scope;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_workspaceRoot);
        _ttlTimer = new Timer(
            async _ =>
            {
                try
                {
                    await SweepExpiredAsync(_shutdown.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Sandbox TTL sweep failed.");
                }
            },
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));

        // Docker probing and orphan reconciliation are optional housekeeping. Do not make
        // the whole host wait for a missing/unresponsive Docker CLI during startup.
        _startupTask = ReconcileOrphansInBackgroundAsync(_shutdown.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _shutdown.Cancel();
        _ttlTimer?.Change(Timeout.Infinite, 0);
        if (_startupTask == null)
        {
            return;
        }

        try
        {
            await _startupTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested || _shutdown.IsCancellationRequested)
        {
        }
    }

    public void Dispose()
    {
        _ttlTimer?.Dispose();
        _shutdown.Dispose();
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
        string? alias = null,
        string? extraPortMappings = null,
        CancellationToken cancellationToken = default)
    {
        if (!SandboxTemplateCatalog.TryGet(templateKey, out var template))
        {
            throw new InvalidOperationException($"Unknown template: {templateKey}");
        }

        var normalizedAlias = (alias ?? string.Empty).Trim();
        if (normalizedAlias.Length > 128)
        {
            throw new InvalidOperationException("The alias must be at most 128 characters.");
        }

        var parsedExtraPorts = SandboxPortMappings.Parse(extraPortMappings, _quota.MaxExtraPortMappings);
        if (parsedExtraPorts.Count > 0
            && !string.Equals(template.Key, SandboxTemplateKeys.JupyterPython, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(template.Key, SandboxTemplateKeys.JupyterCsharp, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Additional port mappings are supported only for the JupyterLab template.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var scope = CreateTenantScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();

            var userCount = await sessionService.CountActiveByUserAsync(ownerUserId).ConfigureAwait(false);
            if (userCount >= _quota.MaxSessionsPerUser)
            {
                throw new InvalidOperationException($"The per-user session limit ({_quota.MaxSessionsPerUser}) has been reached. Destroy an idle sandbox first.");
            }

            var globalCount = await sessionService.CountActiveGlobalAsync().ConfigureAwait(false);
            if (globalCount >= _quota.MaxGlobalSessions)
            {
                throw new InvalidOperationException($"The global session limit ({_quota.MaxGlobalSessions}) has been reached.");
            }

            var runtimeKind = preferredRuntime ?? template.PreferredRuntime;
            var runtime = await ResolveRuntimeAsync(runtimeKind, cancellationToken).ConfigureAwait(false);

            if (string.Equals(template.Key, SandboxTemplateKeys.NcfPreview, StringComparison.OrdinalIgnoreCase)
                && ncfPreview == null)
            {
                throw new InvalidOperationException("NCF previews must be created through the controlled workload service.");
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
            if (normalizedAlias.Length > 0)
            {
                entity.SetAlias(normalizedAlias);
            }

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
                        message: $"The Exec template has been registered. Call Exec to run code; {ttlMessage}");
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
                            NcfPreview = ncfPreview,
                            ExtraPortMappings = parsedExtraPorts
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(created.ExtraPorts))
                {
                    entity.SetExtraPorts(created.ExtraPorts);
                }

                // List links use the host port assigned by Docker; NCF previews still use the site proxy.
                var accessUrl = string.Equals(template.Key, SandboxTemplateKeys.NcfPreview, StringComparison.OrdinalIgnoreCase)
                    ? SandboxNcfPreviewPaths.GetEntryUrl(sessionId)
                    : SandboxJupyterPaths.GetDirectLabEntryUrl(
                        sessionId,
                        created.HostPort ?? throw new InvalidOperationException("The Jupyter container did not return a host port."),
                        created.AccessToken ?? throw new InvalidOperationException("The Jupyter container did not return an access token."));
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
            throw new InvalidOperationException("SessionId is required.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var scope = CreateTenantScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
            var entity = await sessionService.GetBySessionIdAsync(sessionId.Trim()).ConfigureAwait(false)
                         ?? throw new InvalidOperationException("The session does not exist.");

            if (entity.Status is not (SandboxSessionStatus.Creating or SandboxSessionStatus.Running))
            {
                throw new InvalidOperationException("Only creating or running sessions can change TTL.");
            }

            if (!SandboxTemplateCatalog.TryGet(entity.TemplateKey, out var template))
            {
                throw new InvalidOperationException($"The session template does not exist: {entity.TemplateKey}");
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

    public async Task<SandboxSessionInfo> UpdateAliasAsync(
        string sessionId,
        string? alias,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new InvalidOperationException("SessionId is required.");
        }

        var normalizedAlias = (alias ?? string.Empty).Trim();
        if (normalizedAlias.Length > 128)
        {
            throw new InvalidOperationException("The alias must be at most 128 characters.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var scope = CreateTenantScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
            var entity = await sessionService.GetBySessionIdAsync(sessionId.Trim()).ConfigureAwait(false)
                         ?? throw new InvalidOperationException("The session does not exist.");

            entity.SetAlias(normalizedAlias.Length == 0 ? null : normalizedAlias);
            await sessionService.SaveObjectAsync(entity).ConfigureAwait(false);
            _logger.LogInformation("Sandbox session alias updated: SessionId={SessionId}", entity.SessionId);
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
        using var scope = CreateTenantScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        var entity = await sessionService.GetBySessionIdAsync(sessionId).ConfigureAwait(false)
                     ?? throw new InvalidOperationException($"The session does not exist: {sessionId}");

        if (!SandboxTemplateCatalog.TryGet(entity.TemplateKey, out var template) || template.Interactive)
        {
            throw new InvalidOperationException("The current session template does not support Exec.");
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
        string? stdinContent = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateTenantScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        var context = await GetInteractiveControlContextAsync(sessionService, sessionId, cancellationToken)
            .ConfigureAwait(false);

        var normalizedCommand = (command ?? string.Empty).Trim();
        if (normalizedCommand.Length == 0)
        {
            throw new InvalidOperationException("The Lab command is required.");
        }

        if (normalizedCommand.Length > _quota.MaxInteractiveCommandCharacters)
        {
            throw new InvalidOperationException(
                $"The Lab command must be at most {_quota.MaxInteractiveCommandCharacters} characters.");
        }

        var normalizedStdin = stdinContent ?? string.Empty;
        if (normalizedStdin.Length > _quota.MaxInteractiveStdinCharacters)
        {
            throw new InvalidOperationException(
                $"Standard input must be at most {_quota.MaxInteractiveStdinCharacters} characters.");
        }

        var timeout = timeoutSeconds <= 0 ? 30 : timeoutSeconds;
        if (timeout > _quota.MaxInteractiveCommandSeconds)
        {
            throw new InvalidOperationException(
                $"The Lab command timeout must be at most {_quota.MaxInteractiveCommandSeconds} seconds.");
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
                    MaxOutputCharacters = _quota.MaxInteractiveOutputCharacters,
                    StdinContent = normalizedStdin.Length == 0 ? null : normalizedStdin
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
        content ??= Array.Empty<byte>();
        await using var input = new MemoryStream(content, writable: false);
        return await UploadWorkspaceFileAsync(
                sessionId,
                relativePath,
                input,
                content.LongLength,
                overwrite,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<SandboxWorkspaceFileInfo> UploadWorkspaceFileAsync(
        string sessionId,
        string relativePath,
        Stream content,
        long? contentLength = null,
        bool overwrite = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        using var scope = CreateTenantScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        var context = await GetInteractiveControlContextAsync(sessionService, sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (contentLength is > 0 && contentLength > _quota.MaxWorkspaceFileBytes)
        {
            throw new InvalidOperationException(
                $"An uploaded file must be at most {_quota.MaxWorkspaceFileBytes} bytes.");
        }

        var normalizedPath = SandboxWorkspacePaths.NormalizeRelativePath(relativePath);
        var workspace = SandboxWorkspacePaths.GetSessionWorkspacePath(_workspaceRoot, context.Entity.SessionId);
        var target = SandboxWorkspacePaths.CombineHostPath(workspace, normalizedPath);
        EnsureWorkspacePathSafe(workspace, target);
        if (File.Exists(target) && !overwrite)
        {
            throw new InvalidOperationException($"The Workspace file already exists: {normalizedPath}");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temporaryTarget = target + ".ncf-upload-" + Guid.NewGuid().ToString("N");
        EnsureWorkspacePathSafe(workspace, temporaryTarget);
        var length = 0L;
        try
        {
            await using (var output = new FileStream(
                temporaryTarget,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[64 * 1024];
                int read;
                while ((read = await content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    length = checked(length + read);
                    if (length > _quota.MaxWorkspaceFileBytes)
                    {
                        throw new InvalidOperationException(
                            $"An uploaded file must be at most {_quota.MaxWorkspaceFileBytes} bytes.");
                    }

                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            if (contentLength.HasValue && contentLength.Value != length)
            {
                throw new InvalidOperationException("The uploaded file length does not match the declared length.");
            }

            File.Move(temporaryTarget, target, overwrite);
        }
        finally
        {
            if (File.Exists(temporaryTarget))
            {
                File.Delete(temporaryTarget);
            }
        }

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
        using var scope = CreateTenantScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        var context = await GetInteractiveControlContextAsync(sessionService, sessionId, cancellationToken)
            .ConfigureAwait(false);

        var normalizedPath = SandboxWorkspacePaths.NormalizeRelativePath(relativePath);
        var workspace = SandboxWorkspacePaths.GetSessionWorkspacePath(_workspaceRoot, context.Entity.SessionId);
        var target = SandboxWorkspacePaths.CombineHostPath(workspace, normalizedPath);
        EnsureWorkspacePathSafe(workspace, target);
        if (!File.Exists(target))
        {
            throw new FileNotFoundException("The Workspace file does not exist.", normalizedPath);
        }

        var info = new FileInfo(target);
        if (info.Length > _quota.MaxWorkspaceReadBytes)
        {
            throw new InvalidOperationException(
                $"A file read must be at most {_quota.MaxWorkspaceReadBytes} bytes.");
        }

        var requestedMaxBytes = maxBytes <= 0
            ? _quota.MaxWorkspaceReadBytes
            : Math.Min(maxBytes, _quota.MaxWorkspaceReadBytes);
        if (info.Length > requestedMaxBytes)
        {
            throw new InvalidOperationException(
                $"The file size exceeds this read limit of {requestedMaxBytes} bytes.");
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
        using var scope = CreateTenantScope();
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
            throw new DirectoryNotFoundException($"The Workspace directory does not exist: {normalizedDirectory}");
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
        using var scope = CreateTenantScope();
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
        using var scope = CreateTenantScope();
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
                    LastActivityAtUtc = info.LastActivityAtUtc,
                    Alias = info.Alias,
                    ExtraPorts = info.ExtraPorts
                };
            }
        }

        return info;
    }

    /// <summary>
    /// Used by the reverse proxy to resolve the upstream; never send it to the browser.
    /// </summary>
    public async Task<SandboxJupyterProxyTarget?> TryGetJupyterProxyTargetAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        using var scope = CreateTenantScope();
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

        // High-frequency proxy requests do not touch or write the database, preventing Lab static assets from overwhelming it; TTL still uses ExpiresAtUtc from creation.
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

        using var scope = CreateTenantScope();
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
            using var scope = CreateTenantScope();
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
            throw new InvalidOperationException("SessionId is required.");
        }

        var normalizedSessionId = sessionId.Trim();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var scope = CreateTenantScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
            var entity = await sessionService.GetBySessionIdAsync(normalizedSessionId).ConfigureAwait(false);
            if (entity == null)
            {
                return;
            }

            if (!entity.CanDeleteRecord())
            {
                throw new InvalidOperationException("Only stopped, expired, or fully cleaned-up failed sessions can delete records; destroy the runtime first.");
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
                      ?? throw new InvalidOperationException($"Runtime is not registered: {kind}");

        if (!await runtime.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            if (kind == SandboxRuntimeKind.Docker)
            {
                throw new InvalidOperationException(
                    "The Docker runtime is unavailable. Install and start Docker (or a Podman-compatible docker CLI); do not fall back to running untrusted code as a bare process.");
            }

            throw new InvalidOperationException($"Runtime {kind} is unavailable. The Wasm provider is still a stub; use Docker instead.");
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
            throw new InvalidOperationException("SessionId is required.");
        }

        var entity = await sessionService.GetBySessionIdAsync(sessionId.Trim()).ConfigureAwait(false)
                     ?? throw new InvalidOperationException($"The session does not exist: {sessionId}");
        if (entity.Status != SandboxSessionStatus.Running
            || string.IsNullOrWhiteSpace(entity.RuntimeHandle)
            || entity.RuntimeHandle.StartsWith("exec-placeholder:", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The current session is not a controllable running Lab.");
        }

        if (!SandboxTemplateCatalog.TryGet(entity.TemplateKey, out var template)
            || !template.Interactive
            || !template.SupportsInteractiveControl)
        {
            throw new InvalidOperationException(
                $"Template {entity.TemplateKey} does not support persistent Lab control.");
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
            throw new InvalidOperationException("The Workspace path is outside the allowed boundary.");
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
                throw new InvalidOperationException("The Workspace cannot cross symbolic links or reparse points.");
            }

            if (File.Exists(current)
                && new FileInfo(current).Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException("The Workspace cannot operate on symbolic links or reparse points.");
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
            if (_tenantScopeFactory != null)
            {
                // Sandbox sessions are tenant-scoped data; background TTL cleanup must iterate by tenant.
                await _tenantScopeFactory.ForEachEnabledTenantAsync(
                    async (scopeServiceProvider, ct) =>
                    {
                        var sessionService = scopeServiceProvider.GetRequiredService<SandboxSessionService>();
                        await SweepForTenantCoreAsync(sessionService, ct).ConfigureAwait(false);
                    },
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            using var scope = CreateTenantScope();
            await SweepForTenantCoreAsync(scope.ServiceProvider.GetRequiredService<SandboxSessionService>(), cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SweepForTenantCoreAsync(SandboxSessionService sessionService, CancellationToken cancellationToken)
    {
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
                if (runtime.Kind == SandboxRuntimeKind.Docker)
                {
                    _logger.LogWarning(
                        "Sandbox Docker runtime is unavailable. The Sandbox module remains loaded, " +
                        "but Docker-backed features are disabled until Docker CLI and daemon are available.");
                }

                continue;
            }

            if (_tenantScopeFactory != null)
            {
                // Sandbox sessions are tenant-scoped data; startup orphan cleanup must iterate by tenant.
                await _tenantScopeFactory.ForEachEnabledTenantAsync(
                    async (scopeServiceProvider, ct) =>
                    {
                        var sessionService = scopeServiceProvider.GetRequiredService<SandboxSessionService>();
                        await ReconcileRuntimeCoreAsync(runtime, sessionService, ct).ConfigureAwait(false);
                    },
                    cancellationToken).ConfigureAwait(false);
                continue;
            }

            using var scope = CreateTenantScope();
            await ReconcileRuntimeCoreAsync(
                runtime,
                scope.ServiceProvider.GetRequiredService<SandboxSessionService>(),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ReconcileOrphansInBackgroundAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ReconcileOrphansAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Sandbox startup reconciliation failed. The Sandbox module remains loaded, " +
                "but orphan cleanup will be retried by normal lifecycle operations.");
        }
    }

    private async Task ReconcileRuntimeCoreAsync(
        ISandboxRuntime runtime,
        SandboxSessionService sessionService,
        CancellationToken cancellationToken)
    {
        try
        {
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
                    ? "The Sandbox container has stopped; startup synchronized it to Stopped."
                    : "The Sandbox container no longer exists; startup synchronized it to Stopped.";
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
            ? "TTL is set to keep-alive; an administrator must destroy it manually."
            : $"TTL expiration time (UTC): {expiresAtUtc:u}.";
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
