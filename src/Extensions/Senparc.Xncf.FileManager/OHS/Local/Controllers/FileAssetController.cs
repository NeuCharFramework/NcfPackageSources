/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FileAssetController.cs
    文件功能描述：HTTP 控制器与远程接口


    创建标识：Senparc - 20260813

    修改标识：Senparc - 20260813
    修改描述：v0.6.0-preview1 完善文件资源边界、安全删除策略与静态资源管理

    修改标识：Senparc - 20260915
    修改描述：v0.7.0 优化文件资源在线预览与安全发布路径

----------------------------------------------------------------*/

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
        var located = await _fileService.TryGetPhysicalPathAsync(id, requirePublicSiteAsset: true);
        if (located == null)
        {
            return NotFound();
        }

        var (file, fullPath) = located.Value;
        var contentHash = file.ContentHash;
        if (string.IsNullOrWhiteSpace(contentHash) || contentHash.Length < 16)
        {
            return NotFound();
        }

        var hasFingerprint = !string.IsNullOrWhiteSpace(fingerprint);
        var fingerprintMatches = hasFingerprint && contentHash.StartsWith(fingerprint, StringComparison.OrdinalIgnoreCase);
        if (hasFingerprint && (!fingerprintMatches || fingerprint.Length < 12))
        {
            return NotFound();
        }

        var etag = $"\"{contentHash}\"";
        if (Request.Headers.IfNoneMatch.ToString().Contains(etag, StringComparison.Ordinal))
        {
            return StatusCode(304);
        }

        Response.Headers["ETag"] = etag;
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = fingerprintMatches
            ? "public,max-age=31536000,immutable"
            : "no-store";

        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        return PhysicalFile(fullPath, contentType);
    }
}
