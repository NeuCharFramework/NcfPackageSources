using System.Collections.Generic;
using System.IO;
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

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService;

[ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
public sealed class SkillAppService : AppServiceBase
{
    private readonly ISkillCatalog _catalog;
    private readonly ISkillExecutor _executor;
    private readonly ISkillPackageService _packageService;

    public SkillAppService(
        System.IServiceProvider serviceProvider,
        ISkillCatalog catalog,
        ISkillExecutor executor,
        ISkillPackageService packageService)
        : base(serviceProvider)
    {
        _catalog = catalog;
        _executor = executor;
        _packageService = packageService;
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
