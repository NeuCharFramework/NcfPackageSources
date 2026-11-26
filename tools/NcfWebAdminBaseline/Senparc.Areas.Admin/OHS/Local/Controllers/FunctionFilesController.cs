/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FunctionFilesController.cs
    File description: Related implementation.

    Created by: Senparc - 20261101

    Modified by: Senparc - 20261103
    Change description: v0.12.15 add bulk file upload functionality to Sandbox Lab

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.XncfBase.Functions.Files;

namespace Senparc.Areas.Admin.OHS.Local.Controllers;

[ApiController]
[Route("api/Senparc.Areas.Admin/FunctionFiles")]
[AdminOrJwtAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class FunctionFilesController : ControllerBase
{
    private const long TransportLimitBytes = 256L * 1024 * 1024;
    private readonly IFunctionFileStore _fileStore;
    private readonly FunctionFileStoreOptions _options;

    /// <summary>
    /// Initializes the function-file controller with its storage service and limits.
    /// </summary>
    /// <param name="fileStore">The file store used to persist and retrieve function files.</param>
    /// <param name="options">The configured file-size and batch limits.</param>
    public FunctionFilesController(
        IFunctionFileStore fileStore,
        IOptions<FunctionFileStoreOptions> options)
    {
        _fileStore = fileStore;
        _options = options?.Value ?? new FunctionFileStoreOptions();
    }

    /// <summary>
    /// Returns the individual-file, total-batch, and file-count limits exposed to the client.
    /// </summary>
    /// <returns>An <see cref="OkObjectResult"/> containing the configured upload capabilities.</returns>
    [HttpGet("capabilities")]
    public IActionResult Capabilities()
    {
        return Ok(new
        {
            maxFileBytes = _options.MaxFileBytes,
            maxBatchBytes = _options.MaxBatchBytes,
            maxFilesPerBatch = _options.MaxFilesPerBatch
        });
    }

    /// <summary>
    /// Streams a bounded batch of multipart files to the function-file store.
    /// Each stream is disposed after persistence completes or fails.
    /// </summary>
    /// <param name="files">The multipart files to store.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>
    /// An <see cref="OkObjectResult"/> containing stored references, or a bad-request result
    /// when any configured batch or file constraint is violated.
    /// </returns>
    [HttpPost("upload")]
    [RequestSizeLimit(TransportLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = TransportLimitBytes)]
    public async Task<IActionResult> Upload(
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new { message = "至少需要上传一个文件。" });
        }

        if (files.Count > _options.MaxFilesPerBatch)
        {
            return BadRequest(new
            {
                message = $"一次最多上传 {_options.MaxFilesPerBatch} 个文件。"
            });
        }

        var totalBytes = files.Sum(file => file?.Length ?? 0L);
        if (files.Any(file => file == null || file.Length <= 0))
        {
            return BadRequest(new { message = "上传文件不能为空。" });
        }

        if (files.Any(file => file.Length > _options.MaxFileBytes))
        {
            return BadRequest(new
            {
                message = $"单个文件不能超过 {_options.MaxFileBytes} 字节。"
            });
        }

        if (totalBytes > _options.MaxBatchBytes)
        {
            return BadRequest(new
            {
                message = $"本次上传总大小不能超过 {_options.MaxBatchBytes} 字节。"
            });
        }

        var uploads = new List<FunctionFileUpload>(files.Count);
        var streams = new List<Stream>(files.Count);
        try
        {
            foreach (var file in files)
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);
                uploads.Add(new FunctionFileUpload
                {
                    Content = stream,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Length = file.Length
                });
            }

            try
            {
                var references = await _fileStore.SaveManyAsync(uploads, cancellationToken)
                    .ConfigureAwait(false);
                return Ok(new
                {
                    files = references
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Retrieves a stored function-file reference by identifier.
    /// </summary>
    /// <param name="fileId">The file identifier returned by upload.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The file reference, a not-found result, or a bad-request result for invalid identifiers.</returns>
    [HttpGet("{fileId}")]
    public async Task<IActionResult> Get(
        string fileId,
        CancellationToken cancellationToken)
    {
        try
        {
            var reference = await _fileStore.GetAsync(fileId, cancellationToken).ConfigureAwait(false);
            return reference == null
                ? NotFound(new { message = "文件不存在或已过期。" })
                : Ok(reference);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes the stored function file identified by the request route.
    /// </summary>
    /// <param name="fileId">The file identifier to delete.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>An empty success response, or a bad-request result when the store rejects the identifier.</returns>
    [HttpDelete("{fileId}")]
    public async Task<IActionResult> Delete(
        string fileId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _fileStore.DeleteAsync(fileId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
