/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptRangeSkillService.cs
    文件功能描述：PromptRange Skill 目录与执行服务
    
    
    创建标识：Senparc - 20261016
    
    修改标识：Senparc - 20261021
    修改描述：v0.20.0 按策略提供 Skill 目录与执行

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Abstractions.Skills;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

namespace Senparc.Xncf.PromptRange.Domain.Services;

/// <summary>
/// Exposes the existing PromptRange tree as a portable Skill catalog and runtime.
/// A PromptRange is the Skill identity and PromptCode is its immutable executable version.
/// </summary>
public sealed class PromptRangeSkillService : ISkillCatalog, ISkillExecutor, ISkillPackageService
{
    private readonly PromptRangeService _promptRangeService;
    private readonly PromptItemService _promptItemService;
    private readonly PromptResultService _promptResultService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AIModelService _aiModelService;

    public PromptRangeSkillService(
        PromptRangeService promptRangeService,
        PromptItemService promptItemService,
        PromptResultService promptResultService,
        IHttpClientFactory httpClientFactory,
        AIModelService aiModelService)
    {
        _promptRangeService = promptRangeService;
        _promptItemService = promptItemService;
        _promptResultService = promptResultService;
        _httpClientFactory = httpClientFactory;
        _aiModelService = aiModelService;
    }

    public async Task<IReadOnlyList<SkillDescriptor>> ListAsync(
        SkillQuery query = null,
        CancellationToken cancellationToken = default)
    {
        query ??= new SkillQuery();
        var ranges = await _promptRangeService.GetListAsync();
        var result = new List<SkillDescriptor>();

        foreach (var range in ranges)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var skillId = GetSkillId(range);
            if (range.Kind != SkillKind.Skill ||
                query.AllowedSkillIds != null &&
                !query.AllowedSkillIds.Contains(skillId, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }
            if (!Matches(query, skillId, range.Alias, range.RangeName))
            {
                continue;
            }

            var items = await GetItemsAsync(range.Id, cancellationToken);
            var versions = items
                .Where(item => !query.PublishedOnly || !item.IsDraft)
                .OrderBy(item => item.Tactic, StringComparer.Ordinal)
                .ThenBy(item => item.Aiming)
                .Select(item => ToVersionDescriptor(skillId, range, item))
                .ToList();

            if (versions.Count == 0)
            {
                continue;
            }

            var current = SelectBestItem(items, query.PublishedOnly);
            result.Add(new SkillDescriptor(
                skillId,
                skillId,
                GetRangeName(range),
                $"PromptRange Skill: {GetRangeName(range)}",
                current?.IsDraft == true ? SkillStatus.Draft : SkillStatus.Published,
                SkillSourceType.PromptRange,
                current?.FullVersion,
                versions,
                Array.Empty<SkillToolDefinition>(),
                SkillKind.Skill,
                new SkillExecutionPolicy()));
        }

        return result;
    }

    public async Task<SkillDescriptor> GetAsync(
        string skillId,
        string version = null,
        SkillExecutionContext context = null,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveItemAsync(skillId, version, context, cancellationToken);
        var range = await _promptRangeService.GetAsync(resolved.RangeId);
        if (range.Kind != SkillKind.Skill ||
            context?.CallerModule != "PromptRange" &&
            context?.CallerModule != "AdminChat" &&
            context?.CallerModule != "AgentsManager")
        {
            throw new SkillNotFoundException(skillId, version);
        }
        var items = await GetItemsAsync(resolved.RangeId, cancellationToken);
        var descriptorVersions = items
            .Where(item => !item.IsDraft || context?.Mode is SkillExecutionMode.Evaluation or SkillExecutionMode.Preview)
            .Select(item => ToVersionDescriptor(skillId, range, item))
            .ToList();

        return new SkillDescriptor(
            skillId,
            skillId,
            GetRangeName(range),
            $"PromptRange Skill: {GetRangeName(range)}",
            resolved.IsDraft ? SkillStatus.Draft : SkillStatus.Published,
            SkillSourceType.PromptRange,
            resolved.FullVersion,
            descriptorVersions,
            Array.Empty<SkillToolDefinition>(),
            SkillKind.Skill,
            new SkillExecutionPolicy());
    }

    public async Task<SkillExecutionResult> ExecuteAsync(
        SkillExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.SkillId))
        {
            throw new NcfExceptionBase("SkillId 不能为空");
        }

        var context = request.Context ?? new SkillExecutionContext("Unknown");
        if (context.Policy?.RiskLevel > SkillRiskLevel.ReadOnly &&
            context.Policy.TestIsolation == SkillTestIsolation.PromptOnly)
        {
            throw new NcfExceptionBase(
                "具有文件、网络或系统副作用的 Skill 不能在 PromptOnly 执行，必须通过 Workspace 或 Sandbox 隔离适配器。");
        }
        var item = await ResolveItemAsync(request.SkillId, request.Version, context, cancellationToken);
        var itemDto = await _promptItemService.GetAsync(item.Id);
        ApplyArguments(itemDto, context.Arguments, request.Input);

        cancellationToken.ThrowIfCancellationRequested();
        var result = await _promptResultService.SenparcGenerateResultAsync(
            itemDto,
            request.Input,
            null,
            null,
            new PromptExecutionOptions
            {
                PersistResult = request.PersistResult
            });

        return new SkillExecutionResult(
            request.SkillId,
            item.FullVersion,
            item.FullVersion,
            result.ResultString,
            true,
            null,
            result.Id);
    }

    public async Task<Stream> ExportAsync(
        string skillId,
        string version = null,
        CancellationToken cancellationToken = default)
    {
        var item = await ResolveItemAsync(
            skillId,
            version,
            new SkillExecutionContext("PromptRange", Mode: SkillExecutionMode.Preview),
            cancellationToken);
        var range = await _promptRangeService.GetAsync(item.RangeId);
        var sourceDirectory = await _promptItemService.ExportPluginWithItemAsync(item);
        var output = new MemoryStream();

        try
        {
            using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
            {
                var manifest = new SkillPackageManifest(
                    SkillId: skillId,
                    SkillCode: skillId,
                    Name: GetRangeName(range),
                    Description: $"PromptRange Skill: {GetRangeName(range)}",
                    Version: item.FullVersion,
                    PromptCode: item.FullVersion,
                    ExportedAt: DateTimeOffset.UtcNow);
                WriteEntry(archive, "skill.json", JsonSerializer.Serialize(manifest, JsonOptions));

                foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var relativePath = Path.GetRelativePath(sourceDirectory, file)
                        .Replace(Path.DirectorySeparatorChar, '/');
                    var packageRoot = SanitizePackageComponent(skillId);
                    var entry = archive.CreateEntry($"{packageRoot}/{relativePath}", CompressionLevel.Fastest);
                    await using var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                    await using var entryStream = entry.Open();
                    await input.CopyToAsync(entryStream, cancellationToken);
                }

            }

            output.Position = 0;
            return output;
        }
        catch
        {
            await output.DisposeAsync();
            throw;
        }
    }

    public async Task<SkillInstallResult> InstallAsync(
        Stream package,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (package == null || !package.CanRead)
        {
            throw new NcfExceptionBase("Skill 包内容不可读");
        }

        if (package.CanSeek && package.Length > 50L * 1024 * 1024)
        {
            throw new NcfExceptionBase("Skill 包大小超过限制（50 MB）");
        }

        await using var buffer = new MemoryStream();
        await package.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length > 50L * 1024 * 1024)
        {
            throw new NcfExceptionBase("Skill 包大小超过限制（50 MB）");
        }

        buffer.Position = 0;
        SkillPackageManifest manifest;
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Read, true))
        {
            var manifestEntry = archive.GetEntry("skill.json")
                ?? throw new NcfExceptionBase("Skill 包缺少 skill.json");
            await using var manifestStream = manifestEntry.Open();
            manifest = await JsonSerializer.DeserializeAsync<SkillPackageManifest>(
                manifestStream,
                JsonOptions,
                cancellationToken)
                ?? throw new NcfExceptionBase("Skill manifest 无效");

            if (string.IsNullOrWhiteSpace(manifest.SkillId) ||
                string.IsNullOrWhiteSpace(manifest.Version))
            {
                throw new NcfExceptionBase("Skill manifest 必须包含 SkillId 和 Version");
            }

            var packageRoot = SanitizePackageComponent(manifest.SkillId) + "/";
            foreach (var entry in archive.Entries)
            {
                if (!string.Equals(entry.FullName, "skill.json", StringComparison.OrdinalIgnoreCase) &&
                    !entry.FullName.StartsWith(packageRoot, StringComparison.OrdinalIgnoreCase))
                {
                    throw new NcfExceptionBase("Skill 包目录与 manifest.SkillId 不一致");
                }

                if (entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                    entry.FullName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                    entry.FullName.EndsWith(".sh", StringComparison.OrdinalIgnoreCase) ||
                    entry.FullName.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) ||
                    entry.FullName.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) ||
                    entry.FullName.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
                {
                    throw new NcfExceptionBase("Skill 包不允许包含可执行文件或脚本");
                }
            }
        }

        var existingItems = await _promptRangeService.GetListAsync();
        var existingRange = existingItems.FirstOrDefault(range =>
            string.Equals(GetSkillId(range), manifest.SkillId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(range.Alias, manifest.SkillId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(range.RangeName, manifest.SkillId, StringComparison.OrdinalIgnoreCase));
        if (existingRange != null &&
            (await GetItemsAsync(existingRange.Id, cancellationToken))
                .Any(item => string.Equals(item.FullVersion, manifest.Version, StringComparison.OrdinalIgnoreCase)))
        {
            throw new NcfExceptionBase($"Skill {manifest.SkillId}@{manifest.Version} 已存在");
        }

        buffer.Position = 0;
        var formFile = new FormFile(
            buffer,
            0,
            buffer.Length,
            "skillPackage",
            Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? "skill.zip" : fileName));
        await _promptItemService.UploadPluginsAsync(formFile);

        var installedRange = (await _promptRangeService.GetListAsync())
            .FirstOrDefault(range =>
                string.Equals(GetSkillId(range), manifest.SkillId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(range.Alias, manifest.SkillId, StringComparison.OrdinalIgnoreCase));
        if (installedRange != null)
        {
            await _promptRangeService.ChangeKindAsync(installedRange.Id, SkillKind.Skill);
        }

        SkillDescriptor skill;
        try
        {
            skill = await GetAsync(
                manifest.SkillId,
                manifest.Version,
                new SkillExecutionContext("PromptRange", Mode: SkillExecutionMode.Preview),
                cancellationToken);
        }
        catch (SkillNotFoundException) when (
            string.Equals(manifest.Version, "external", StringComparison.OrdinalIgnoreCase))
        {
            skill = await GetAsync(
                manifest.SkillId,
                null,
                new SkillExecutionContext("PromptRange", Mode: SkillExecutionMode.Preview),
                cancellationToken);
        }
        return new SkillInstallResult(
            skill,
            true,
            "Skill 已安装为 Draft，审核并发布后才能被 AdminChat 或 AgentsManager 执行。");
    }

    public async Task<SkillInstallResult> InstallFromUrlAsync(
        string packageUrl,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(packageUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new NcfExceptionBase("Skill 安装地址必须使用 HTTPS");
        }

        if (TryParseGitHubFolder(packageUrl, out var repository, out var branch, out var folder))
        {
            await using var githubPackage = await DownloadGitHubFolderAsync(
                repository,
                branch,
                folder,
                cancellationToken);
            return await InstallAsync(githubPackage, $"{Path.GetFileName(folder)}.skill.zip", cancellationToken);
        }

        var client = _httpClientFactory.CreateClient("PromptRangeSkillPackage");
        if (client.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("NeuCharFramework-SkillImporter/1.0");
        }
        using var response = await client.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > 50L * 1024 * 1024)
        {
            throw new NcfExceptionBase("远程 Skill 包大小超过限制（50 MB）");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await InstallAsync(stream, Path.GetFileName(uri.LocalPath), cancellationToken);
    }

    public async Task<SkillContentPreview> PreviewAsync(
        string skillId,
        string version = null,
        CancellationToken cancellationToken = default)
    {
        await using var package = await ExportAsync(skillId, version, cancellationToken);
        using var archive = new ZipArchive(package, ZipArchiveMode.Read, leaveOpen: false);
        var files = new List<SkillContentFile>();
        foreach (var entry in archive.Entries
                     .Where(entry => !string.IsNullOrEmpty(entry.Name))
                     .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var isPreviewable = IsPreviewable(entry.FullName);
            string content = null;
            if (isPreviewable && entry.Length <= 512 * 1024)
            {
                using var reader = new StreamReader(entry.Open());
                content = await reader.ReadToEndAsync(cancellationToken);
            }

            files.Add(new SkillContentFile(
                entry.FullName,
                GetContentType(entry.FullName),
                entry.Length,
                isPreviewable,
                content));
        }

        var manifest = files.FirstOrDefault(file =>
            string.Equals(file.Path, "skill.json", StringComparison.OrdinalIgnoreCase));
        return new SkillContentPreview(
            skillId,
            version,
            manifest?.Content,
            files);
    }

    public async Task<SkillGenerationResult> GenerateAsync(
        SkillGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Description))
        {
            throw new NcfExceptionBase("Skill 需求描述不能为空");
        }

        var model = await _aiModelService.GetObjectAsync(
            item => item.Id == request.ModelId &&
                    item.ConfigModelType == ConfigModelType.Chat);
        if (model == null)
        {
            throw new NcfExceptionBase("请选择有效的 Chat 类型 AIKernel 模型");
        }

        var modelDto = _aiModelService.Mapping<Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto.AIModelDto>(model);
        var existingContent = string.Empty;
        if (!string.IsNullOrWhiteSpace(request.ExistingSkillId))
        {
            var existingItem = await ResolveItemAsync(
                request.ExistingSkillId,
                request.ExistingVersion,
                new SkillExecutionContext("PromptRange", Mode: SkillExecutionMode.Preview),
                cancellationToken);
            existingContent = (await _promptItemService.GetAsync(existingItem.Id)).Content;
        }
        var generated = await _promptResultService.SenparcGenerateResultAsync(
            new PromptItemDto
            {
                Id = 0,
                ModelId = model.Id,
                AIModelDto = modelDto,
                Content = BuildGenerationPrompt(request, existingContent),
                MaxToken = 4000,
                Temperature = 0.3f,
                TopP = 0.8f,
                FullVersion = "Skill.Generation"
            },
            executionOptions: new PromptExecutionOptions { PersistResult = false });

        var definition = ParseGeneratedSkill(generated.ResultString, request);
        var range = await _promptRangeService.AddAsync(definition.SkillId);
        await _promptRangeService.ChangeKindAsync(range.Id, SkillKind.Skill);
        var item = await _promptItemService.AddPromptItemAsync(new PromptItem_AddRequest
        {
            RangeId = range.Id,
            ModelId = request.ModelId,
            Content = definition.Content,
            Note = definition.Description,
            IsDraft = true,
            IsNewTactic = false,
            IsNewSubTactic = false,
            IsTopTactic = false,
            IsNewAiming = false,
            NumsOfResults = 1,
            MaxToken = 4000,
            Temperature = 0.3f,
            TopP = 0.8f
        });
        var skill = await GetAsync(
            GetSkillId(range),
            item.FullVersion,
            new SkillExecutionContext("PromptRange", Mode: SkillExecutionMode.Preview),
            cancellationToken);
        return new SkillGenerationResult(
            skill,
            definition.Content,
            true,
            "Skill 已由 AI 生成并保存为 Draft，请预览、编辑和测试后再发布。");
    }

    public async Task<SkillDescriptor> PublishAsync(
        string skillId,
        string version,
        CancellationToken cancellationToken = default)
    {
        var item = await ResolveItemAsync(
            skillId,
            version,
            new SkillExecutionContext("PromptRange", Mode: SkillExecutionMode.Preview),
            cancellationToken);
        await _promptItemService.DraftSwitch(item.Id, false);
        return await GetAsync(
            skillId,
            version,
            new SkillExecutionContext("PromptRange", Mode: SkillExecutionMode.Production),
            cancellationToken);
    }

    private async Task<PromptItem> ResolveItemAsync(
        string skillId,
        string version,
        SkillExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            throw new SkillNotFoundException(skillId, version);
        }

        var ranges = await _promptRangeService.GetListAsync();
        var range = ranges.FirstOrDefault(item =>
            string.Equals(GetSkillId(item), skillId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.Alias, skillId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.RangeName, skillId, StringComparison.OrdinalIgnoreCase));
        if (range == null)
        {
            throw new SkillNotFoundException(skillId, version);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var items = await GetItemsAsync(range.Id, cancellationToken);
        var candidates = items
            .Where(item => context?.Mode is SkillExecutionMode.Evaluation or SkillExecutionMode.Preview || !item.IsDraft)
            .ToList();

        PromptItem selected = null;
        if (!string.IsNullOrWhiteSpace(version))
        {
            selected = candidates.FirstOrDefault(item =>
                string.Equals(item.FullVersion, version, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Tactic, version, StringComparison.OrdinalIgnoreCase));
        }

        selected ??= SelectBestItem(candidates, false);
        if (selected == null)
        {
            throw new SkillNotFoundException(skillId, version);
        }

        return selected;
    }

    private async Task<List<PromptItem>> GetItemsAsync(int rangeId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _promptItemService.GetFullListAsync(item => item.RangeId == rangeId);
    }

    private static PromptItem SelectBestItem(IEnumerable<PromptItem> items, bool publishedOnly)
    {
        var candidates = items.Where(item => !publishedOnly || !item.IsDraft).ToList();
        return candidates
            .OrderByDescending(item => item.EvalAvgScore >= 0 ? item.EvalAvgScore : decimal.MinValue)
            .ThenByDescending(item => item.EvalMaxScore >= 0 ? item.EvalMaxScore : decimal.MinValue)
            .ThenByDescending(item => item.Aiming)
            .ThenByDescending(item => item.Id)
            .FirstOrDefault();
    }

    private static SkillVersionDescriptor ToVersionDescriptor(
        string skillId,
        PromptRangeDto range,
        PromptItem item)
    {
        return new SkillVersionDescriptor(
            skillId,
            skillId,
            item.FullVersion,
            item.FullVersion,
            string.IsNullOrWhiteSpace(item.NickName) ? item.FullVersion : item.NickName,
            item.Note,
            item.IsDraft ? SkillStatus.Draft : SkillStatus.Published,
            item.EvalAvgScore,
            item.EvalMaxScore,
            ParseInputs(item.VariableDictJson));
    }

    private static IReadOnlyList<SkillInputDefinition> ParseInputs(string variableDictJson)
    {
        if (string.IsNullOrWhiteSpace(variableDictJson))
        {
            return Array.Empty<SkillInputDefinition>();
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(variableDictJson);
            return values?.Keys
                .Select(key => new SkillInputDefinition(key, key))
                .ToList() ?? new List<SkillInputDefinition>();
        }
        catch (JsonException ex)
        {
            throw new NcfExceptionBase($"Skill 输入参数格式错误：{ex.Message}");
        }
    }

    private static void ApplyArguments(
        PromptItemDto item,
        IReadOnlyDictionary<string, object> arguments,
        string input)
    {
        if (string.IsNullOrWhiteSpace(item.VariableDictJson))
        {
            return;
        }

        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(item.VariableDictJson)
                     ?? new Dictionary<string, string>();
        if (arguments != null)
        {
            foreach (var argument in arguments)
            {
                if (values.ContainsKey(argument.Key))
                {
                    values[argument.Key] = argument.Value?.ToString() ?? string.Empty;
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(input) && values.Count == 1)
        {
            values[values.Keys.Single()] = input;
        }

        item.VariableDictJson = JsonSerializer.Serialize(values);
    }

    private static string BuildGenerationPrompt(SkillGenerationRequest request, string existingContent)
    {
        var prompt = $@"You are designing a safe PromptRange Skill. Return JSON only with these properties:
skillId, name, description, content.
content must be a complete, production-quality prompt for the requested Skill.
Do not include executable code, shell commands, secrets, or claims of permissions.
The Skill must declare any file, network, or system side effects as requirements in prose only.
Requested Skill:
{request.Description}
{(string.IsNullOrWhiteSpace(request.ExistingSkillId) ? string.Empty : $"Existing Skill to improve: {request.ExistingSkillId}@{request.ExistingVersion}")}";
        return prompt + (string.IsNullOrWhiteSpace(existingContent)
            ? string.Empty
            : $"\nExisting prompt content to improve:\n{existingContent}");
    }

    private static GeneratedSkillDefinition ParseGeneratedSkill(
        string output,
        SkillGenerationRequest request)
    {
        try
        {
            var json = output?.Trim();
            if (json?.StartsWith("```", StringComparison.Ordinal) == true)
            {
                json = Regex.Replace(json, "^```(?:json)?\\s*|\\s*```$", string.Empty,
                    RegexOptions.IgnoreCase).Trim();
            }

            var definition = JsonSerializer.Deserialize<GeneratedSkillDefinition>(json, JsonOptions);
            if (definition != null && !string.IsNullOrWhiteSpace(definition.Content))
            {
                definition.SkillId = NormalizeSkillId(
                    string.IsNullOrWhiteSpace(request.SkillId) ? definition.SkillId : request.SkillId);
                definition.Name = string.IsNullOrWhiteSpace(request.SkillName)
                    ? definition.Name
                    : request.SkillName;
                return definition;
            }
        }
        catch (JsonException)
        {
            // Treat non-JSON model output as prompt content below.
        }

        return new GeneratedSkillDefinition
        {
            SkillId = NormalizeSkillId(request.SkillId ?? request.SkillName ?? "generated-skill"),
            Name = request.SkillName ?? request.SkillId ?? "Generated Skill",
            Description = request.Description,
            Content = output?.Trim()
        };
    }

    private static string NormalizeSkillId(string value)
    {
        var normalized = new string((value ?? "generated-skill")
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.'
                ? character
                : '-')
            .ToArray());
        return string.IsNullOrWhiteSpace(normalized) ? "generated-skill" : normalized;
    }

    private async Task<MemoryStream> DownloadGitHubFolderAsync(
        string repository,
        string branch,
        string folder,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("PromptRangeSkillPackage");
        if (client.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("NeuCharFramework-SkillImporter/1.0");
        }
        using var response = await client.GetAsync(
            $"https://api.github.com/repos/{repository}/zipball/{Uri.EscapeDataString(branch)}",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > 50L * 1024 * 1024)
        {
            throw new NcfExceptionBase("GitHub Skill 包大小超过限制（50 MB）");
        }

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var sourceBuffer = new MemoryStream();
        await source.CopyToAsync(sourceBuffer, cancellationToken);
        if (sourceBuffer.Length > 50L * 1024 * 1024)
        {
            throw new NcfExceptionBase("GitHub Skill 包大小超过限制（50 MB）");
        }

        sourceBuffer.Position = 0;
        using var sourceArchive = new ZipArchive(sourceBuffer, ZipArchiveMode.Read, leaveOpen: false);
        var normalizedFolder = folder.Trim('/').Replace('\\', '/');
        var skillId = NormalizeSkillId(Path.GetFileName(normalizedFolder));
        var output = new MemoryStream();
        using (var targetArchive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var manifest = new SkillPackageManifest(
                SkillId: skillId,
                SkillCode: skillId,
                Name: skillId,
                Description: $"Imported from GitHub: {repository}/{normalizedFolder}",
                Version: "external",
                PromptCode: "external");
            WriteEntry(targetArchive, "skill.json", JsonSerializer.Serialize(manifest, JsonOptions));

            var matchingEntries = sourceArchive.Entries
                .Where(entry => entry.FullName.Replace('\\', '/')
                    .Contains($"/{normalizedFolder}/", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matchingEntries.Count == 0)
            {
                throw new NcfExceptionBase("GitHub 地址对应的 Skill 文件夹不存在或为空");
            }

            var promptEntry = matchingEntries.FirstOrDefault(entry =>
                string.Equals(Path.GetFileName(entry.FullName), "SKILL.md", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(entry.FullName), "skprompt.txt", StringComparison.OrdinalIgnoreCase));
            if (promptEntry == null)
            {
                throw new NcfExceptionBase("GitHub Skill 文件夹必须包含 SKILL.md 或 skprompt.txt");
            }

            var pluginRoot = $"{skillId}/github/";
            var config = targetArchive.CreateEntry($"{pluginRoot}config.json");
            await using (var writer = new StreamWriter(config.Open()))
            {
                await writer.WriteAsync("{\"execution_settings\":{\"default\":{\"top_p\":0.8,\"temperature\":0.3,\"max_tokens\":4000,\"stop_sequences\":[]}}}");
            }

            var prompt = targetArchive.CreateEntry($"{pluginRoot}skprompt.txt");
            using (var promptWriter = new StreamWriter(prompt.Open()))
            using (var promptReader = new StreamReader(promptEntry.Open()))
            {
                await promptReader.BaseStream.CopyToAsync(promptWriter.BaseStream, cancellationToken);
            }

            foreach (var entry in matchingEntries.Where(entry => entry != promptEntry && !string.IsNullOrEmpty(entry.Name)))
            {
                var relative = entry.FullName.Replace('\\', '/');
                var marker = $"/{normalizedFolder}/";
                var index = relative.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                var relativePath = index >= 0 ? relative[(index + marker.Length)..] : entry.Name;
                var target = targetArchive.CreateEntry($"{pluginRoot}references/{relativePath}");
                await using var input = entry.Open();
                await using var targetStream = target.Open();
                await input.CopyToAsync(targetStream, cancellationToken);
            }
        }

        output.Position = 0;
        return output;
    }

    private static bool TryParseGitHubFolder(
        string value,
        out string repository,
        out string branch,
        out string folder)
    {
        repository = branch = folder = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var match = Regex.Match(uri.AbsolutePath,
            "^/(?<owner>[^/]+)/(?<repo>[^/]+)/tree/(?<branch>[^/]+)/(?<folder>.+)$",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return false;
        }

        repository = $"{match.Groups["owner"].Value}/{match.Groups["repo"].Value}";
        branch = Uri.UnescapeDataString(match.Groups["branch"].Value);
        folder = Uri.UnescapeDataString(match.Groups["folder"].Value);
        return true;
    }

    private static bool IsPreviewable(string path)
        => new[] { ".md", ".txt", ".json", ".yaml", ".yml", ".xml", ".cs", ".js", ".ts", ".py" }
            .Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private static string GetContentType(string path)
        => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".json" => "application/json",
            ".yaml" or ".yml" => "application/yaml",
            ".xml" => "application/xml",
            ".md" => "text/markdown",
            _ => "text/plain"
        };

    private sealed class GeneratedSkillDefinition
    {
        public string SkillId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
    }

    private static string SanitizePackageComponent(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string((value ?? "skill")
            .Select(character => invalidCharacters.Contains(character) ? '_' : character)
            .ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "skill" : sanitized;
    }

    private static string GetSkillId(PromptRangeDto range)
        => string.IsNullOrWhiteSpace(range.Alias) ? range.RangeName : range.Alias;

    private static string GetRangeName(PromptRangeDto range)
        => string.IsNullOrWhiteSpace(range.Alias) ? range.RangeName : range.Alias;

    private static bool Matches(SkillQuery query, string skillId, string alias, string rangeName)
    {
        if (!string.IsNullOrWhiteSpace(query.SkillId) &&
            !string.Equals(query.SkillId, skillId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(query.Keyword))
        {
            return true;
        }

        return skillId.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase) ||
               (alias?.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (rangeName?.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };
}
