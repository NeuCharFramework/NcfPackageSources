/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SkillAppService.cs
    文件功能描述：Skill 查询、执行与测试应用服务
    
    
    创建标识：Senparc - 20261016
    
    修改标识：Senparc - 20261021
    修改描述：v0.20.0 开放 Skill 查询、执行与测试

----------------------------------------------------------------*/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.PromptRange.Abstractions.Skills;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService;

[ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
public sealed class SkillAppService : AppServiceBase
{
    private readonly ISkillCatalog _catalog;
    private readonly ISkillExecutor _executor;
    private readonly ISkillPackageService _packageService;
    private readonly ISkillTestRunner _testRunner;
    private readonly AIModelService _aiModelService;

    public SkillAppService(
        System.IServiceProvider serviceProvider,
        ISkillCatalog catalog,
        ISkillExecutor executor,
        ISkillPackageService packageService,
        ISkillTestRunner testRunner,
        AIModelService aiModelService)
        : base(serviceProvider)
    {
        _catalog = catalog;
        _executor = executor;
        _packageService = packageService;
        _testRunner = testRunner;
        _aiModelService = aiModelService;
    }

    [ApiBind]
    public async Task<AppResponseBase<IReadOnlyList<SkillDescriptor>>> ListAsync(
        string keyword = null,
        bool publishedOnly = true)
    {
        return await this.GetResponseAsync<IReadOnlyList<SkillDescriptor>>(
            async (response, logger) =>
                await _catalog.ListAsync(new SkillQuery(Keyword: keyword, PublishedOnly: publishedOnly)));
    }

    [ApiBind]
    public async Task<AppResponseBase<SkillDescriptor>> GetAsync(
        string skillId,
        string version = null)
    {
        return await this.GetResponseAsync<SkillDescriptor>(
            async (response, logger) => await _catalog.GetAsync(skillId, version));
    }

    [ApiBind]
    public async Task<AppResponseBase<IReadOnlyList<AIModelDto>>> ListChatModelsAsync()
    {
        return await this.GetResponseAsync<IReadOnlyList<AIModelDto>>(
            async (response, logger) =>
            {
                var models = await _aiModelService.GetFullListAsync(
                    item => item.ConfigModelType == ConfigModelType.Chat,
                    item => item.Id,
                    Senparc.Ncf.Core.Enums.OrderingType.Descending);
                return models
                    .Select(model => _aiModelService.Mapping<AIModelDto>(model))
                    .ToList();
            });
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<SkillExecutionResult>> ExecuteAsync(
        string skillId,
        string version,
        string input)
    {
        return await this.GetResponseAsync<SkillExecutionResult>(
            async (response, logger) => await _executor.ExecuteAsync(new SkillExecutionRequest(
                skillId,
                version,
                input,
                new SkillExecutionContext("PromptRange", Mode: SkillExecutionMode.Evaluation))));
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<SkillTestResult>> TestAsync(
        SkillTestRequest request)
    {
        return await this.GetResponseAsync<SkillTestResult>(
            async (response, logger) => await _testRunner.TestAsync(request));
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public async Task<FileContentResult> ExportAsync(string skillId, string version = null)
    {
        await using var stream = await _packageService.ExportAsync(skillId, version);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return new FileContentResult(buffer.ToArray(), "application/zip")
        {
            FileDownloadName = $"{skillId}-{version ?? "current"}.skill.zip"
        };
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<SkillInstallResult>> InstallAsync(IFormFile package)
    {
        return await this.GetResponseAsync<SkillInstallResult>(
            async (response, logger) =>
            {
                if (package == null)
                {
                    throw new System.ArgumentNullException(nameof(package));
                }

                await using var stream = package.OpenReadStream();
                return await _packageService.InstallAsync(stream, package.FileName);
            });
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<SkillInstallResult>> InstallFromUrlAsync(string packageUrl)
    {
        return await this.GetResponseAsync<SkillInstallResult>(
            async (response, logger) =>
                await _packageService.InstallFromUrlAsync(packageUrl));
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public async Task<AppResponseBase<SkillContentPreview>> PreviewAsync(
        string skillId,
        string version = null)
    {
        return await this.GetResponseAsync<SkillContentPreview>(
            async (response, logger) =>
                await _packageService.PreviewAsync(skillId, version));
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<SkillGenerationResult>> GenerateAsync(
        SkillGenerationRequest request)
    {
        return await this.GetResponseAsync<SkillGenerationResult>(
            async (response, logger) =>
                await _packageService.GenerateAsync(request));
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<SkillDescriptor>> PublishAsync(
        string skillId,
        string version)
    {
        return await this.GetResponseAsync<SkillDescriptor>(
            async (response, logger) =>
                await _packageService.PublishAsync(skillId, version));
    }
}
