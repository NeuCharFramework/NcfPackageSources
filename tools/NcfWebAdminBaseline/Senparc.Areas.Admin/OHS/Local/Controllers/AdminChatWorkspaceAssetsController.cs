/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatWorkspaceAssetsController.cs
    File description: AdminChatWorkspaceAssetsController related definitions and implementation

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.12.15 add bilingual contextual documentation help with iframe dialog and new-window access

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261124
    Change description: v0.12.29 Improved AdminChat, system log analysis, workflow commands, and workspace asset operations.

----------------------------------------------------------------*/

using System;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Authorization;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Areas.Admin.OHS.Local.Controllers;

[ApiController]
[Route("api/Senparc.Areas.Admin/WorkspaceAssets")]
[AdminOrJwtAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminChatWorkspaceAssetsController : ControllerBase
{
    private static readonly SemaphoreSlim DownloadSlots = new(2, 2);
    private const long LargeAttachmentTransportLimit = 320L * 1024 * 1024;
    private readonly AdminChatWorkspaceAssetService _assets;
    private readonly WorkspaceAssetShareStore _shares;
    private readonly WorkspaceAssetCapabilityStore _capabilities;
    private readonly ILogger<AdminChatWorkspaceAssetsController> _logger;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes the authenticated Workspace asset API and its protected-share/capability stores.
    /// </summary>
    /// <param name="assets">The owner-scoped asset service.</param>
    /// <param name="shares">The protected public-share store.</param>
    /// <param name="logger">The request diagnostic logger.</param>
    /// <param name="environment">The host environment used for HTTPS share policy.</param>
    public AdminChatWorkspaceAssetsController(
        AdminChatWorkspaceAssetService assets, WorkspaceAssetShareStore shares,
        ILogger<AdminChatWorkspaceAssetsController> logger, IWebHostEnvironment environment)
    {
        _assets = assets;
        _shares = shares;
        _capabilities = new WorkspaceAssetCapabilityStore();
        _logger = logger;
        _environment = environment;
    }

    private WorkspaceAssetCapabilityStore Capabilities =>
        HttpContext?.RequestServices?.GetService<WorkspaceAssetCapabilityStore>() ?? _capabilities;

    /// <summary>
    /// Returns a sanitized HTML preview for a session-owned workspace file.
    /// </summary>
    /// <param name="sessionId">The owner-scoped chat session.</param>
    /// <param name="path">The workspace-relative HTML path.</param>
    /// <param name="cancellationToken">Token used to cancel reads and rewriting.</param>
    /// <returns>An HTML response or a mapped authorization/validation error.</returns>
    [HttpGet("{sessionId:int}/preview")]
    public Task<IActionResult> Preview(int sessionId, [FromQuery] string path, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            var preview = await _assets.BuildPreviewAsync(session.WorkspaceId, path, cancellationToken);
            return Html(preview.Html);
        });

    /// <summary>
    /// Returns the upload limits and supported extensions for a session workspace.
    /// </summary>
    /// <param name="sessionId">The owner-scoped chat session.</param>
    /// <param name="cancellationToken">Token used to validate session access.</param>
    /// <returns>JSON capability metadata.</returns>
    [HttpGet("{sessionId:int}/attachments/capabilities")]
    public Task<IActionResult> AttachmentCapabilities(int sessionId, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            var attachments = HttpContext.RequestServices.GetRequiredService<AdminChatAttachmentService>();
            return Ok(new
            {
                maxFileBytes = attachments.LargeUploadLimit,
                inlineParseMaxFileBytes = attachments.UploadLimit,
                maxFiles = AdminChatAttachmentService.MaxAttachments,
                extensions = AdminChatAttachmentService.AllowedExtensions,
                notice = "小文件可本地解析；超过本地解析限制的文件会流式保存到 Workspace，不读取全文。请使用 Workspace/Python 分批处理，不会自动把大文件发送给 AI。"
            });
        });

    [HttpPost("{sessionId:int}/attachments")]
    [RequestSizeLimit(17 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 17 * 1024 * 1024)]
    /// <summary>
    /// Uploads a bounded attachment and optional image vision preview.
    /// </summary>
    /// <param name="sessionId">The owner-scoped chat session.</param>
    /// <param name="file">The multipart attachment.</param>
    /// <param name="visionPreview">Optional validated JPEG preview.</param>
    /// <param name="cancellationToken">Token used to cancel upload.</param>
    /// <returns>JSON attachment metadata.</returns>
    public Task<IActionResult> UploadAttachment(int sessionId, IFormFile file,
        IFormFile visionPreview, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            var attachment = await HttpContext.RequestServices.GetRequiredService<AdminChatAttachmentService>()
                .UploadAsync(session, file, visionPreview, cancellationToken);
            return Ok(new { attachment });
        });

    [HttpPost("{sessionId:int}/attachments/large")]
    [RequestSizeLimit(LargeAttachmentTransportLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = LargeAttachmentTransportLimit)]
    /// <summary>
    /// Streams a large attachment to workspace storage for deferred processing.
    /// </summary>
    /// <param name="sessionId">The owner-scoped chat session.</param>
    /// <param name="file">The multipart attachment.</param>
    /// <param name="cancellationToken">Token used to cancel streaming.</param>
    /// <returns>JSON attachment metadata.</returns>
    public Task<IActionResult> UploadLargeAttachment(int sessionId, IFormFile file,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            var attachment = await HttpContext.RequestServices.GetRequiredService<AdminChatAttachmentService>()
                .UploadLargeAsync(session, file, cancellationToken);
            return Ok(new { attachment });
        });

    [HttpPost("{sessionId:int}/attachments/delete")]
    [RequestSizeLimit(64 * 1024)]
    /// <summary>
    /// Deletes an uploaded attachment that is not referenced by an existing message.
    /// </summary>
    /// <param name="sessionId">The owner-scoped chat session.</param>
    /// <param name="request">The attachment path and expected ETag.</param>
    /// <param name="cancellationToken">Token used to cancel deletion.</param>
    /// <returns>JSON deletion status.</returns>
    public Task<IActionResult> DeletePendingAttachment(int sessionId,
        [FromBody] AdminChatAttachmentInput request, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            var messages = await HttpContext.RequestServices.GetRequiredService<AdminChatMessageService>()
                .GetSessionMessagesAsync(sessionId);
            await HttpContext.RequestServices.GetRequiredService<AdminChatAttachmentService>()
                .DeletePendingAsync(session, request, messages.messages, cancellationToken);
            return Ok(new { deleted = true });
        });

    [HttpGet("{sessionId:int}/download")]
    /// <summary>
    /// Downloads one file or a bounded ZIP archive from the session workspace.
    /// </summary>
    /// <param name="sessionId">The owner-scoped chat session.</param>
    /// <param name="path">The file or directory path, empty for workspace root.</param>
    /// <param name="cancellationToken">Token used to cancel download.</param>
    /// <returns>An octet-stream file response or a busy/error response.</returns>
    public Task<IActionResult> Download(int sessionId, [FromQuery] string path = "",
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            if (!await DownloadSlots.WaitAsync(0, cancellationToken))
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "下载任务繁忙，请稍后重试。" });
            try
            {
                var download = await _assets.DownloadAsync(session.WorkspaceId, path, cancellationToken);
                Response.Headers["X-Content-Type-Options"] = "nosniff";
                return File(download.Bytes, "application/octet-stream", download.FileName);
            }
            finally { DownloadSlots.Release(); }
        });

    [HttpPost("{sessionId:int}/capabilities")]
    [RequestSizeLimit(4096)]
    /// <summary>
    /// Creates a short-lived capability URL for one session-owned workspace file.
    /// </summary>
    /// <param name="sessionId">The owner-scoped chat session.</param>
    /// <param name="request">The target file path.</param>
    /// <param name="cancellationToken">Token used to cancel metadata access.</param>
    /// <returns>Capability URL, expiry, ETag, size, and content type.</returns>
    public Task<IActionResult> CreateCapability(
        int sessionId, [FromBody] WorkspaceCapabilityRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            var path = WorkspaceAssetPreviewBuilder.NormalizePath(request?.Path);
            var metadata = await _assets.Storage.GetFileInfoAsync(session.WorkspaceId, path, cancellationToken);
            if (metadata.IsDirectory)
                throw new ArgumentException("能力令牌只能访问文件。");
            var capability = Capabilities.Create(sessionId, session.WorkspaceId, path, metadata.ETag);
            return Ok(new
            {
                url = $"{Request.PathBase}/api/Senparc.Areas.Admin/WorkspaceAssets/asset/{capability.Id}/{capability.Token}",
                expiresAt = capability.ExpiresAt,
                etag = metadata.ETag,
                size = metadata.Length,
                contentType = WorkspaceTextFiles.ResolveContentType(path)
            });
        });

    [HttpPost("{sessionId:int}/manifest")]
    [RequestSizeLimit(4096)]
    /// <summary>
    /// Builds a bounded capability manifest for preview assets under one source scope.
    /// </summary>
    /// <param name="sessionId">The owner-scoped chat session.</param>
    /// <param name="request">The source preview path.</param>
    /// <param name="cancellationToken">Token used to cancel traversal.</param>
    /// <returns>JSON asset capability metadata.</returns>
    public Task<IActionResult> CreateManifest(
        int sessionId, [FromBody] WorkspaceCapabilityRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            var sourcePath = WorkspaceAssetPreviewBuilder.NormalizePath(request?.Path);
            var scope = sourcePath.Contains('/')
                ? sourcePath[..sourcePath.LastIndexOf('/')]
                : string.Empty;
            var queue = new Queue<(string Path, int Depth)>();
            queue.Enqueue((scope, 0));
            var assets = new Dictionary<string, object>(StringComparer.Ordinal);
            var visited = 0;
            while (queue.TryDequeue(out var directory))
            {
                if (directory.Depth > 4)
                    throw new ArgumentException("Workspace 预览依赖最多支持 4 层目录。");
                var entries = await _assets.Storage.ListAsync(
                    session.WorkspaceId, directory.Path, cancellationToken);
                foreach (var entry in entries)
                {
                    var path = WorkspaceAssetPreviewBuilder.NormalizePath(entry.RelativePath);
                    if (!path.StartsWith(scope.Length == 0 ? string.Empty : scope + "/", StringComparison.Ordinal))
                        throw new IOException("Workspace returned a path outside the preview scope.");
                    if (++visited > 100)
                        throw new ArgumentException("Workspace 预览依赖不能超过 100 个文件。");
                    if (entry.IsDirectory)
                    {
                        queue.Enqueue((path, directory.Depth + 1));
                        continue;
                    }
                    if (entry.Length > 32 * 1024 * 1024
                        || !WorkspaceAssetPreviewBuilder.IsPreviewAsset(path))
                        continue;
                    var metadata = await _assets.Storage.GetFileInfoAsync(
                        session.WorkspaceId, path, cancellationToken);
                    var capability = Capabilities.Create(
                        sessionId, session.WorkspaceId, path, metadata.ETag);
                    assets[path] = new
                    {
                        url = $"{Request.PathBase}/api/Senparc.Areas.Admin/WorkspaceAssets/asset/{capability.Id}/{capability.Token}",
                        etag = metadata.ETag,
                        size = entry.Length,
                        contentType = WorkspaceTextFiles.ResolveContentType(path)
                    };
                }
            }
            return Ok(new { scope, assets });
        });

    [AllowAnonymous]
    [HttpGet("asset/{id}/{token}")]
    /// <summary>
    /// Serves a public capability-protected workspace asset only while its ETag
    /// and short-lived token remain valid.
    /// </summary>
    /// <param name="id">The capability identifier.</param>
    /// <param name="token">The capability token.</param>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    /// <returns>The file response or HTTP 404 when the capability is invalid/stale.</returns>
    public async Task<IActionResult> Asset(string id, string token, CancellationToken cancellationToken)
    {
        if (!Capabilities.TryGet(id, token, out var capability))
            return NotFound();
        try
        {
            var metadata = await _assets.Storage.GetFileInfoAsync(
                capability.WorkspaceId, capability.Path, cancellationToken);
            if (metadata.IsDirectory
                || !string.Equals(metadata.ETag, capability.ETag, StringComparison.Ordinal))
                return NotFound();
            var file = await _assets.Storage.ReadFileBytesAsync(
                capability.WorkspaceId, capability.Path, 32 * 1024 * 1024, cancellationToken);
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Referrer-Policy"] = "no-referrer";
            Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
            Response.Headers.CacheControl = "no-store";
            return File(file.Content, WorkspaceTextFiles.ResolveContentType(capability.Path));
        }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException
            or WorkspaceLimitException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Lists public shares belonging to the owner-scoped session/workspace.
    /// </summary>
    /// <param name="sessionId">The owner-scoped chat session.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>Share metadata without HTML bodies.</returns>
    [HttpGet("{sessionId:int}/shares")]
    public Task<IActionResult> ListShares(int sessionId, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            EnsureShareTransport();
            var shares = await _shares.ListAsync(GetOwnerId(), sessionId, session.WorkspaceId, cancellationToken);
            return Ok(new { shares = System.Linq.Enumerable.Select(shares, ShareDto) });
        });

    [HttpPost("{sessionId:int}/shares")]
    [RequestSizeLimit(4096)]
    /// <summary>
    /// Creates an HTTPS-protected, expiring HTML snapshot share for a workspace path.
    /// </summary>
    /// <param name="sessionId">The owner-scoped chat session.</param>
    /// <param name="request">The source path and allowed lifetime.</param>
    /// <param name="cancellationToken">Token used to cancel preview/share creation.</param>
    /// <returns>Share metadata and preview warnings.</returns>
    public Task<IActionResult> CreateShare(int sessionId, [FromBody] CreateWorkspaceAssetShare request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            EnsureShareTransport();
            if (request.ExpiresInHours is not (1 or 24 or 168))
                throw new ArgumentException("分享有效期只能是 1 小时、24 小时或 7 天。");
            var preview = await _assets.BuildPreviewAsync(session.WorkspaceId, request.Path, cancellationToken);
            var share = await _shares.CreateAsync(GetOwnerId(), sessionId, session.WorkspaceId,
                request.Path, preview.Html, request.ExpiresInHours, cancellationToken);
            return Ok(new { share = ShareDto(share), warnings = preview.Warnings });
        });

    [HttpPost("{sessionId:int}/shares/{shareId}/revoke")]
    /// <summary>
    /// Revokes one share belonging to the owner-scoped session/workspace.
    /// </summary>
    /// <param name="sessionId">The owner-scoped chat session.</param>
    /// <param name="shareId">The share identifier.</param>
    /// <param name="cancellationToken">Token used to cancel revocation.</param>
    /// <returns>Success or not-found response.</returns>
    public Task<IActionResult> RevokeShare(int sessionId, string shareId, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var session = await _assets.GetSessionAsync(sessionId, User, cancellationToken);
            return await _shares.RevokeAsync(GetOwnerId(), sessionId, session.WorkspaceId, shareId, cancellationToken)
                ? Ok(new { revoked = true })
                : NotFound(new { message = "分享不存在或已失效。" });
        });

    [AllowAnonymous]
    [HttpGet("shared/{id}/{token}")]
    /// <summary>
    /// Serves an anonymous HTML share after HTTPS, rate-limit, expiration,
    /// workspace ownership, and token checks.
    /// </summary>
    /// <param name="id">The share identifier.</param>
    /// <param name="token">The share token.</param>
    /// <param name="cancellationToken">Token used to cancel share access.</param>
    /// <returns>The sanitized HTML share or an error response.</returns>
    public Task<IActionResult> Shared(string id, string token, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            Response.Headers["Referrer-Policy"] = "no-referrer";
            Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
            EnsureShareTransport();
            if (!_shares.TryAcquirePublicRequest(HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"))
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "访问过于频繁，请稍后重试。" });
            var share = await _shares.ReadPublicAsync(id, token, cancellationToken);
            if (share == null || !await _assets.IsShareWorkspaceAvailableAsync(share, cancellationToken))
                return NotFound(new { message = "分享不存在、已撤销或已过期。" });
            return Html(share.Html);
        });

    /// <summary>
    /// Projects a share into client metadata and its public URL.
    /// </summary>
    /// <param name="share">The persisted share.</param>
    /// <returns>An anonymous response DTO.</returns>
    private object ShareDto(WorkspaceAssetShare share) => new
    {
        share.Id, share.Path, share.CreatedAt, share.ExpiresAt,
        url = $"{Request.PathBase}/api/Senparc.Areas.Admin/WorkspaceAssets/shared/{share.Id}/{share.Token}"
    };

    /// <summary>
    /// Reads the authenticated administrator identifier from the current principal.
    /// </summary>
    /// <returns>The administrator identifier.</returns>
    private int GetOwnerId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    /// <summary>
    /// Requires HTTPS for anonymous share transport, allowing only development
    /// loopback HTTP for local testing.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when transport is not permitted.</exception>
    private void EnsureShareTransport()
    {
        if (Request.IsHttps) return;
        var loopbackHost = string.Equals(Request.Host.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || IPAddress.TryParse(Request.Host.Host, out var host) && IPAddress.IsLoopback(host);
        if (_environment.IsDevelopment() && loopbackHost
            && HttpContext.Connection.RemoteIpAddress is { } remote && IPAddress.IsLoopback(remote))
            return;
        throw new InvalidOperationException("匿名分享需要 HTTPS，本机回环地址调试除外；反向代理应正确转发 HTTPS 协议。");
    }

    /// <summary>
    /// Creates an HTML response with restrictive preview, framing, referrer,
    /// indexing, and cache headers.
    /// </summary>
    /// <param name="html">The sanitized HTML content.</param>
    /// <returns>The configured HTML file response.</returns>
    private FileContentResult Html(string html)
    {
        Response.Headers["Content-Security-Policy"] = WorkspaceAssetPreviewBuilder.ContentSecurityPolicy;
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
        Response.Headers["X-Frame-Options"] = "DENY";
        Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
        Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        Response.Headers.CacheControl = "no-store";
        return File(Encoding.UTF8.GetBytes(html), "text/html; charset=utf-8");
    }

    /// <summary>
    /// Runs an asset operation and maps authorization, validation, missing-file,
    /// and I/O exceptions to stable HTTP responses.
    /// </summary>
    /// <param name="action">The asset operation to execute.</param>
    /// <returns>The operation result or mapped error response.</returns>
    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (UnauthorizedAccessException error)
        {
            _logger.LogWarning("Workspace asset access denied: {Reason}", error.Message);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "无权访问此 Workspace。" });
        }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException)
        {
            return NotFound(new { message = "Workspace、文件或分享不存在。" });
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or InvalidDataException or NotSupportedException or System.Xml.XmlException)
        {
            _logger.LogWarning("Workspace asset request rejected: {Reason}", error.Message);
            return BadRequest(new { message = error.Message });
        }
        catch (IOException error)
        {
            _logger.LogError(error, "Workspace asset operation failed.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Workspace 文件操作失败，请检查服务器日志。" });
        }
    }

    public sealed class CreateWorkspaceAssetShare
    {
        public string Path { get; set; } = "";
        public int ExpiresInHours { get; set; } = 24;
    }

    public sealed class WorkspaceCapabilityRequest
    {
        public string Path { get; set; } = "";
    }
}
