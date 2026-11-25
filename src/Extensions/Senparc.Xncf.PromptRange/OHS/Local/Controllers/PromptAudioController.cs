/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptAudioController.cs
    File description: Related implementation.

    Created by: Senparc - 20260705

    Modified by: Senparc - 20260705

    Modified by: Senparc - 20260729
    Change description: v0.17.1-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Authorization;
using System;
using System.IO;
using System.Linq;

namespace Senparc.Xncf.PromptRange.OHS.Local.Controllers;

[ApiController]
[ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
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
    /// <summary>
    /// Serves an authorized PromptRange audio file under the configured App_Data prefixes.
    /// </summary>
    /// <param name="path">The relative TextToSpeech or SpeechToText path.</param>
    /// <returns>A range-enabled file result or an HTTP validation/not-found response.</returns>
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

    /// <summary>
    /// Normalizes a user-supplied relative path and rejects traversal segments.
    /// </summary>
    /// <param name="path">The raw relative path.</param>
    /// <returns>A slash-normalized path, or <see langword="null"/> when traversal is detected.</returns>
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
