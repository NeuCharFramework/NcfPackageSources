/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceFilesController.cs
    File description: WorkspaceFilesController related definitions and implementation

    Created by: Senparc - 20261102

    Modified by: Senparc - 20261103
    Change description: v0.12.15 add streamed large-file Workspace uploads with tenant-aware ownership checks

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Senparc.Areas.Admin;
using Senparc.Xncf.Workspace;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Areas.Admin.OHS.Local.Controllers;

[ApiController]
[Route("api/Senparc.Areas.Admin/WorkspaceFiles")]
[AdminOrJwtAuthorize("AdminSuperAdmin")]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class WorkspaceFilesController : ControllerBase
{
    private const long TransportLimitBytes = 320L * 1024 * 1024;
    private readonly IWorkspaceStorage _storage;
    private readonly WorkspaceOptions _options;

    /// <summary>
    /// Initializes the workspace file controller with storage and upload-limit settings.
    /// </summary>
    /// <param name="storage">The tenant-aware workspace storage service.</param>
    /// <param name="options">The workspace upload and quota options.</param>
    public WorkspaceFilesController(
        IWorkspaceStorage storage,
        IOptions<WorkspaceOptions> options)
    {
        _storage = storage;
        _options = options.Value;
    }

    /// <summary>
    /// Returns the file-size, workspace-quota, and target-directory capabilities exposed to the client.
    /// </summary>
    /// <returns>An <see cref="OkObjectResult"/> containing the configured upload capabilities.</returns>
    [HttpGet("capabilities")]
    public IActionResult Capabilities()
    {
        return Ok(new
        {
            maxFileBytes = _options.MaxUploadFileBytes,
            workspaceQuotaBytes = _options.MaxWorkspaceBytes,
            targetDirectory = "input"
        });
    }

    /// <summary>
    /// Streams one multipart file into the authenticated administrator's workspace input directory.
    /// The workspace owner, file name, individual size, and transport limits are validated before writing.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier receiving the file.</param>
    /// <param name="file">The multipart file to store.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>
    /// An <see cref="OkObjectResult"/> with stored metadata, or a client error when ownership,
    /// file, or configured limits are invalid.
    /// </returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the current administrator identity cannot be parsed.</exception>
    [HttpPost("{workspaceId}/upload")]
    [RequestSizeLimit(TransportLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = TransportLimitBytes)]
    public async Task<IActionResult> Upload(
        string workspaceId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentOwnerId();
        var workspace = await _storage.GetAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        if (workspace == null || workspace.OwnerId != ownerId)
        {
            return NotFound(new { message = "Workspace 不存在或不属于当前管理员。" });
        }

        if (file == null || file.Length <= 0)
        {
            return BadRequest(new { message = "上传文件不能为空。" });
        }

        if (file.Length > _options.MaxUploadFileBytes)
        {
            return BadRequest(new
            {
                message = $"单个文件不能超过 {_options.MaxUploadFileBytes} 字节。"
            });
        }

        var fileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (fileName.Length == 0
            || fileName.Length > 200
            || fileName.Any(char.IsControl)
            || fileName.Contains(':'))
        {
            return BadRequest(new { message = "文件名无效或超过 200 个字符。" });
        }

        var relativePath = $"input/{fileName}";
        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _storage.WriteFileStreamAsync(
                workspaceId,
                new WorkspaceStreamWriteRequest(
                    relativePath,
                    Overwrite: true,
                    ContentLength: file.Length),
                stream,
                cancellationToken).ConfigureAwait(false);
            return Ok(new
            {
                file = result.Metadata,
                message = "文件已上传到 Workspace 的 input 目录。"
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    /// <summary>
    /// Reads and validates the current administrator identifier from the authenticated principal.
    /// </summary>
    /// <returns>The positive administrator identifier.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the principal has no valid administrator identifier.</exception>
    private int GetCurrentOwnerId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out var ownerId) || ownerId <= 0)
        {
            throw new UnauthorizedAccessException("当前管理员身份无效。");
        }

        return ownerId;
    }
}
