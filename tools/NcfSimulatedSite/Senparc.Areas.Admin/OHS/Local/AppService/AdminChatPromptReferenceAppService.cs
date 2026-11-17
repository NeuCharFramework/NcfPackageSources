/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatPromptReferenceAppService.cs
    File description: Related implementation.

    Created by: Senparc - 20261104

    Modified by: Senparc - 20261105
    Change description: v0.12.26 Updated module behavior and related support.

----------------------------------------------------------------*/
using System;
using System.Threading.Tasks;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Xncf.PromptRange.Abstractions.References;

namespace Senparc.Areas.Admin.OHS.Local.AppService;

[AdminOrJwtAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
public sealed class AdminChatPromptReferenceAppService : LocalAppServiceBase
{
    public AdminChatPromptReferenceAppService(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<PromptReferenceInfo>> ScoreWithAIAsync(AdminChatPromptScoreRequest request)
    {
        return this.GetResponseAsync<PromptReferenceInfo>(async (response, logger) =>
        {
            return await GetService<AdminChatMessageService>().ScorePromptReferenceWithAIAsync(
                GetCurrentAdminUserInfoId(), request.MessageId, request.PromptItemId);
        });
    }
}

public sealed class AdminChatPromptScoreRequest
{
    public int MessageId { get; set; }
    public int PromptItemId { get; set; }
}
