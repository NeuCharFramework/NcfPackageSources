/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceGatewayController.cs
    File description: secure Workspace gateway for VS Code, Cursor, and desktop clients

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.2.2 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261119
    Change description: v0.2.9 Localized workspace management and runtime surfaces

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

/// <summary>
/// Authenticated HTTP gateway for policy-bound Workspace storage and isolated execution operations.
/// Every endpoint verifies the caller and workspace ownership before creating a client.
/// </summary>
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

    /// <summary>
    /// Initializes the Workspace gateway controller and its authorization/session dependencies.
    /// </summary>
    /// <param name="storage">Workspace metadata and content storage.</param>
    /// <param name="clientFactory">Factory for policy-bound workspace clients.</param>
    /// <param name="changeHub">Workspace change-event publisher.</param>
    /// <param name="processFactory">Factory exposing isolated-agent runtime status.</param>
    /// <param name="options">Workspace limits and gateway settings.</param>
    /// <param name="desktopSessionValidators">Optional desktop-session validators; exactly one is accepted.</param>
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

    /// <summary>
    /// Returns protocol, runtime, execution, size-limit, and feature capabilities.
    /// </summary>
    /// <returns>An HTTP 200 capability document, or an authorization failure.</returns>
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

    /// <summary>
    /// Lists workspaces owned by the authenticated actor.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel metadata enumeration.</param>
    /// <returns>The actor's visible workspace descriptors.</returns>
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

    /// <summary>
    /// Creates a workspace owned by the authenticated actor.
    /// </summary>
    /// <param name="request">The workspace display-name request.</param>
    /// <param name="cancellationToken">Token used to cancel creation.</param>
    /// <returns>The created workspace descriptor.</returns>
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

    /// <summary>
    /// Lists entries below a workspace-relative directory.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="path">The workspace-relative directory path.</param>
    /// <param name="cancellationToken">Token used to cancel enumeration.</param>
    /// <returns>The visible workspace entries.</returns>
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

    /// <summary>
    /// Retrieves metadata and the current ETag for a workspace entry.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="path">The workspace-relative path.</param>
    /// <param name="cancellationToken">Token used to cancel metadata lookup.</param>
    /// <returns>The entry metadata.</returns>
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

    /// <summary>
    /// Streams a workspace file as an octet-stream response and exposes its ETag.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="path">The workspace-relative file path.</param>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    /// <returns>The file response or a mapped gateway error.</returns>
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

    /// <summary>
    /// Writes a bounded request body to a workspace file using create, overwrite, and If-Match rules.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="path">The workspace-relative file path.</param>
    /// <param name="create">Whether a missing file may be created.</param>
    /// <param name="overwrite">Whether an existing file may be replaced.</param>
    /// <param name="cancellationToken">Token used to cancel the write.</param>
    /// <returns>The resulting file metadata and creation flag.</returns>
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

    /// <summary>
    /// Creates a workspace-relative directory.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="request">The relative directory path.</param>
    /// <param name="cancellationToken">Token used to cancel creation.</param>
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

    /// <summary>
    /// Deletes a workspace entry according to recursive and ETag policy.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="path">The workspace-relative path.</param>
    /// <param name="recursive">Whether directories may be deleted recursively.</param>
    /// <param name="cancellationToken">Token used to cancel deletion.</param>
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

    /// <summary>
    /// Moves a workspace entry to a new relative path.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="request">The source, target, overwrite, and ETag options.</param>
    /// <param name="cancellationToken">Token used to cancel the move.</param>
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

    /// <summary>
    /// Copies a workspace entry to a new relative path.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="request">The source, target, overwrite, and ETag options.</param>
    /// <param name="cancellationToken">Token used to cancel the copy.</param>
    [HttpPost("workspaces/{workspaceId}/copy")]
    [RequestSizeLimit(4096)]
    public async Task<IActionResult> CopyEntry(
        string workspaceId,
        [FromBody] WorkspaceCopyEntryRequest request,
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
            await client.CopyEntryAsync(request, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches workspace paths and content using the supplied bounded search request.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="request">The path, content, glob, regex, and result-limit options.</param>
    /// <param name="cancellationToken">Token used to cancel traversal.</param>
    /// <returns>Matching workspace results.</returns>
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

    /// <summary>
    /// Applies an exact-text patch after checking the optional hash and replacement count.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="request">The patch target, old text, replacement, and concurrency checks.</param>
    /// <param name="cancellationToken">Token used to cancel the patch.</param>
    /// <returns>The patch application result.</returns>
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

    /// <summary>
    /// Streams workspace change events using an authenticated Server-Sent Events response.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="replayBuffered">Whether buffered changes should be replayed first.</param>
    /// <param name="cancellationToken">Token used to cancel the event stream.</param>
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

    /// <summary>
    /// Authorizes workspace ownership, creates a policy-bound client, and maps errors
    /// for an operation returning a value.
    /// </summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="actorId">The authenticated actor identifier.</param>
    /// <param name="action">The operation to execute.</param>
    /// <param name="allowWrite">Whether the client may mutate workspace content.</param>
    /// <returns>The mapped HTTP action result.</returns>
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

    /// <summary>
    /// Creates a client bound to the authenticated actor and gateway read/write policy.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="actorId">The authenticated actor identifier.</param>
    /// <param name="allowWrite">Whether write operations are enabled.</param>
    /// <returns>A policy-bound workspace client.</returns>
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

    /// <summary>
    /// Resolves a workspace and verifies that it belongs to the authenticated actor.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="actorId">The authenticated actor identifier.</param>
    /// <param name="cancellationToken">Token used to cancel lookup.</param>
    /// <exception cref="FileNotFoundException">Thrown when the workspace does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when another actor owns the workspace.</exception>
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

    /// <summary>
    /// Authorizes the desktop gateway using secure transport, validator availability,
    /// desktop-session credentials, and the authenticated administrator identity.
    /// </summary>
    /// <param name="actorId">Receives the authenticated actor identifier.</param>
    /// <returns>An error result, or <see langword="null"/> when authorization succeeds.</returns>
    private ActionResult? AuthorizeGateway(out int actorId)
    {
        return AuthorizeGateway(out actorId, out _);
    }

    /// <summary>
    /// Performs the full gateway authorization check and returns a revocation token when applicable.
    /// </summary>
    /// <param name="actorId">Receives the authenticated actor identifier.</param>
    /// <param name="sessionRevoked">Receives the session-revocation cancellation token.</param>
    /// <returns>An error result, or <see langword="null"/> when authorization succeeds.</returns>
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

    /// <summary>
    /// Revalidates the current desktop session for an open event stream.
    /// </summary>
    /// <returns><see langword="true"/> when the current session remains authorized.</returns>
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

    /// <summary>
    /// Requires HTTPS for remote gateway calls while permitting loopback HTTP.
    /// </summary>
    /// <returns>An error result, or <see langword="null"/> when transport is allowed.</returns>
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

    /// <summary>
    /// Executes a gateway action and converts exceptions into HTTP ProblemDetails.
    /// </summary>
    /// <typeparam name="T">The action result type.</typeparam>
    /// <param name="action">The operation to run.</param>
    /// <returns>The mapped HTTP action result.</returns>
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

    /// <summary>
    /// Executes a no-content gateway action and maps exceptions into HTTP ProblemDetails.
    /// </summary>
    /// <param name="action">The operation to run.</param>
    /// <returns>The HTTP response result.</returns>
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

    /// <summary>
    /// Maps workspace domain and file-system exceptions to stable HTTP status/problem titles.
    /// </summary>
    /// <param name="exception">The exception to map.</param>
    /// <returns>An HTTP ProblemDetails result.</returns>
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
    /// <summary>
    /// Maps a storage descriptor to the gateway-safe descriptor shape.
    /// </summary>
    /// <param name="descriptor">The storage descriptor.</param>
    /// <returns>A descriptor without internal root paths or owner details.</returns>
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
