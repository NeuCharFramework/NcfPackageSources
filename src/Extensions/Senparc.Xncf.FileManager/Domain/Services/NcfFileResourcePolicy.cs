/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: NcfFileResourcePolicy.cs
    File description: domain services and business-process implementation


    Created by: Senparc - 20260813

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview1 Updated module behavior and related support.

    Modified by: Senparc - 20261021
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.8.1 add SourceFileId, SourceType, and SourceUrl columns to PromptRange table for PostgreSQL, SQL Server, and SQLite

----------------------------------------------------------------*/

using Microsoft.AspNetCore.StaticFiles;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Senparc.Xncf.FileManager.Domain.Services;

/// <summary>
/// Centralizes the resource-boundary policy.  The public-asset profile deliberately
/// excludes HTML, SVG, JavaScript and archives: these files can become an XSS or
/// content-sniffing boundary when served from the site's own origin.
/// </summary>
public static class NcfFileResourcePolicy
{
    private static readonly HashSet<string> KnowledgeBaseExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".log", ".md", ".markdown", ".csv", ".tsv", ".json", ".xml",
        ".yaml", ".yml", ".html", ".htm", ".css", ".js", ".ts", ".cs", ".sql",
        ".docx", ".xlsx", ".pptx"
    };

    private static readonly HashSet<string> SiteAssetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".ico",
        ".mp3", ".wav", ".ogg", ".mp4", ".webm",
        ".woff", ".woff2", ".ttf", ".otf"
    };

    private static readonly HashSet<string> PrivateAttachmentExtensions = new(
        KnowledgeBaseExtensions
            .Concat(SiteAssetExtensions)
            .Concat(new[]
            {
                ".pdf", ".doc", ".xls", ".ppt",
                ".zip", ".7z", ".rar",
                ".amr", ".m4a", ".mov",
                ".jsonl"
            }),
        StringComparer.OrdinalIgnoreCase);

    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    public static bool IsValidScope(NcfFileResourceScope scope) =>
        scope is NcfFileResourceScope.KnowledgeBase
            or NcfFileResourceScope.SiteAsset
            or NcfFileResourceScope.PrivateAttachment
            or NcfFileResourceScope.Skill;

    public static string NormalizeExtension(string fileName)
    {
        return Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
    }

    public static bool IsAllowedExtension(NcfFileResourceScope scope, string extension)
    {
        return scope switch
        {
            NcfFileResourceScope.KnowledgeBase => KnowledgeBaseExtensions.Contains(extension),
            NcfFileResourceScope.SiteAsset => SiteAssetExtensions.Contains(extension),
            NcfFileResourceScope.PrivateAttachment or NcfFileResourceScope.Skill =>
                PrivateAttachmentExtensions.Contains(extension),
            _ => false
        };
    }

    /// <summary>
    /// Existing installations may already contain legacy formats which are no
    /// longer accepted for new KnowledgeBase uploads. They remain readable and
    /// downloadable, but cannot bypass the newer ingestion policy.
    /// </summary>
    public static bool IsAllowedStoredExtension(NcfFileResourceScope scope, string extension)
    {
        if (IsAllowedExtension(scope, extension))
        {
            return true;
        }

        return scope == NcfFileResourceScope.KnowledgeBase && extension is
            ".pdf" or ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or
            ".doc" or ".xls" or ".ppt" or ".zip";
    }

    public static string GetContentType(string extension)
    {
        if (!ContentTypeProvider.TryGetContentType($"file{extension}", out var contentType))
        {
            return "application/octet-stream";
        }

        return contentType;
    }

    public static string GetStorageRoot(NcfFileResourceScope scope)
    {
        return scope switch
        {
            NcfFileResourceScope.KnowledgeBase => "knowledge-base",
            NcfFileResourceScope.SiteAsset => "site-assets",
            NcfFileResourceScope.Skill => "skills",
            NcfFileResourceScope.PrivateAttachment => "private-attachments",
            _ => throw new ArgumentOutOfRangeException(nameof(scope))
        };
    }

    public static void EnsureCanPublish(NcfFile file)
    {
        if (file == null || file.ResourceScope != NcfFileResourceScope.SiteAsset)
        {
            throw new InvalidOperationException("只有站点静态资源可以公开发布。");
        }

        if (string.IsNullOrWhiteSpace(file.ContentHash) || file.ContentHash.Length != 64)
        {
            throw new InvalidOperationException("资源缺少完整性指纹，不能公开发布。");
        }
    }
}
