using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Abstractions.Skills;

public enum SkillStatus
{
    Draft = 0,
    Published = 1,
    Disabled = 2,
    Archived = 3
}

public enum SkillSourceType
{
    PromptRange = 0,
    FilePackage = 1,
    External = 2
}

public enum SkillExecutionMode
{
    Evaluation = 0,
    Preview = 1,
    Production = 2,
    Test = 3
}

public sealed record SkillQuery(
    string SkillId = null,
    string Keyword = null,
    bool PublishedOnly = true,
    string CallerModule = null);

public sealed record SkillInputDefinition(
    string Name,
    string Description = null,
    string Type = "string",
    bool Required = false);

public sealed record SkillToolDefinition(
    string Name,
    string Description = null,
    string InputSchemaJson = null);

public sealed record SkillVersionDescriptor(
    string SkillId,
    string SkillCode,
    string Version,
    string PromptCode,
    string Name,
    string Description,
    SkillStatus Status,
    decimal AverageScore,
    decimal MaximumScore,
    IReadOnlyList<SkillInputDefinition> Inputs);

public sealed record SkillDescriptor(
    string SkillId,
    string SkillCode,
    string Name,
    string Description,
    SkillStatus Status,
    SkillSourceType SourceType,
    string CurrentVersion,
    IReadOnlyList<SkillVersionDescriptor> Versions,
    IReadOnlyList<SkillToolDefinition> Tools);

public sealed record SkillExecutionContext(
    string CallerModule,
    string UserId = null,
    string TenantId = null,
    string ConversationId = null,
    SkillExecutionMode Mode = SkillExecutionMode.Production,
    IReadOnlyDictionary<string, object> Arguments = null);

public sealed record SkillExecutionRequest(
    string SkillId,
    string Version = null,
    string Input = null,
    SkillExecutionContext Context = null,
    bool PersistResult = true);

public sealed record SkillExecutionResult(
    string SkillId,
    string Version,
    string PromptCode,
    string Output,
    bool Succeeded,
    string Error = null,
    int? PromptResultId = null);

public sealed record SkillPackageManifest(
    int Schema = 1,
    string SkillId = null,
    string SkillCode = null,
    string Name = null,
    string Description = null,
    string Version = null,
    string PromptCode = null,
    DateTimeOffset? ExportedAt = null);

public sealed record SkillInstallResult(
    SkillDescriptor Skill,
    bool RequiresApproval,
    string Message);

public interface ISkillCatalog
{
    Task<IReadOnlyList<SkillDescriptor>> ListAsync(
        SkillQuery query = null,
        CancellationToken cancellationToken = default);

    Task<SkillDescriptor> GetAsync(
        string skillId,
        string version = null,
        SkillExecutionContext context = null,
        CancellationToken cancellationToken = default);
}

public interface ISkillExecutor
{
    Task<SkillExecutionResult> ExecuteAsync(
        SkillExecutionRequest request,
        CancellationToken cancellationToken = default);
}

public interface ISkillPackageService
{
    Task<Stream> ExportAsync(
        string skillId,
        string version = null,
        CancellationToken cancellationToken = default);

    Task<SkillInstallResult> InstallAsync(
        Stream package,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<SkillInstallResult> InstallFromUrlAsync(
        string packageUrl,
        CancellationToken cancellationToken = default);

    Task<SkillDescriptor> PublishAsync(
        string skillId,
        string version,
        CancellationToken cancellationToken = default);
}
