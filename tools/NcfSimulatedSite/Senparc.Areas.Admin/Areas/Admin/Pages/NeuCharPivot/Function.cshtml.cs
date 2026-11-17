/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Function.cshtml.cs
    File description: Related implementation.

    Created by: Senparc - 20260829

    Modified by: Senparc - 20260829
    Change description: v0.7.0 Updated module behavior and related support.

    Modified by: Senparc - 20260917
    Change description: v0.9.1 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Authorization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages.NeuCharPivot;

[IgnoreAuth]
[AdminAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
public class FunctionModel(
    IServiceProvider serviceProvider,
    NeuCharPivotGlobalFunctionService globalFunctionService,
    NeuCharFunctionProvitAccessService accessPolicyService) : BaseAdminPageModel(serviceProvider)
{
    private readonly NeuCharPivotGlobalFunctionService _globalFunctionService = globalFunctionService;
    private readonly NeuCharFunctionProvitAccessService _accessPolicyService = accessPolicyService;

    public async Task<IActionResult> OnGetDescribeAsync(
        [FromQuery] string moduleUid,
        [FromQuery] string functionKey)
    {
        var resolution = await _globalFunctionService.ResolveAsync(
                moduleUid,
                functionKey,
                HttpContext.RequestAborted)
            .ConfigureAwait(false);
        if (!resolution.Success)
        {
            return StatusCode(403, resolution.ErrorMessage);
        }

        var descriptor = resolution.Descriptor;

        //Legacy comment translated to English.
        object policyDto = null;
        var policy = _accessPolicyService.GetPolicy(
            descriptor.ModuleUid,
            descriptor.FunctionKey);
        if (policy != null &&
            policy.AccessMode != Senparc.Areas.Admin.Domain.Models.DatabaseModel.NeuCharFunctionProvitAccess.AccessModeInherit)
        {
            policyDto = new
            {
                policy.AccessMode,
                roleCodes = policy.RoleCodeList,
                permissionCodes = policy.PermissionCodeList,
                userIds = policy.AdminUserIdList,
                policy.Remark
            };
        }

        return Ok(new
        {
            descriptor.ModuleUid,
            descriptor.ModuleName,
            descriptor.ModuleVersion,
            descriptor.FunctionKey,
            descriptor.Name,
            descriptor.Description,
            parameters = descriptor.Parameters,
            access = new
            {
                enabled = descriptor.AllowGlobalPivot,
                roleCodes = descriptor.GlobalPivotRoleCodes,
                permissionCodes = descriptor.GlobalPivotPermissionCodes,
                policy = policyDto
            }
        });
    }

    public async Task<IActionResult> OnPostRunAsync([FromBody] GlobalFunctionRunRequest request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.ModuleUid) ||
            string.IsNullOrWhiteSpace(request.FunctionKey))
        {
            return BadRequest("全局 Function 请求无效。");
        }

        if (request.ParametersJson?.Length > 1_000_000)
        {
            return BadRequest("Function 参数不能超过 1000000 个字符。");
        }

        var result = await _globalFunctionService.ExecuteAsync(
                request.ModuleUid,
                request.FunctionKey,
                request.ParametersJson,
                HttpContext.RequestAborted)
            .ConfigureAwait(false);
        return Ok(result);
    }

    public sealed class GlobalFunctionRunRequest
    {
        public string ModuleUid { get; set; }
        public string FunctionKey { get; set; }
        public string ParametersJson { get; set; } = "{}";
    }
}
