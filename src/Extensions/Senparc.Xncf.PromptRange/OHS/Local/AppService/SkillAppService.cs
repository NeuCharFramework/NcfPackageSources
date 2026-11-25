/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SkillAppService.cs
    File description: Related implementation.

    Created by: Senparc - 20261016

    Modified by: Senparc - 20261021
    Change description: v0.20.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.20.8 add Skill management page and functionality

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

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

    /// <summary>
    /// Initializes the authenticated Skill catalog, executor, test runner, package service,
    /// and Chat model selector.
    /// </summary>
    /// <param name="serviceProvider">Dependency-injection provider.</param>
    /// <param name="catalog">Skill catalog service.</param>
    /// <param name="executor">Skill execution service.</param>
    /// <param name="packageService">Skill package/import/export service.</param>
    /// <param name="testRunner">Skill isolation test runner.</param>
    /// <param name="aiModelService">AI model service used for generation model selection.</param>
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

    /// <summary>Lists catalogued Skills using keyword and publication filters.</summary>
    /// <param name="keyword">Optional Skill ID/name keyword.</param>
    /// <param name="publishedOnly">Whether draft versions are excluded.</param>
    /// <returns>A task containing the catalog response.</returns>
    [ApiBind]
    public async Task<AppResponseBase<IReadOnlyList<SkillDescriptor>>> ListAsync(
        string keyword = null,
        bool publishedOnly = true)
    {
        return await this.GetResponseAsync<IReadOnlyList<SkillDescriptor>>(
            async (response, logger) =>
                await _catalog.ListAsync(new SkillQuery(Keyword: keyword, PublishedOnly: publishedOnly)));
    }

    /// <summary>Resolves one Skill descriptor and optional version.</summary>
    /// <param name="skillId">The Skill identifier.</param>
    /// <param name="version">Optional version selector.</param>
    /// <returns>A task containing the descriptor response.</returns>
    [ApiBind]
    public async Task<AppResponseBase<SkillDescriptor>> GetAsync(
        string skillId,
        string version = null)
    {
        return await this.GetResponseAsync<SkillDescriptor>(
            async (response, logger) => await _catalog.GetAsync(skillId, version));
    }

    /// <summary>Lists configured Chat models available for Skill generation.</summary>
    /// <returns>A task containing Chat model DTOs.</returns>
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

    /// <summary>Executes a Skill in PromptRange evaluation mode.</summary>
    /// <param name="skillId">The Skill identifier.</param>
    /// <param name="version">The optional version selector.</param>
    /// <param name="input">The Skill input text.</param>
    /// <returns>A task containing execution output and status.</returns>
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

    /// <summary>Runs an isolated Skill test request.</summary>
    /// <param name="request">The Skill test and isolation request.</param>
    /// <returns>A task containing test output, isolation state, and diagnostics.</returns>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<SkillTestResult>> TestAsync(
        SkillTestRequest request)
    {
        return await this.GetResponseAsync<SkillTestResult>(
            async (response, logger) => await _testRunner.TestAsync(request));
    }

    /// <summary>Exports a Skill version as a downloadable package.</summary>
    /// <param name="skillId">The Skill identifier.</param>
    /// <param name="version">Optional version selector.</param>
    /// <returns>A ZIP file result.</returns>
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

    /// <summary>Installs a Skill package uploaded as a form file.</summary>
    /// <param name="package">The uploaded Skill package.</param>
    /// <returns>A task containing installation status and descriptor.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="package"/> is missing.</exception>
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

    /// <summary>Downloads and installs a Skill package from a URL.</summary>
    /// <param name="packageUrl">The package URL.</param>
    /// <returns>A task containing installation status and descriptor.</returns>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<SkillInstallResult>> InstallFromUrlAsync(string packageUrl)
    {
        return await this.GetResponseAsync<SkillInstallResult>(
            async (response, logger) =>
                await _packageService.InstallFromUrlAsync(packageUrl));
    }

    /// <summary>Returns a bounded preview of a Skill package's manifest and files.</summary>
    /// <param name="skillId">The Skill identifier.</param>
    /// <param name="version">Optional version selector.</param>
    /// <returns>A task containing preview metadata.</returns>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public async Task<AppResponseBase<SkillContentPreview>> PreviewAsync(
        string skillId,
        string version = null)
    {
        return await this.GetResponseAsync<SkillContentPreview>(
            async (response, logger) =>
                await _packageService.PreviewAsync(skillId, version));
    }

    /// <summary>Generates or improves a Skill definition with the selected AI model.</summary>
    /// <param name="request">The Skill generation request.</param>
    /// <returns>A task containing generated content and approval state.</returns>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<SkillGenerationResult>> GenerateAsync(
        SkillGenerationRequest request)
    {
        return await this.GetResponseAsync<SkillGenerationResult>(
            async (response, logger) =>
                await _packageService.GenerateAsync(request));
    }

    /// <summary>Publishes a selected Skill version for production use.</summary>
    /// <param name="skillId">The Skill identifier.</param>
    /// <param name="version">The version to publish.</param>
    /// <returns>A task containing the published descriptor.</returns>
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
