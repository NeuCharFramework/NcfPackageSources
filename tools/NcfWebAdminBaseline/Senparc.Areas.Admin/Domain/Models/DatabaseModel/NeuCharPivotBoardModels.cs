/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharPivotBoardModels.cs
    File description: Related implementation.
    Legacy comment translated to English.

    Created by: Senparc - 20260901

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

/// <summary>
/// Stores the administrator-owned layout and availability state for a NeuChar Pivot board.
/// The serialized block collection is kept in <see cref="BlocksJson"/> so the board can
/// be rendered and updated without changing the entity shape for each block type.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(NeuCharPivotBoard))]
[Serializable]
public class NeuCharPivotBoard : EntityBase<int>
{
    /// <summary>
    /// Gets the normalized page key to which this board is bound.
    /// </summary>
    [Required, MaxLength(100)]
    public string PageKey { get; private set; }

    /// <summary>
    /// Gets the display name shown for the board.
    /// </summary>
    [Required, MaxLength(200)]
    public string Name { get; private set; }

    /// <summary>
    /// Gets the optional explanatory text displayed with the board.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the number of layout columns used when arranging blocks.
    /// </summary>
    public int Columns { get; private set; }

    /// <summary>
    /// Gets the JSON representation of the board blocks.
    /// </summary>
    public string BlocksJson { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the board is available to the normal board lookup.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Gets the administrator who owns the board.
    /// </summary>
    public int AdminUserId { get; private set; }

    private NeuCharPivotBoard() { }

    /// <summary>
    /// Initializes a board with normalized page identity, default two-column layout,
    /// an empty block collection, and enabled state.
    /// </summary>
    /// <param name="pageKey">The page key to normalize and bind to the board.</param>
    /// <param name="name">The initial display name; null becomes an empty string.</param>
    /// <param name="adminUserId">The administrator who owns the board.</param>
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

    /// <summary>
    /// Updates the board's display metadata and clamps the column count to the supported range.
    /// A non-positive column value selects the default two-column layout.
    /// </summary>
    /// <param name="name">The replacement display name; blank input preserves the current name.</param>
    /// <param name="description">The replacement description; null becomes an empty string.</param>
    /// <param name="columns">The requested column count, constrained to one through six.</param>
    /// <param name="isEnabled">Whether the board should be returned by enabled-board queries.</param>
    public void UpdateInfo(string name, string description, int columns, bool isEnabled)
    {
        Name = string.IsNullOrWhiteSpace(name) ? Name : name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Columns = Math.Clamp(columns <= 0 ? 2 : columns, 1, 6);
        IsEnabled = isEnabled;
        SetUpdateTime();
    }

    /// <summary>
    /// Rebinds the board to a normalized page key and records the entity update time.
    /// </summary>
    /// <param name="pageKey">The new page key to normalize.</param>
    public void RebindPage(string pageKey)
    {
        PageKey = NormalizePageKey(pageKey);
        SetUpdateTime();
    }

    /// <summary>
    /// Replaces the serialized block collection, using an empty JSON array for blank input.
    /// </summary>
    /// <param name="blocksJson">The serialized board-block collection.</param>
    public void SetBlocks(string blocksJson)
    {
        BlocksJson = string.IsNullOrWhiteSpace(blocksJson) ? "[]" : blocksJson;
        SetUpdateTime();
    }

    /// <summary>
    /// Normalizes a page key by trimming whitespace, removing a leading slash,
    /// and returning <c>default</c> when no usable key remains.
    /// </summary>
    /// <param name="pageKey">The raw page key to normalize.</param>
    /// <returns>The normalized page key.</returns>
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
