/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceGatewayController.cs
    文件功能描述：VS Code、Cursor 与桌面客户端安全 Workspace 网关

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Shared.Abstractions.Security;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace.OHS.Local.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "Bearer_Backend", Policy = "AdminSuperAdmin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/Senparc.Xncf.Workspace")]
public sealed class WorkspaceGatewayController : ControllerBase
{
    public const int CurrentProtocolVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IWorkspaceStorage _storage;
    private readonly IWorkspaceClientFactory _clientFactory;
    private readonly WorkspaceChangeHub _changeHub;
    private readonly WorkspaceAgentProcessFactory _processFactory;
    private readonly WorkspaceOptions _options;
    private readonly IDesktopBridgeSessionValidator? _desktopSessionValidator;

    public WorkspaceGatewayController(
        IWorkspaceStorage storage,
        IWorkspaceClientFactory clientFactory,
        WorkspaceChangeHub changeHub,
        WorkspaceAgentProcessFactory processFactory,
        IOptions<WorkspaceOptions> options,
        IEnumerable<IDesktopBridgeSessionValidator>? desktopSessionValidators = null)
    {
        _storage = storage;
        _clientFactory = clientFactory;
        _changeHub = changeHub;
        _processFactory = processFactory;
        _options = options.Value;
        var validators = desktopSessionValidators?.Take(2).ToArray() ?? [];
        _desktopSessionValidator = validators.Length == 1 ? validators[0] : null;
    }

    [HttpGet("capabilities")]
    public ActionResult<WorkspaceGatewayCapabilities> GetCapabilities()
    {
        var authorizationFailure = AuthorizeGateway(out _);
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        var runtime = _processFactory.Status;
        return Ok(new WorkspaceGatewayCapabilities(
            CurrentProtocolVersion,
            runtime.Provider,
            runtime.IsolationLevel,
            runtime.ExecutionEnabled,
            _options.MaxFileReadBytes,
            _options.MaxFileWriteBytes,
            SupportsBinaryFiles: true,
            SupportsEtags: true,
            SupportsEvents: true));
    }

    [HttpGet("workspaces")]
    public async Task<ActionResult<IReadOnlyList<WorkspaceGatewayDescriptor>>> ListWorkspaces(
        CancellationToken cancellationToken)
    {
        var authorizationFailure = AuthorizeGateway(out var actorId);
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        return await ExecuteAsync(async () =>
        {
            var workspaces = await _storage.ListWorkspacesAsync(cancellationToken).ConfigureAwait(false);
            return (IReadOnlyList<WorkspaceGatewayDescriptor>)workspaces
                .Where(workspace => workspace.OwnerId == actorId)
                .Select(WorkspaceGatewayDescriptor.From)
                .ToArray();
        }).ConfigureAwait(false);
    }

    [HttpPost("workspaces")]
    [RequestSizeLimit(4096)]
    public async Task<ActionResult<WorkspaceGatewayDescriptor>> CreateWorkspace(
        [FromBody] WorkspaceGatewayCreateRequest request,
        CancellationToken cancellationToken)
    {
        var authorizationFailure = AuthorizeGateway(out var actorId);
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        return await ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Workspace name is required.");
            }

            var descriptor = await _storage.CreateAsync(
                new WorkspaceCreateRequest(actorId, request.Name.Trim()),
                cancellationToken).ConfigureAwait(false);
            return WorkspaceGatewayDescriptor.From(descriptor);
        }).ConfigureAwait(false);
    }

    [HttpGet("workspaces/{workspaceId}/entries")]
    public async Task<ActionResult<IReadOnlyList<WorkspaceEntry>>> ListEntries(
        string workspaceId,
        [FromQuery] string path = "",
        CancellationToken cancellationToken = default)
    {
        var authorizationFailure = AuthorizeGateway(out var actorId);
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        return await WithClientAsync(
            workspaceId,
            actorId,
            client => client.ListAsync(path, cancellationToken)).ConfigureAwait(false);
    }

    [HttpGet("workspaces/{workspaceId}/stat")]
    public async Task<ActionResult<WorkspaceFileMetadata>> Stat(
        string workspaceId,
        [FromQuery] string path = "",
        CancellationToken cancellationToken = default)
    {
        var authorizationFailure = AuthorizeGateway(out var actorId);
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        return await WithClientAsync(
            workspaceId,
            actorId,
            client => client.GetFileInfoAsync(path, cancellationToken)).ConfigureAwait(false);
    }

    [HttpGet("workspaces/{workspaceId}/file")]
    public async Task<IActionResult> ReadFile(
        string workspaceId,
        [FromQuery] string path,
        CancellationToken cancellationToken)
    {
        var authorizationFailure = AuthorizeGateway(out var actorId);
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        try
        {
            await EnsureOwnedWorkspaceAsync(workspaceId, actorId, cancellationToken).ConfigureAwait(false);
            await using var client = CreateClient(workspaceId, actorId, allowWrite: false);
            var file = await client
                .ReadFileAsync(path, _options.MaxFileReadBytes, cancellationToken)
                .ConfigureAwait(false);
            Response.Headers.ETag = file.Metadata.ETag;
            Response.Headers.LastModified = file.Metadata.LastWriteTimeUtc.ToString("R");
            return File(file.Content, "application/octet-stream", enableRangeProcessing: false);
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    [HttpPut("workspaces/{workspaceId}/file")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<ActionResult<WorkspaceWriteFileResult>> WriteFile(
        string workspaceId,
        [FromQuery] string path,
        [FromQuery] bool create = true,
        [FromQuery] bool overwrite = true,
        CancellationToken cancellationToken = default)
    {
        var authorizationFailure = AuthorizeGateway(out var actorId);
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        return await ExecuteAsync(async () =>
        {
            await EnsureOwnedWorkspaceAsync(workspaceId, actorId, cancellationToken).ConfigureAwait(false);
            if (Request.ContentLength is > 0 && Request.ContentLength > _options.MaxFileWriteBytes)
            {
                throw new InvalidOperationException("Workspace upload exceeds the configured write limit.");
            }

            await using var buffer = new MemoryStream();
            await Request.Body.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (buffer.Length > _options.MaxFileWriteBytes)
            {
                throw new InvalidOperationException("Workspace upload exceeds the configured write limit.");
            }

            await using var client = CreateClient(workspaceId, actorId, allowWrite: true);
            return await client.WriteFileAsync(
                new WorkspaceWriteFileRequest(
                    path,
                    buffer.ToArray(),
                    create,
                    overwrite,
                    Request.Headers.IfMatch.FirstOrDefault() ?? string.Empty),
                cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [HttpPost("workspaces/{workspaceId}/directories")]
    [RequestSizeLimit(4096)]
    public async Task<IActionResult> CreateDirectory(
        string workspaceId,
        [FromBody] WorkspaceGatewayPathRequest request,
        CancellationToken cancellationToken)
    {
        var authorizationFailure = AuthorizeGateway(out var actorId);
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        return await ExecuteNoContentAsync(async () =>
        {
            await EnsureOwnedWorkspaceAsync(workspaceId, actorId, cancellationToken).ConfigureAwait(false);
            await using var client = CreateClient(workspaceId, actorId, allowWrite: true);
            await client.CreateDirectoryAsync(request.Path, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [HttpDelete("workspaces/{workspaceId}/entry")]
    public async Task<IActionResult> DeleteEntry(
        string workspaceId,
        [FromQuery] string path,
        [FromQuery] bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        var authorizationFailure = AuthorizeGateway(out var actorId);
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        return await ExecuteNoContentAsync(async () =>
        {
            await EnsureOwnedWorkspaceAsync(workspaceId, actorId, cancellationToken).ConfigureAwait(false);
            await using var client = CreateClient(workspaceId, actorId, allowWrite: true);
            await client.DeleteEntryAsync(
                new WorkspaceDeleteEntryRequest(
                    path,
                    recursive,
                    Request.Headers.IfMatch.FirstOrDefault() ?? string.Empty),
                cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [HttpPost("workspaces/{workspaceId}/move")]
    [RequestSizeLimit(4096)]
    public async Task<IActionResult> MoveEntry(
        string workspaceId,
        [FromBody] WorkspaceMoveEntryRequest request,
        CancellationToken cancellationToken)
    {
        var authorizationFailure = AuthorizeGateway(out var actorId);
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        return await ExecuteNoContentAsync(async () =>
        {
            await EnsureOwnedWorkspaceAsync(workspaceId, actorId, cancellationToken).ConfigureAwait(false);
            await using var client = CreateClient(workspaceId, actorId, allowWrite: true);
            await client.MoveEntryAsync(request, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [HttpPost("workspaces/{workspaceId}/search")]
    [RequestSizeLimit(16 * 1024)]
    public async Task<ActionResult<IReadOnlyList<WorkspaceSearchResult>>> Search(
        string workspaceId,
        [FromBody] WorkspaceSearchRequest request,
        CancellationToken cancellationToken)
    {
        var authorizationFailure = AuthorizeGateway(out var actorId);
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        return await WithClientAsync(
            workspaceId,
            actorId,
            client => client.SearchAsync(request, cancellationToken)).ConfigureAwait(false);
    }

    [HttpPost("workspaces/{workspaceId}/patch")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<ActionResult<WorkspacePatchResult>> Patch(
        string workspaceId,
        [FromBody] WorkspacePatchRequest request,
        CancellationToken cancellationToken)
    {
        var authorizationFailure = AuthorizeGateway(out var actorId);
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        return await WithClientAsync(
            workspaceId,
            actorId,
            client => client.ApplyPatchAsync(request, cancellationToken),
            allowWrite: true).ConfigureAwait(false);
    }

    [HttpGet("workspaces/{workspaceId}/events")]
    public async Task Events(
        string workspaceId,
        bool replayBuffered = false,
        CancellationToken cancellationToken = default)
    {
        var authorizationFailure = AuthorizeGateway(out var actorId, out var sessionRevoked);
        if (authorizationFailure != null)
        {
            await authorizationFailure.ExecuteResultAsync(ControllerContext).ConfigureAwait(false);
            return;
        }

        try
        {
            await EnsureOwnedWorkspaceAsync(workspaceId, actorId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await MapException(exception).ExecuteResultAsync(ControllerContext).ConfigureAwait(false);
            return;
        }

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        using var streamCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            sessionRevoked);
        try
        {
            await Response.WriteAsync(": connected\n\n", streamCancellation.Token).ConfigureAwait(false);
            await Response.Body.FlushAsync(streamCancellation.Token).ConfigureAwait(false);
            await foreach (var change in _changeHub
                               .Subscribe(workspaceId, replayBuffered, streamCancellation.Token)
                               .ConfigureAwait(false))
            {
                if (!HasAuthorizedDesktopSession())
                {
                    return;
                }

                await Response.WriteAsync(
                    $"id: {change.Sequence}\nevent: workspace-change\ndata: {JsonSerializer.Serialize(change, JsonOptions)}\n\n",
                    streamCancellation.Token).ConfigureAwait(false);
                await Response.Body.FlushAsync(streamCancellation.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (streamCancellation.IsCancellationRequested)
        {
        }
    }

    private async Task<ActionResult<T>> WithClientAsync<T>(
        string workspaceId,
        int actorId,
        Func<IWorkspaceClient, Task<T>> action,
        bool allowWrite = false)
    {
        return await ExecuteAsync(async () =>
        {
            await EnsureOwnedWorkspaceAsync(workspaceId, actorId, HttpContext.RequestAborted)
                .ConfigureAwait(false);
            await using var client = CreateClient(workspaceId, actorId, allowWrite);
            return await action(client).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private IWorkspaceClient CreateClient(string workspaceId, int actorId, bool allowWrite)
    {
        return _clientFactory.Create(new WorkspaceAccessContext(
            workspaceId,
            new WorkspacePolicy(
                AllowRead: true,
                AllowWrite: allowWrite,
                MaxBytes: Math.Max(_options.MaxFileReadBytes, _options.MaxFileWriteBytes)),
            "DesktopGateway",
            actorId.ToString(),
            User.FindFirstValue("jti") ?? string.Empty));
    }

    private async Task EnsureOwnedWorkspaceAsync(
        string workspaceId,
        int actorId,
        CancellationToken cancellationToken)
    {
        WorkspaceDescriptor? descriptor;
        try
        {
            descriptor = await _storage.GetAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        }
        catch (DirectoryNotFoundException)
        {
            descriptor = null;
        }

        if (descriptor == null)
        {
            throw new FileNotFoundException("Workspace does not exist.");
        }

        if (descriptor.OwnerId != actorId)
        {
            throw new UnauthorizedAccessException("Workspace belongs to another administrator.");
        }
    }

    private ActionResult? AuthorizeGateway(out int actorId)
    {
        return AuthorizeGateway(out actorId, out _);
    }

    private ActionResult? AuthorizeGateway(
        out int actorId,
        out CancellationToken sessionRevoked)
    {
        actorId = 0;
        sessionRevoked = CancellationToken.None;
        var transportFailure = ValidateSecureTransport();
        if (transportFailure != null)
        {
            return transportFailure;
        }

        if (_desktopSessionValidator == null)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Title = "Workspace Gateway is inactive",
                    Detail = "Install and enable exactly one DesktopBridge session validator.",
                    Status = StatusCodes.Status503ServiceUnavailable
                });
        }

        var suppliedToken = Request.Headers[_desktopSessionValidator.HeaderName].FirstOrDefault();
        if (!_desktopSessionValidator.TryAuthorize(suppliedToken, out sessionRevoked))
        {
            return Unauthorized();
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId) || actorId <= 0)
        {
            return Forbid();
        }

        return null;
    }

    private bool HasAuthorizedDesktopSession()
    {
        if (_desktopSessionValidator == null)
        {
            return false;
        }

        return _desktopSessionValidator.TryAuthorize(
            Request.Headers[_desktopSessionValidator.HeaderName].FirstOrDefault(),
            out _);
    }

    private ActionResult? ValidateSecureTransport()
    {
        var remoteAddress = HttpContext.Connection.RemoteIpAddress;
        if (Request.IsHttps || remoteAddress != null && IPAddress.IsLoopback(remoteAddress))
        {
            return null;
        }

        return StatusCode(
            StatusCodes.Status403Forbidden,
            new ProblemDetails
            {
                Title = "Workspace Gateway requires secure transport",
                Detail = "Remote access requires HTTPS. Plain HTTP is accepted only from loopback or a local tunnel.",
                Status = StatusCodes.Status403Forbidden
            });
    }

    private async Task<ActionResult<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action().ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private async Task<IActionResult> ExecuteNoContentAsync(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
            return NoContent();
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private ObjectResult MapException(Exception exception)
    {
        var (status, title) = exception switch
        {
            WorkspaceConflictException => (StatusCodes.Status409Conflict, "Workspace file changed"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Workspace access denied"),
            FileNotFoundException or DirectoryNotFoundException => (StatusCodes.Status404NotFound, "Workspace entry not found"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Workspace request is invalid"),
            IOException => (StatusCodes.Status409Conflict, "Workspace operation conflicted"),
            _ => (StatusCodes.Status500InternalServerError, "Workspace operation failed")
        };
        return StatusCode(
            status,
            new ProblemDetails
            {
                Title = title,
                Detail = status == StatusCodes.Status500InternalServerError
                    ? "The Workspace Agent could not complete the operation."
                    : exception.Message,
                Status = status
            });
    }
}

public sealed record WorkspaceGatewayCapabilities(
    int ProtocolVersion,
    string RuntimeProvider,
    string IsolationLevel,
    bool ExecutionEnabled,
    int MaxReadBytes,
    int MaxWriteBytes,
    bool SupportsBinaryFiles,
    bool SupportsEtags,
    bool SupportsEvents);

public sealed record WorkspaceGatewayDescriptor(
    string Id,
    string Name,
    DateTime CreatedUtc,
    DateTime LastAccessedUtc,
    string StorageProvider)
{
    public static WorkspaceGatewayDescriptor From(WorkspaceDescriptor descriptor)
    {
        return new WorkspaceGatewayDescriptor(
            descriptor.Id,
            descriptor.Name,
            descriptor.CreatedUtc,
            descriptor.LastAccessedUtc,
            descriptor.StorageProvider);
    }
}

public sealed record WorkspaceGatewayCreateRequest(string Name);

public sealed record WorkspaceGatewayPathRequest(string Path);
