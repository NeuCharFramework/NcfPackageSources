/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharFunctionProvitAccessModels.cs
    File description: Related implementation.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.
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

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

/// <summary>
///Legacy comment translated to English.
/// <para>
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </para>
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(NeuCharFunctionProvitAccess))]
[Serializable]
public class NeuCharFunctionProvitAccess : EntityBase<int>
{
    #region Legacy section

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public const int AccessModeInherit = 0;
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public const int AccessModeOpen = 1;
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public const int AccessModeRestricted = 2;
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public const int AccessModeDeny = 3;

    #endregion

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Required, MaxLength(100)]
    public string ModuleUid { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Required, MaxLength(200)]
    public string FunctionKey { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Required]
    public int AccessMode { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [MaxLength(2000)]
    public string AllowedRoleCodes { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [MaxLength(2000)]
    public string AllowedPermissionCodes { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [MaxLength(2000)]
    public string AllowedUserIds { get; private set; }

    private NeuCharFunctionProvitAccess() { }

    public NeuCharFunctionProvitAccess(string moduleUid, string functionKey)
    {
        ModuleUid = NormalizeModuleUid(moduleUid);
        FunctionKey = NormalizeFunctionKey(functionKey);
        AccessMode = AccessModeInherit;
        AllowedRoleCodes = string.Empty;
        AllowedPermissionCodes = string.Empty;
        AllowedUserIds = string.Empty;
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    ///Legacy comment translated to English.
    public void Update(
        int accessMode,
        IReadOnlyList<string> roleCodes,
        IReadOnlyList<string> permissionCodes,
        IReadOnlyList<int> adminUserIds,
        string remark = null)
    {
        if (accessMode is not (AccessModeInherit or AccessModeOpen or AccessModeRestricted or AccessModeDeny))
        {
            throw new ArgumentException($"不支持的 AccessMode：{accessMode}");
        }
        var roles = JoinCodes(roleCodes);
        var permissions = JoinCodes(permissionCodes);
        var users = JoinUserIds(adminUserIds);
        if (accessMode == AccessModeRestricted &&
            string.IsNullOrEmpty(roles) &&
            string.IsNullOrEmpty(permissions) &&
            string.IsNullOrEmpty(users))
        {
            throw new ArgumentException("受限（Restricted）策略必须至少绑定一个用户、角色或权限码。");
        }
        AccessMode = accessMode;
        AllowedRoleCodes = roles;
        AllowedPermissionCodes = permissions;
        AllowedUserIds = users;
        Remark = string.IsNullOrWhiteSpace(remark) ? Remark : remark.Trim();
        SetUpdateTime();
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public IReadOnlyList<string> RoleCodeList => ParseCodes(AllowedRoleCodes);

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public IReadOnlyList<string> PermissionCodeList => ParseCodes(AllowedPermissionCodes);

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public IReadOnlyList<int> AdminUserIdList =>
        ParseCodes(AllowedUserIds).Where(z => int.TryParse(z, out _)).Select(z => int.Parse(z)).ToList();

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public bool HasAnySubjectBinding =>
        !string.IsNullOrEmpty(AllowedRoleCodes) ||
        !string.IsNullOrEmpty(AllowedPermissionCodes) ||
        !string.IsNullOrEmpty(AllowedUserIds);

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public bool MatchesAdminUser(int adminUserId) =>
        adminUserId > 0 && AdminUserIdList.Contains(adminUserId);

    public static string NormalizeModuleUid(string moduleUid) => (moduleUid ?? string.Empty).Trim();

    public static string NormalizeFunctionKey(string functionKey) => (functionKey ?? string.Empty).Trim();

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static string GetPolicyKey(string moduleUid, string functionKey) =>
        $"{NormalizeModuleUid(moduleUid)}|{NormalizeFunctionKey(functionKey)}";

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static IReadOnlyList<string> ParseCodes(string codes)
    {
        if (string.IsNullOrWhiteSpace(codes))
        {
            return Array.Empty<string>();
        }
        return codes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(z => z.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static string JoinCodes(IReadOnlyList<string> codes)
    {
        if (codes == null || codes.Count == 0)
        {
            return string.Empty;
        }
        return string.Join(',',
            codes
                .Where(z => !string.IsNullOrWhiteSpace(z))
                .Select(z => z.Trim())
                .Where(z => z.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static string JoinUserIds(IReadOnlyList<int> adminUserIds)
    {
        if (adminUserIds == null || adminUserIds.Count == 0)
        {
            return string.Empty;
        }
        return string.Join(',', adminUserIds.Where(z => z > 0).Distinct());
    }
}
