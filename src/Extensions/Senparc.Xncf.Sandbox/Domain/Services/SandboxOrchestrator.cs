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

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

    Modified by: Senparc - 20261125
    Change description: v0.4.4 Improved Sandbox setup, orchestration, and localized user feedback.

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

    /// <summary>
    /// Creates the sandbox workspace root, starts periodic TTL cleanup, and launches
    /// non-blocking orphan reconciliation.
    /// </summary>
    /// <param name="cancellationToken">Token supplied by the host lifecycle.</param>
    /// <returns>A completed task after housekeeping has been scheduled.</returns>
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

    /// <summary>
    /// Stops cleanup timers, signals active sandbox work to stop, and waits for startup
    /// reconciliation when cancellation permits.
    /// </summary>
    /// <param name="cancellationToken">Token bounding shutdown wait time.</param>
    /// <returns>A task that completes after shutdown cleanup.</returns>
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

    /// <summary>
    /// Releases timers, shutdown cancellation, and the orchestration gate.
    /// </summary>
    public void Dispose()
    {
        _ttlTimer?.Dispose();
        _shutdown.Dispose();
        _gate.Dispose();
    }

    /// <summary>
    /// Gets all registered sandbox template definitions.
    /// </summary>
    /// <returns>The immutable template catalog view.</returns>
    public IReadOnlyCollection<SandboxTemplateDefinition> ListTemplates() => SandboxTemplateCatalog.All;

    /// <summary>
    /// Creates a quota-checked sandbox session, initializes its workspace, and starts
    /// the selected interactive runtime or tracks an Exec-only placeholder.
    /// </summary>
    /// <param name="ownerUserId">The owning administrator/user identifier.</param>
    /// <param name="templateKey">The sandbox template key.</param>
    /// <param name="preferredRuntime">An optional runtime override.</param>
    /// <param name="initializeWorkspace">Optional workspace initialization callback.</param>
    /// <param name="ncfPreview">Optional controlled NCF preview configuration.</param>
    /// <param name="ttlMinutes">Optional requested TTL.</param>
    /// <param name="keepAlive">Whether the session should have no expiry.</param>
    /// <param name="alias">Optional user-facing alias.</param>
    /// <param name="extraPortMappings">Optional extra Jupyter port mappings.</param>
    /// <param name="cancellationToken">Token used to cancel creation.</param>
    /// <returns>A task containing the created session information.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the template, quota, runtime, ports, or controlled preview options are invalid.</exception>
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

    /// <summary>
    /// Updates the expiration policy of an existing creating or running session.
    /// </summary>
    /// <param name="sessionId">The sandbox session identifier.</param>
    /// <param name="ttlMinutes">The requested TTL in minutes.</param>
    /// <param name="keepAlive">Whether to remove the expiry.</param>
    /// <param name="cancellationToken">Token used to cancel the update.</param>
    /// <returns>A task containing the updated session information.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the session is missing, terminal, or its template is unavailable.</exception>
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

    /// <summary>
    /// Updates the optional display alias of an existing sandbox session.
    /// </summary>
    /// <param name="sessionId">The sandbox session identifier.</param>
    /// <param name="alias">The new alias; blank clears the alias.</param>
    /// <param name="cancellationToken">Token used to cancel the update.</param>
    /// <returns>A task containing the updated session information.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the session is missing or the alias exceeds its limit.</exception>
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

    /// <summary>
    /// Executes code through an Exec-only sandbox template after validating ownership
    /// of the tracked session and applying template-specific code defaults.
    /// </summary>
    /// <param name="sessionId">The sandbox session identifier.</param>
    /// <param name="code">The code to execute.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <returns>A task containing stdout/stderr and execution status.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the session or template does not support Exec.</exception>
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

    /// <summary>
    /// Executes an interactive command against a running sandbox session.
    /// </summary>
    /// <param name="sessionId">The sandbox session identifier.</param>
    /// <param name="command">The interactive command or input.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <returns>A task containing interactive execution output and status.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the session is missing, not interactive, or not running.</exception>
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

    /// <summary>
    /// Uploads a byte-array workspace file through the stream-based upload path.
    /// </summary>
    /// <param name="sessionId">The sandbox session identifier.</param>
    /// <param name="relativePath">The relative workspace path.</param>
    /// <param name="content">The file bytes.</param>
    /// <param name="overwrite">Whether an existing file may be replaced.</param>
    /// <param name="cancellationToken">Token used to cancel upload.</param>
    /// <returns>A task containing file metadata.</returns>
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

    /// <summary>
    /// Streams a workspace file to a temporary path, enforces size/path boundaries,
    /// and atomically moves it into the session workspace.
    /// </summary>
    /// <param name="sessionId">The sandbox session identifier.</param>
    /// <param name="relativePath">The relative workspace path.</param>
    /// <param name="content">The source stream.</param>
    /// <param name="contentLength">Optional declared content length.</param>
    /// <param name="overwrite">Whether an existing file may be replaced.</param>
    /// <param name="cancellationToken">Token used to cancel upload.</param>
    /// <returns>A task containing file metadata.</returns>
    /// <exception cref="InvalidOperationException">Thrown when size, declared length, overwrite, or path limits are violated.</exception>
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

    /// <summary>
    /// Reads a bounded file from a session workspace after validating its path and size.
    /// </summary>
    /// <param name="sessionId">The sandbox session identifier.</param>
    /// <param name="relativePath">The relative workspace path.</param>
    /// <param name="maxBytes">Optional maximum read size.</param>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    /// <returns>A task containing file metadata and bytes.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when path or size limits are violated.</exception>
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

    /// <summary>
    /// Lists bounded workspace files under a session directory, optionally recursively,
    /// while skipping reparse points and inaccessible entries.
    /// </summary>
    /// <param name="sessionId">The sandbox session identifier.</param>
    /// <param name="relativeDirectory">The relative directory to enumerate.</param>
    /// <param name="recursive">Whether subdirectories are included.</param>
    /// <param name="maxItems">Optional maximum result count.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task containing workspace file metadata.</returns>
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

    /// <summary>
    /// Reconciles the current tenant and lists recent sandbox sessions.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel reconciliation and listing.</param>
    /// <returns>A task containing sessions ordered by creation time.</returns>
    public async Task<IReadOnlyList<SandboxSessionInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        await SynchronizeCurrentTenantAsync(cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Deletes up to 200 requested session records, returning identifiers skipped by ownership,
    /// state, or deletion constraints.
    /// </summary>
    /// <param name="sessionIds">The session identifiers to process.</param>
    /// <param name="cancellationToken">Token used to cancel deletion.</param>
    /// <returns>Deleted count and skipped identifiers.</returns>
    public async Task<(int Deleted, IReadOnlyList<string> Skipped)> DeleteRecordsAsync(
        IEnumerable<string> sessionIds,
        CancellationToken cancellationToken = default)
    {
        var ids = sessionIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(200)
            .ToArray();
        if (ids.Length == 0)
        {
            return (0, Array.Empty<string>());
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var scope = CreateTenantScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
            var skipped = new List<string>();
            var deleted = 0;
            foreach (var sessionId in ids)
            {
                var entity = await sessionService.GetBySessionIdAsync(sessionId).ConfigureAwait(false);
                if (entity == null || !entity.CanDeleteRecord())
                {
                    skipped.Add(sessionId);
                    continue;
                }

                TryDeleteWorkspace(sessionId);
                await sessionService.DeletePermanentlyAsync(entity).ConfigureAwait(false);
                deleted++;
            }

            _logger.LogInformation(
                "Bulk deleted {DeletedCount} sandbox session records; skipped {SkippedCount}.",
                deleted,
                skipped.Count);
            return (deleted, skipped);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Gets one sandbox session after tenant synchronization.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Token used to cancel synchronization.</param>
    /// <returns>A task containing session information, or <see langword="null"/> when absent.</returns>
    public async Task<SandboxSessionInfo?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using var scope = CreateTenantScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
        var entity = await sessionService.GetBySessionIdAsync(sessionId).ConfigureAwait(false);
        return entity == null ? null : ToInfo(entity);
    }

    /// <summary>
    /// Maps a sandbox session entity to its public session-information DTO.
    /// </summary>
    /// <param name="entity">The session entity.</param>
    /// <returns>The public session information.</returns>
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
    /// <summary>
    /// Resolves a running Jupyter session's loopback proxy target without updating activity timestamps.
    /// The returned access token is for server-side proxy use only.
    /// </summary>
    /// <param name="sessionId">The sandbox session identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the lookup.</param>
    /// <returns>A task containing the proxy target, or <see langword="null"/> when unavailable.</returns>
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
    /// <summary>
    /// Resolves the loopback target for a running NCF preview session for server-side proxying.
    /// </summary>
    /// <param name="sessionId">The sandbox session identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the lookup.</param>
    /// <returns>A task containing the proxy target, or <see langword="null"/> when unavailable.</returns>
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

    /// <summary>
    /// Stops the runtime, removes the workspace, and marks a sandbox session stopped.
    /// </summary>
    /// <param name="sessionId">The sandbox session identifier.</param>
    /// <param name="cancellationToken">Token used to cancel runtime destruction.</param>
    /// <returns>A task that completes after cleanup.</returns>
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

    /// <summary>
    /// Permanently deletes a stopped/expired session record and its workspace.
    /// </summary>
    /// <param name="sessionId">The sandbox session identifier.</param>
    /// <param name="cancellationToken">Token used to cancel deletion.</param>
    /// <returns>A task that completes after deletion.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the session is not eligible for permanent deletion.</exception>
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

    /// <summary>
    /// Resolves an available sandbox runtime and rejects unavailable unsafe fallbacks.
    /// </summary>
    /// <param name="kind">The requested runtime kind.</param>
    /// <param name="cancellationToken">Token used for availability probing.</param>
    /// <returns>A task containing the available runtime.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the runtime is not registered or available.</exception>
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

    /// <summary>
    /// Loads and validates a running interactive session and resolves its runtime controller.
    /// </summary>
    /// <param name="sessionService">The scoped session service.</param>
    /// <param name="sessionId">The sandbox session identifier.</param>
    /// <param name="cancellationToken">Token used for runtime availability probing.</param>
    /// <returns>A task containing the entity, template, and runtime context.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the session is missing, stopped, or not interactive.</exception>
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

    /// <summary>
    /// Enforces workspace containment and rejects symbolic links/reparse points along the path.
    /// </summary>
    /// <param name="workspace">The allowed workspace root.</param>
    /// <param name="target">The target path to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when the target escapes the workspace boundary.</exception>
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

    /// <summary>
    /// Maps a file-system path to workspace-relative file metadata.
    /// </summary>
    /// <param name="workspace">The workspace root.</param>
    /// <param name="path">The file path.</param>
    /// <returns>Workspace-relative file information.</returns>
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

    /// <summary>
    /// Acquires the orchestration gate and sweeps expired sessions across enabled tenants.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the sweep.</param>
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

    /// <summary>
    /// Finds expired sessions for one tenant and destroys their runtime/workspace state.
    /// </summary>
    /// <param name="sessionService">The tenant-scoped session service.</param>
    /// <param name="cancellationToken">Token used to cancel cleanup.</param>
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

    /// <summary>
    /// Destroys one expired runtime, removes its workspace, and persists the expired state.
    /// </summary>
    /// <param name="sessionService">The tenant-scoped session service.</param>
    /// <param name="entity">The expired session entity.</param>
    /// <param name="cancellationToken">Token used to cancel runtime destruction.</param>
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

    /// <summary>
    /// Reconciles runtime handles against persisted sessions across available runtimes and tenants.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel reconciliation.</param>
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

    /// <summary>
    /// Runs startup orphan reconciliation without allowing failures to prevent host startup.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel background reconciliation.</param>
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

    /// <summary>
    /// Reconciles available runtimes for the current tenant under the orchestration gate.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel synchronization.</param>
    private async Task SynchronizeCurrentTenantAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var scope = CreateTenantScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<SandboxSessionService>();
            foreach (var runtime in _runtimes)
            {
                if (await runtime.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
                {
                    await ReconcileRuntimeCoreAsync(runtime, sessionService, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Aligns persisted active sessions with runtime handles and destroys orphaned handles.
    /// </summary>
    /// <param name="runtime">The runtime being reconciled.</param>
    /// <param name="sessionService">The tenant-scoped session service.</param>
    /// <param name="cancellationToken">Token used to cancel runtime queries.</param>
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

    /// <summary>
    /// Matches a runtime handle against returned handles using exact or prefix-compatible comparison.
    /// </summary>
    /// <param name="handles">The runtime handles returned by the provider.</param>
    /// <param name="handle">The persisted handle to find.</param>
    /// <returns><see langword="true"/> when a compatible handle exists.</returns>
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

    /// <summary>
    /// Formats the session TTL state for user-facing status messages.
    /// </summary>
    /// <param name="expiresAtUtc">The session expiration timestamp.</param>
    /// <returns>A keep-alive or UTC expiration message.</returns>
    private static string FormatTtlMessage(DateTime expiresAtUtc)
    {
        return SandboxTtlPolicy.IsUnlimited(expiresAtUtc)
            ? "TTL is set to keep-alive; an administrator must destroy it manually."
            : $"TTL expiration time (UTC): {expiresAtUtc:u}.";
    }

    /// <summary>
    /// Best-effort deletes the workspace directory for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier used to locate the workspace.</param>
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
