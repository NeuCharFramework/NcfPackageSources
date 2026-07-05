/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptAudioController.cs
    文件功能描述：PromptAudioController 控制器逻辑
    
    
    创建标识：Senparc - 20260705

    修改标识：Senparc - 20260705
    修改描述：v0.16.4-preview3 增强文生图重试机制并兼容 TLS1.2/TLS1.3----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Senparc.Xncf.AreaBase.Admin.Filters;
using System;
using System.IO;
using System.Linq;

namespace Senparc.Xncf.PromptRange.OHS.Local.Controllers;

[ApiController]
[ApiAuthorize("AdminOnly")]
[Route("api/Senparc.Xncf.PromptRange/[controller]/[action]")]
public class PromptAudioController : ControllerBase
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();
    private static readonly string[] AllowedPrefixes = new[]
    {
        "PromptRange/TextToSpeech",
        "PromptRange/SpeechToText"
    };

    [HttpGet]
    public IActionResult Get(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest("path is required");
        }

        var normalizedPath = NormalizeRelativePath(path);
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return BadRequest("path is invalid");
        }

        if (!AllowedPrefixes.Any(prefix => normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("path prefix is invalid");
        }

        var appDataRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "App_Data"));
        var fullPath = Path.GetFullPath(Path.Combine(appDataRoot, normalizedPath.Replace('/', Path.DirectorySeparatorChar)));
        var expectedPrefix = appDataRoot.EndsWith(Path.DirectorySeparatorChar)
            ? appDataRoot
            : appDataRoot + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("path is out of App_Data");
        }

        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        if (!ContentTypeProvider.TryGetContentType(fullPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
    }

    private static string NormalizeRelativePath(string path)
    {
        var normalized = (path ?? string.Empty).Trim().Replace('\\', '/');
        while (normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = normalized.Substring(1);
        }

        if (normalized.Contains("../", StringComparison.Ordinal) ||
            normalized.Contains("..\\", StringComparison.Ordinal))
        {
            return null;
        }

        return normalized;
    }
}
