/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharPivotGlobalFunctionService.cs
    File description: Related implementation.
    Legacy comment translated to English.

    Created by: Senparc - 20260829

    Modified by: Senparc - 20260829
    Change description: v0.7.0 Updated module behavior and related support.

    Modified by: Senparc - 20260917
    Change description: v0.9.1 add Function global Provit database access strategy (DB overrides code properties)，

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.WorkContext;
using Senparc.Ncf.Core.WorkContext.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed record NeuCharGlobalFunctionResolution(
    NeuCharFunctionDescriptor Descriptor,
    string ErrorMessage)
{
    /// <summary>
    /// Gets a value indicating whether a descriptor was resolved without an access or lookup error.
    /// </summary>
    public bool Success => Descriptor != null && string.IsNullOrWhiteSpace(ErrorMessage);
}

public sealed class NeuCharPivotGlobalAccessService
{
    private readonly IAdminWorkContextProvider _workContextProvider;
    private readonly ICheckPermission _checkPermission;

    public NeuCharPivotGlobalAccessService(
        IAdminWorkContextProvider workContextProvider,
        ICheckPermission checkPermission)
    {
        _workContextProvider = workContextProvider;
        _checkPermission = checkPermission;
    }

    /// <summary>
    /// Evaluates the current administrator's access to a globally exposed function.
    /// A persisted policy overrides the code-level role and permission requirements;
    /// otherwise the descriptor's declared requirements are evaluated.
    /// </summary>
    /// <param name="descriptor">The registered function descriptor being authorized.</param>
    /// <param name="policy">The optional persisted policy overriding code defaults.</param>
    /// <returns>
    /// A localized denial reason, or <see langword="null"/> when the current administrator may proceed.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Propagated if the work-context or permission service cannot evaluate the current administrator.
    /// </exception>
    public async Task<string> GetDenialReasonAsync(
        NeuCharFunctionDescriptor descriptor,
        NeuCharFunctionProvitAccess policy = null)
    {
        var context = _workContextProvider.GetAdminWorkContext();
        if (context == null || context.AdminUserId <= 0)
        {
            return "请先登录后台管理员账号。";
        }

        if (policy is { AccessMode: not NeuCharFunctionProvitAccess.AccessModeInherit })
        {
            return policy.AccessMode switch
            {
                NeuCharFunctionProvitAccess.AccessModeOpen => null,
                NeuCharFunctionProvitAccess.AccessModeDeny => "该 Function 的全局访问已被管理员策略禁用。",
                NeuCharFunctionProvitAccess.AccessModeRestricted =>
                    await GetRestrictedDenialReasonAsync(policy, context).ConfigureAwait(false),
                _ => null
            };
        }

        var requiredRoles = descriptor.GlobalPivotRoleCodes ?? Array.Empty<string>();
        var requiredPermissions = descriptor.GlobalPivotPermissionCodes ?? Array.Empty<string>();
        if (requiredRoles.Count == 0 && requiredPermissions.Count == 0)
        {
            return null;
        }

        if (RoleMatches(context, requiredRoles))
        {
            return null;
        }

        if (requiredPermissions.Count > 0 &&
            await _checkPermission.HasPermissionAsync(
                requiredPermissions.ToArray(),
                context.AdminUserId).ConfigureAwait(false))
        {
            return null;
        }

        return "当前账号没有访问该全局 Function 的角色或权限。";
    }

    /// <summary>
    /// Evaluates the user, role, and permission bindings of a restricted persisted policy.
    /// </summary>
    /// <param name="policy">The restricted policy to evaluate.</param>
    /// <param name="context">The current administrator work context.</param>
    /// <returns>
    /// A localized denial reason, or <see langword="null"/> when one binding grants access.
    /// </returns>
    private async Task<string> GetRestrictedDenialReasonAsync(
        NeuCharFunctionProvitAccess policy,
        AdminWorkContext context)
    {
        if (policy.MatchesAdminUser(context.AdminUserId))
        {
            return null;
        }

        var requiredRoles = policy.RoleCodeList;
        if (requiredRoles.Count > 0 && RoleMatches(context, requiredRoles))
        {
            return null;
        }

        var requiredPermissions = policy.PermissionCodeList;
        if (requiredPermissions.Count > 0 &&
            await _checkPermission.HasPermissionAsync(
                requiredPermissions.ToArray(),
                context.AdminUserId).ConfigureAwait(false))
        {
            return null;
        }

        return "当前账号未包含在该 Function 全局访问策略绑定的用户、角色或权限中。";
    }

    /// <summary>
    /// Resolves whether a function is exposed to global Pivot access after applying
    /// a persisted policy over the code-level default.
    /// </summary>
    /// <param name="codeAllowGlobalPivot">The exposure declaration from the function descriptor.</param>
    /// <param name="policy">The optional persisted access policy.</param>
    /// <returns><see langword="true"/> when the function is exposed under the combined policy.</returns>
    public static bool ResolveExposureAllowed(
        bool codeAllowGlobalPivot,
        NeuCharFunctionProvitAccess policy)
    {
        if (policy == null || policy.AccessMode == NeuCharFunctionProvitAccess.AccessModeInherit)
        {
            return codeAllowGlobalPivot;
        }
        return policy.AccessMode switch
        {
            NeuCharFunctionProvitAccess.AccessModeOpen => true,
            NeuCharFunctionProvitAccess.AccessModeRestricted => true,
            NeuCharFunctionProvitAccess.AccessModeDeny => false,
            _ => codeAllowGlobalPivot
        };
    }

    /// <summary>
    /// Determines whether the work context contains at least one required role.
    /// Comparison is case-insensitive and ignores blank required-role values.
    /// </summary>
    /// <param name="context">The administrator context whose role codes are checked.</param>
    /// <param name="requiredRoles">The role codes required by the function or policy.</param>
    /// <returns><see langword="true"/> when at least one required role is present.</returns>
    public static bool RoleMatches(
        AdminWorkContext context,
        IEnumerable<string> requiredRoles)
    {
        var required = requiredRoles?
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (required.Count == 0)
        {
            return false;
        }

        return (context?.RoleCodes ?? Array.Empty<string>())
            .Any(role => required.Contains(role));
    }
}

    /// <summary>
    /// Resolves a globally callable function after validating its module registration,
    /// exposure policy, and current administrator authorization.
    /// </summary>
public sealed class NeuCharPivotGlobalFunctionService
{
    private readonly NeuCharFunctionService _functionService;
    private readonly NeuCharPivotGlobalAccessService _accessService;
    private readonly NeuCharFunctionProvitAccessService _accessPolicyService;

    public NeuCharPivotGlobalFunctionService(
        NeuCharFunctionService functionService,
        NeuCharPivotGlobalAccessService accessService,
        NeuCharFunctionProvitAccessService accessPolicyService)
    {
        _functionService = functionService;
        _accessService = accessService;
        _accessPolicyService = accessPolicyService;
    }

    /// <param name="moduleUid">The registered module UID containing the function.</param>
    /// <param name="functionKey">The function key, method name, or display name to resolve.</param>
    /// <param name="cancellationToken">The token used to cancel catalog loading.</param>
    /// <returns>A resolution containing the descriptor on success or a denial message on failure.</returns>
    public async Task<NeuCharGlobalFunctionResolution> ResolveAsync(
        string moduleUid,
        string functionKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(moduleUid) || string.IsNullOrWhiteSpace(functionKey))
        {
            return Denied("模块 UID 和 Function Key 不能为空。");
        }

        var descriptor = (await _functionService.GetCatalogAsync(
                moduleUid,
                true,
                cancellationToken)
            .ConfigureAwait(false))
            .FirstOrDefault(item => MatchesFunctionKey(item, functionKey));

        if (descriptor == null)
        {
            return Denied("Function 不存在、未加载或已在模块更新后移除。");
        }

        //Legacy comment translated to English.
        var policy = _accessPolicyService.GetPolicy(
            descriptor.ModuleUid,
            descriptor.FunctionKey);
        if (!NeuCharPivotGlobalAccessService.ResolveExposureAllowed(
                descriptor.AllowGlobalPivot,
                policy))
        {
            return Denied(policy is { AccessMode: NeuCharFunctionProvitAccess.AccessModeDeny }
                ? "该 Function 的全局访问已被管理员策略禁用。"
                : "该 Function 未声明为全局 NeuCharPivot 映射。");
        }

        var accessError = await _accessService.GetDenialReasonAsync(descriptor, policy).ConfigureAwait(false);
        return accessError == null
            ? new NeuCharGlobalFunctionResolution(descriptor, null)
            : Denied(accessError);
    }

    /// <summary>
    /// Resolves and executes a globally callable function using the supplied JSON parameters.
    /// Execution is skipped when resolution or authorization fails.
    /// </summary>
    /// <param name="moduleUid">The registered module UID containing the function.</param>
    /// <param name="functionKey">The function key, method name, or display name to execute.</param>
    /// <param name="parametersJson">The JSON object passed to the resolved function.</param>
    /// <param name="cancellationToken">The token used to cancel resolution or execution.</param>
    /// <returns>The function execution result, including any resolution or execution error.</returns>
    public async Task<NeuCharFunctionExecutionResult> ExecuteAsync(
        string moduleUid,
        string functionKey,
        string parametersJson,
        CancellationToken cancellationToken = default)
    {
        var resolution = await ResolveAsync(moduleUid, functionKey, cancellationToken)
            .ConfigureAwait(false);
        if (!resolution.Success)
        {
            return new NeuCharFunctionExecutionResult(
                false,
                null,
                resolution.ErrorMessage,
                null);
        }

        return await _functionService.ExecuteAsync(
                resolution.Descriptor.ModuleUid,
                resolution.Descriptor.FunctionKey,
                parametersJson,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static NeuCharGlobalFunctionResolution Denied(string message) =>
        new(null, message);

    /// <summary>
    /// Determines whether a descriptor matches a requested key, method name, or display name.
    /// </summary>
    /// <param name="descriptor">The descriptor to inspect.</param>
    /// <param name="functionKey">The requested function identifier.</param>
    /// <returns><see langword="true"/> when any supported descriptor identifier matches.</returns>
    public static bool MatchesFunctionKey(
        NeuCharFunctionDescriptor descriptor,
        string functionKey)
    {
        return descriptor != null &&
            !string.IsNullOrWhiteSpace(functionKey) &&
            (string.Equals(descriptor.FunctionKey, functionKey, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(descriptor.MethodName, functionKey, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(descriptor.Name, functionKey, StringComparison.OrdinalIgnoreCase));
    }
}
