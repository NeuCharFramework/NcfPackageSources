/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharPivotBoardModels.cs
    File description: Related implementation.
    Legacy comment translated to English.

    Created by: Senparc - 20260901

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(NeuCharPivotBoard))]
[Serializable]
public class NeuCharPivotBoard : EntityBase<int>
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Required, MaxLength(100)]
    public string PageKey { get; private set; }

    [Required, MaxLength(200)]
    public string Name { get; private set; }

    public string Description { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public int Columns { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public string BlocksJson { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public bool IsEnabled { get; private set; }

    public int AdminUserId { get; private set; }

    private NeuCharPivotBoard() { }

    public NeuCharPivotBoard(string pageKey, string name, int adminUserId)
    {
        PageKey = NormalizePageKey(pageKey);
        Name = name?.Trim() ?? string.Empty;
        AdminUserId = adminUserId;
        Description = string.Empty;
        Columns = 2;
        BlocksJson = "[]";
        IsEnabled = true;
    }

    public void UpdateInfo(string name, string description, int columns, bool isEnabled)
    {
        Name = string.IsNullOrWhiteSpace(name) ? Name : name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Columns = Math.Clamp(columns <= 0 ? 2 : columns, 1, 6);
        IsEnabled = isEnabled;
        SetUpdateTime();
    }

    public void RebindPage(string pageKey)
    {
        PageKey = NormalizePageKey(pageKey);
        SetUpdateTime();
    }

    public void SetBlocks(string blocksJson)
    {
        BlocksJson = string.IsNullOrWhiteSpace(blocksJson) ? "[]" : blocksJson;
        SetUpdateTime();
    }

    public static string NormalizePageKey(string pageKey)
    {
        pageKey = (pageKey ?? string.Empty).Trim().ToLowerInvariant();
        if (pageKey.StartsWith('/'))
        {
            pageKey = pageKey.TrimStart('/');
        }
        return string.IsNullOrEmpty(pageKey) ? "default" : pageKey;
    }
}
