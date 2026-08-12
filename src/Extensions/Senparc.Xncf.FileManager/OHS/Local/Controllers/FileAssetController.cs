using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senparc.Xncf.FileManager.Domain.Services;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.FileManager.OHS.Local.Controllers;

/// <summary>
/// Anonymous endpoint for explicitly published site assets. It resolves an ID
/// through FileManager metadata instead of exposing an App_Data path, so a URL
/// cannot be turned into arbitrary file-system access.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("assets")]
public sealed class FileAssetController : ControllerBase
{
    private readonly NcfFileService _fileService;

    public FileAssetController(NcfFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpGet("{id:int}/{fingerprint?}")]
    public async Task<IActionResult> Get(int id, string fingerprint = null)
    {
        var result = await _fileService.OpenReadAsync(id, requirePublicSiteAsset: true);
        if (result == null)
        {
            return NotFound();
        }

        var contentHash = result.File.ContentHash;
        if (string.IsNullOrWhiteSpace(contentHash) || contentHash.Length < 16)
        {
            await result.Stream.DisposeAsync();
            return NotFound();
        }

        var hasFingerprint = !string.IsNullOrWhiteSpace(fingerprint);
        var fingerprintMatches = hasFingerprint && contentHash.StartsWith(fingerprint, StringComparison.OrdinalIgnoreCase);
        if (hasFingerprint && (!fingerprintMatches || fingerprint.Length < 12))
        {
            await result.Stream.DisposeAsync();
            return NotFound();
        }

        var etag = $"\"{contentHash}\"";
        if (Request.Headers.IfNoneMatch.ToString().Contains(etag, StringComparison.Ordinal))
        {
            await result.Stream.DisposeAsync();
            return StatusCode(304);
        }

        Response.Headers["ETag"] = etag;
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = fingerprintMatches
            ? "public,max-age=31536000,immutable"
            : "no-store";

        return new FileStreamResult(
            result.Stream,
            string.IsNullOrWhiteSpace(result.File.ContentType) ? "application/octet-stream" : result.File.ContentType)
        {
            EnableRangeProcessing = true
        };
    }
}
