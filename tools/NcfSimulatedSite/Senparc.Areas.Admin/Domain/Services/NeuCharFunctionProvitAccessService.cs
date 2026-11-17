/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharFunctionProvitAccessService.cs
    File description: Related implementation.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Change description: v0.9.1 Updated module behavior and related support.

    Created by: Senparc - 20260917

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Cache;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed class NeuCharFunctionProvitAccessInput
{
    public string ModuleUid { get; set; }
    public string FunctionKey { get; set; }
    public int AccessMode { get; set; } = NeuCharFunctionProvitAccess.AccessModeInherit;
    public List<string> AllowedRoleCodes { get; set; } = new();
    public List<string> AllowedPermissionCodes { get; set; } = new();
    public List<int> AllowedUserIds { get; set; } = new();
    public string Remark { get; set; }
}

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed class NeuCharFunctionProvitAccessService : BaseClientService<NeuCharFunctionProvitAccess>
{
    private readonly INeuCharFunctionProvitAccessRepository _repository;
    private readonly FullNeuCharFunctionProvitAccessCache _cache;

    public NeuCharFunctionProvitAccessService(
        INeuCharFunctionProvitAccessRepository repository,
        IServiceProvider serviceProvider,
        FullNeuCharFunctionProvitAccessCache cache)
        : base(repository, serviceProvider)
    {
        _repository = repository;
        _cache = cache;
    }

    #region Legacy section

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public NeuCharFunctionProvitAccess GetPolicy(string moduleUid, string functionKey)
    {
        var full = _cache.Data;
        return full?.Find(moduleUid, functionKey);
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public IReadOnlyList<NeuCharFunctionProvitAccess> GetAllPolicies()
    {
        var full = _cache.Data;
        return full?.Items is { Count: > 0 } items
            ? items
            : Array.Empty<NeuCharFunctionProvitAccess>();
    }

    #endregion

    #region Legacy section

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public async Task<NeuCharFunctionProvitAccess> UpsertPolicyAsync(
        NeuCharFunctionProvitAccessInput input,
        CancellationToken cancellationToken = default)
    {
        if (input == null ||
            string.IsNullOrWhiteSpace(input.ModuleUid) ||
            string.IsNullOrWhiteSpace(input.FunctionKey))
        {
            throw new ArgumentException("ModuleUid 与 FunctionKey 不能为空。");
        }

        var moduleUid = NeuCharFunctionProvitAccess.NormalizeModuleUid(input.ModuleUid);
        var functionKey = NeuCharFunctionProvitAccess.NormalizeFunctionKey(input.FunctionKey);

        var policy = await base.GetObjectAsync(
                z => z.ModuleUid == moduleUid && z.FunctionKey == functionKey && !z.Flag)
            .ConfigureAwait(false)
            ?? new NeuCharFunctionProvitAccess(moduleUid, functionKey);

        policy.Update(
            input.AccessMode,
            input.AllowedRoleCodes,
            input.AllowedPermissionCodes,
            input.AllowedUserIds,
            input.Remark);
        await base.SaveObjectAsync(policy).ConfigureAwait(false);
        await _cache.RemoveCacheAsync().ConfigureAwait(false);
        return policy;
    }

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public async Task<bool> DeletePolicyAsync(
        string moduleUid,
        string functionKey,
        CancellationToken cancellationToken = default)
    {
        var policy = await base.GetObjectAsync(
                z => z.ModuleUid == NeuCharFunctionProvitAccess.NormalizeModuleUid(moduleUid)
                    && z.FunctionKey == NeuCharFunctionProvitAccess.NormalizeFunctionKey(functionKey)
                    && !z.Flag)
            .ConfigureAwait(false);
        if (policy == null)
        {
            return false;
        }
        await _repository.DeleteAsync(policy, softDelete: false).ConfigureAwait(false);
        await _cache.RemoveCacheAsync().ConfigureAwait(false);
        return true;
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    ///Legacy comment translated to English.
    public async Task<int> BatchUpsertAsync(
        IEnumerable<NeuCharFunctionProvitAccessInput> inputs,
        CancellationToken cancellationToken = default)
    {
        var saved = 0;
        foreach (var input in inputs ?? Array.Empty<NeuCharFunctionProvitAccessInput>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (input == null ||
                string.IsNullOrWhiteSpace(input.ModuleUid) ||
                string.IsNullOrWhiteSpace(input.FunctionKey))
            {
                continue;
            }
            var moduleUid = NeuCharFunctionProvitAccess.NormalizeModuleUid(input.ModuleUid);
            var functionKey = NeuCharFunctionProvitAccess.NormalizeFunctionKey(input.FunctionKey);

            var policy = await base.GetObjectAsync(
                    z => z.ModuleUid == moduleUid && z.FunctionKey == functionKey && !z.Flag)
                .ConfigureAwait(false)
                ?? new NeuCharFunctionProvitAccess(moduleUid, functionKey);

            policy.Update(
                input.AccessMode,
                input.AllowedRoleCodes,
                input.AllowedPermissionCodes,
                input.AllowedUserIds,
                input.Remark);
            await base.SaveObjectAsync(policy).ConfigureAwait(false);
            saved++;
        }
        if (saved > 0)
        {
            await _cache.RemoveCacheAsync().ConfigureAwait(false);
        }
        return saved;
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    ///Legacy comment translated to English.
    public async Task<int> BatchDeleteAsync(
        IEnumerable<(string ModuleUid, string FunctionKey)> keys,
        CancellationToken cancellationToken = default)
    {
        var deleted = 0;
        foreach (var (moduleUid, functionKey) in keys ?? Array.Empty<(string, string)>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var policy = await base.GetObjectAsync(
                    z => z.ModuleUid == NeuCharFunctionProvitAccess.NormalizeModuleUid(moduleUid)
                        && z.FunctionKey == NeuCharFunctionProvitAccess.NormalizeFunctionKey(functionKey)
                        && !z.Flag)
                .ConfigureAwait(false);
            if (policy == null)
            {
                continue;
            }
            await _repository.DeleteAsync(policy, softDelete: false).ConfigureAwait(false);
            deleted++;
        }
        if (deleted > 0)
        {
            await _cache.RemoveCacheAsync().ConfigureAwait(false);
        }
        return deleted;
    }

    #endregion
}
