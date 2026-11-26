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
/// Stores the administrator access policy for one registered module function.
/// A policy can inherit the default decision, allow everyone, restrict access to
/// selected subjects, or deny access explicitly.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(NeuCharFunctionProvitAccess))]
[Serializable]
public class NeuCharFunctionProvitAccess : EntityBase<int>
{
    #region Legacy section

    /// <summary>
    /// Indicates that the function inherits the default access decision.
    /// </summary>
    public const int AccessModeInherit = 0;
    /// <summary>
    /// Indicates that the function is available without a subject-specific restriction.
    /// </summary>
    public const int AccessModeOpen = 1;
    /// <summary>
    /// Indicates that the function is available only to configured subjects.
    /// </summary>
    public const int AccessModeRestricted = 2;
    /// <summary>
    /// Indicates that the function is denied explicitly by this policy.
    /// </summary>
    public const int AccessModeDeny = 3;

    #endregion

    /// <summary>
    /// Gets the registered module UID to which the policy applies.
    /// </summary>
    [Required, MaxLength(100)]
    public string ModuleUid { get; private set; }

    /// <summary>
    /// Gets the stable function key to which the policy applies.
    /// </summary>
    [Required, MaxLength(200)]
    public string FunctionKey { get; private set; }

    /// <summary>
    /// Gets the numeric access mode represented by this policy.
    /// </summary>
    [Required]
    public int AccessMode { get; private set; }

    /// <summary>
    /// Gets the comma-separated role codes allowed by a restricted policy.
    /// </summary>
    [MaxLength(2000)]
    public string AllowedRoleCodes { get; private set; }

    /// <summary>
    /// Gets the comma-separated permission codes allowed by a restricted policy.
    /// </summary>
    [MaxLength(2000)]
    public string AllowedPermissionCodes { get; private set; }

    /// <summary>
    /// Gets the comma-separated administrator identifiers allowed by a restricted policy.
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
    /// Updates the policy mode and its role, permission, and administrator bindings.
    /// Restricted policies must contain at least one subject binding.
    /// </summary>
    /// <param name="accessMode">One of the declared access-mode constants.</param>
    /// <param name="roleCodes">Role codes allowed by the restricted policy.</param>
    /// <param name="permissionCodes">Permission codes allowed by the restricted policy.</param>
    /// <param name="adminUserIds">Administrator identifiers allowed by the restricted policy.</param>
    /// <param name="remark">Optional replacement remark.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="accessMode"/> is unsupported or a restricted policy has no bindings.
    /// </exception>
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
    /// Gets the distinct role codes parsed from <see cref="AllowedRoleCodes"/>.
    /// </summary>
    public IReadOnlyList<string> RoleCodeList => ParseCodes(AllowedRoleCodes);

    /// <summary>
    /// Gets the distinct permission codes parsed from <see cref="AllowedPermissionCodes"/>.
    /// </summary>
    public IReadOnlyList<string> PermissionCodeList => ParseCodes(AllowedPermissionCodes);

    /// <summary>
    /// Gets the positive administrator identifiers parsed from <see cref="AllowedUserIds"/>.
    /// </summary>
    public IReadOnlyList<int> AdminUserIdList =>
        ParseCodes(AllowedUserIds).Where(z => int.TryParse(z, out _)).Select(z => int.Parse(z)).ToList();

    /// <summary>
    /// Gets a value indicating whether any role, permission, or administrator binding is stored.
    /// </summary>
    public bool HasAnySubjectBinding =>
        !string.IsNullOrEmpty(AllowedRoleCodes) ||
        !string.IsNullOrEmpty(AllowedPermissionCodes) ||
        !string.IsNullOrEmpty(AllowedUserIds);

    /// <summary>
    /// Determines whether the specified administrator is directly bound to this policy.
    /// </summary>
    /// <param name="adminUserId">The administrator identifier to test.</param>
    /// <returns><see langword="true"/> when the identifier is positive and is included in the binding list.</returns>
    public bool MatchesAdminUser(int adminUserId) =>
        adminUserId > 0 && AdminUserIdList.Contains(adminUserId);

    /// <summary>
    /// Trims a module UID for consistent policy-key storage.
    /// </summary>
    /// <param name="moduleUid">The raw module UID.</param>
    /// <returns>The trimmed UID, or an empty string for null input.</returns>
    public static string NormalizeModuleUid(string moduleUid) => (moduleUid ?? string.Empty).Trim();

    /// <summary>
    /// Trims a function key for consistent policy-key storage.
    /// </summary>
    /// <param name="functionKey">The raw function key.</param>
    /// <returns>The trimmed key, or an empty string for null input.</returns>
    public static string NormalizeFunctionKey(string functionKey) => (functionKey ?? string.Empty).Trim();

    /// <summary>
    /// Combines a normalized module UID and function key into the policy lookup key.
    /// </summary>
    /// <param name="moduleUid">The module UID component.</param>
    /// <param name="functionKey">The function-key component.</param>
    /// <returns>A stable key in the form <c>moduleUid|functionKey</c>.</returns>
    public static string GetPolicyKey(string moduleUid, string functionKey) =>
        $"{NormalizeModuleUid(moduleUid)}|{NormalizeFunctionKey(functionKey)}";

    /// <summary>
    /// Parses a comma-separated code list into trimmed, case-insensitively distinct values.
    /// </summary>
    /// <param name="codes">The stored comma-separated code list.</param>
    /// <returns>A non-null list of distinct codes.</returns>
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
    /// Joins non-empty role or permission codes into the storage representation.
    /// </summary>
    /// <param name="codes">The code values to normalize and join.</param>
    /// <returns>A comma-separated distinct code list, or an empty string for no usable values.</returns>
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
    /// Joins positive administrator identifiers into the storage representation.
    /// </summary>
    /// <param name="adminUserIds">The administrator identifiers to normalize and join.</param>
    /// <returns>A comma-separated distinct identifier list, or an empty string for no positive values.</returns>
    public static string JoinUserIds(IReadOnlyList<int> adminUserIds)
    {
        if (adminUserIds == null || adminUserIds.Count == 0)
        {
            return string.Empty;
        }
        return string.Join(',', adminUserIds.Where(z => z > 0).Distinct());
    }
}
