/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FullNeuCharFunctionProvitAccessCache.cs
    File description: Related implementation.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Change description: v0.9.1 Updated module behavior and related support.

    Created by: Senparc - 20260917

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Log;

namespace Senparc.Areas.Admin.Domain.Cache;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public class FullNeuCharFunctionProvitAccess
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public List<NeuCharFunctionProvitAccess> Items { get; set; } = new();

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public Dictionary<string, NeuCharFunctionProvitAccess> Index { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public NeuCharFunctionProvitAccess Find(string moduleUid, string functionKey)
    {
        if (Index == null)
        {
            return null;
        }
        return Index.TryGetValue(
            NeuCharFunctionProvitAccess.GetPolicyKey(moduleUid, functionKey),
            out var item) ? item : null;
    }
}

/// <summary>
///Legacy comment translated to English.
/// </summary>
public class FullNeuCharFunctionProvitAccessCache : BaseCache<FullNeuCharFunctionProvitAccess>
{
    public const string CACHE_KEY = "FullNeuCharFunctionProvitAccessCache";

    private INcfDbData _dataContext => base._db as INcfDbData;

    public FullNeuCharFunctionProvitAccessCache(INcfDbData db)
        : base(CACHE_KEY, db)
    {
        //Legacy comment translated to English.
        base.TimeOut = 30;
    }

    public override FullNeuCharFunctionProvitAccess Update()
    {
        List<NeuCharFunctionProvitAccess> items = new();
        try
        {
            items = _dataContext.BaseDataContext
                .Set<NeuCharFunctionProvitAccess>()
                .Where(z => !z.Flag)
                .OrderBy(z => z.ModuleUid)
                .ThenBy(z => z.FunctionKey)
                .ToList();
        }
        catch (Exception ex)
        {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            var msg = $"FullNeuCharFunctionProvitAccessCache 访问数据库异常，降级为空策略集（回退代码属性基线）。\n{ex.Message}";
            LogUtility.SystemLogger.Debug(msg, ex);
        }

        var full = new FullNeuCharFunctionProvitAccess
        {
            UpdatedAtUtc = DateTime.UtcNow,
            Items = items,
            Index = BuildIndex(items)
        };

        base.SetData(full, base.TimeOut, null);
        return base.Data;
    }

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    private static Dictionary<string, NeuCharFunctionProvitAccess> BuildIndex(
        List<NeuCharFunctionProvitAccess> items)
    {
        var index = new Dictionary<string, NeuCharFunctionProvitAccess>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            index[NeuCharFunctionProvitAccess.GetPolicyKey(item.ModuleUid, item.FunctionKey)] = item;
        }
        return index;
    }
}
