/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SkillContracts.cs
    File description: Related implementation.

    Created by: Senparc - 20261016

    Modified by: Senparc - 20261021
    Change description: v0.3.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.3.1-preview6 add SourceFileId, SourceType, and SourceUrl columns to PromptRange table for PostgreSQL, SQL Server, and SQLite

    Modified by: Senparc - 20261119
    Change description: v0.3.3-preview8 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

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

public enum SkillKind
{
    Prompt = 0,
    Skill = 1
}

public enum SkillTestIsolation
{
    PromptOnly = 0,
    Workspace = 1,
    Sandbox = 2
}

public enum SkillRiskLevel
{
    ReadOnly = 0,
    WorkspaceWrite = 1,
    ExternalNetwork = 2,
    SystemMutation = 3
}

/// <summary>Describes an isolated Skill test request and its execution boundary.</summary>
public sealed record SkillTestRequest(
    string SkillId,
    string Version = null,
    string Input = null,
    SkillTestIsolation Isolation = SkillTestIsolation.PromptOnly,
    string WorkspaceId = null,
    string CallerModule = "PromptRange");

/// <summary>Reports the result, isolation state, output, and diagnostics of a Skill test.</summary>
public sealed record SkillTestResult(
    string SkillId,
    string Version,
    SkillTestIsolation Isolation,
    bool Succeeded,
    bool Isolated,
    string Output = null,
    string Error = null,
    IReadOnlyList<string> Diagnostics = null);

/// <summary>Defines risk, isolation, permission, timeout, output, and approval constraints.</summary>
public sealed record SkillExecutionPolicy(
    SkillRiskLevel RiskLevel = SkillRiskLevel.ReadOnly,
    SkillTestIsolation TestIsolation = SkillTestIsolation.PromptOnly,
    bool AllowWorkspaceRead = false,
    bool AllowWorkspaceWrite = false,
    bool AllowNetwork = false,
    IReadOnlyList<string> AllowedCommands = null,
    int TimeoutSeconds = 60,
    int MaxOutputBytes = 1024 * 1024,
    bool RequiresApproval = false);

/// <summary>Associates an Agent with a Skill version and execution policy.</summary>
public sealed record AgentSkillBinding(
    string SkillId,
    string Version = null,
    bool Enabled = true,
    bool AutoLoad = true,
    SkillExecutionPolicy Policy = null);

/// <summary>Filters Skill catalog results by identity, keyword, publication, caller, and allow-list.</summary>
public sealed record SkillQuery(
    string SkillId = null,
    string Keyword = null,
    bool PublishedOnly = true,
    string CallerModule = null,
    IReadOnlyCollection<string> AllowedSkillIds = null);

/// <summary>Describes one named Skill input and whether it is required.</summary>
public sealed record SkillInputDefinition(
    string Name,
    string Description = null,
    string Type = "string",
    bool Required = false);

/// <summary>Describes a tool exposed by a Skill.</summary>
public sealed record SkillToolDefinition(
    string Name,
    string Description = null,
    string InputSchemaJson = null);

/// <summary>Describes one executable Skill version and its evaluation metadata.</summary>
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

/// <summary>Describes a Skill identity, source, versions, tools, and execution policy.</summary>
public sealed record SkillDescriptor(
    string SkillId,
    string SkillCode,
    string Name,
    string Description,
    SkillStatus Status,
    SkillSourceType SourceType,
    string CurrentVersion,
    IReadOnlyList<SkillVersionDescriptor> Versions,
    IReadOnlyList<SkillToolDefinition> Tools,
    SkillKind Kind = SkillKind.Skill,
    SkillExecutionPolicy Policy = null);

/// <summary>Defines caller, tenant, conversation, mode, arguments, and policy context.</summary>
public sealed record SkillExecutionContext(
    string CallerModule,
    string UserId = null,
    string TenantId = null,
    string ConversationId = null,
    SkillExecutionMode Mode = SkillExecutionMode.Production,
    IReadOnlyDictionary<string, object> Arguments = null,
    SkillExecutionPolicy Policy = null);

/// <summary>Requests execution of a Skill version with optional input and persistence.</summary>
public sealed record SkillExecutionRequest(
    string SkillId,
    string Version = null,
    string Input = null,
    SkillExecutionContext Context = null,
    bool PersistResult = true);

/// <summary>Reports Skill execution output, failure text, and persisted PromptResult identity.</summary>
public sealed record SkillExecutionResult(
    string SkillId,
    string Version,
    string PromptCode,
    string Output,
    bool Succeeded,
    string Error = null,
    int? PromptResultId = null);

/// <summary>Describes the portable Skill package identity and exported version.</summary>
public sealed record SkillPackageManifest(
    int Schema = 1,
    string SkillId = null,
    string SkillCode = null,
    string Name = null,
    string Description = null,
    string Version = null,
    string PromptCode = null,
    DateTimeOffset? ExportedAt = null);

/// <summary>Reports an installed Skill and whether additional approval is required.</summary>
public sealed record SkillInstallResult(
    SkillDescriptor Skill,
    bool RequiresApproval,
    string Message);

/// <summary>Describes one file included in a Skill package preview.</summary>
public sealed record SkillContentFile(
    string Path,
    string ContentType,
    long Length,
    bool IsPreviewable,
    string Content = null);

/// <summary>Contains a Skill manifest and bounded package-file previews.</summary>
public sealed record SkillContentPreview(
    string SkillId,
    string Version,
    string ManifestJson,
    IReadOnlyList<SkillContentFile> Files);

/// <summary>Requests AI generation or improvement of a Skill prompt.</summary>
public sealed record SkillGenerationRequest(
    string Description,
    int ModelId,
    string SkillId = null,
    string SkillName = null,
    string ExistingSkillId = null,
    string ExistingVersion = null);

/// <summary>Reports generated Skill content and its approval requirement.</summary>
public sealed record SkillGenerationResult(
    SkillDescriptor Skill,
    string GeneratedContent,
    bool RequiresApproval,
    string Message);

public interface ISkillCatalog
{
    /// <summary>
    /// Lists Skills visible to the caller and matching the supplied publication/allow-list filters.
    /// </summary>
    /// <param name="query">Optional catalog filters.</param>
    /// <param name="cancellationToken">Token used to cancel catalog enumeration.</param>
    /// <returns>A task containing matching Skill descriptors.</returns>
    Task<IReadOnlyList<SkillDescriptor>> ListAsync(
        SkillQuery query = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves one Skill identity and version under the supplied caller execution context.
    /// </summary>
    /// <param name="skillId">The Skill ID, alias, or source identity.</param>
    /// <param name="version">Optional version selector.</param>
    /// <param name="context">Caller and execution-mode context.</param>
    /// <param name="cancellationToken">Token used to cancel resolution.</param>
    /// <returns>A task containing the resolved descriptor.</returns>
    /// <exception cref="SkillNotFoundException">Thrown when the Skill/version is unavailable to the caller.</exception>
    Task<SkillDescriptor> GetAsync(
        string skillId,
        string version = null,
        SkillExecutionContext context = null,
        CancellationToken cancellationToken = default);
}

public interface ISkillExecutor
{
    /// <summary>
    /// Executes a Skill after applying its policy, caller context, and version resolution rules.
    /// </summary>
    /// <param name="request">The Skill execution request.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <returns>A task containing output, success state, and optional persisted result identity.</returns>
    Task<SkillExecutionResult> ExecuteAsync(
        SkillExecutionRequest request,
        CancellationToken cancellationToken = default);
}

public interface ISkillTestRunner
{
    /// <summary>
    /// Executes a Skill in the requested test-isolation boundary.
    /// </summary>
    /// <param name="request">The isolated Skill test request.</param>
    /// <param name="cancellationToken">Token used to cancel testing.</param>
    /// <returns>A task containing output, isolation state, and diagnostics.</returns>
    Task<SkillTestResult> TestAsync(
        SkillTestRequest request,
        CancellationToken cancellationToken = default);
}

public interface ISkillPackageService
{
    /// <summary>Exports a Skill version as a portable package stream.</summary>
    /// <param name="skillId">The Skill identifier.</param>
    /// <param name="version">Optional version selector.</param>
    /// <param name="cancellationToken">Token used to cancel export.</param>
    /// <returns>A task containing the package stream.</returns>
    Task<Stream> ExportAsync(
        string skillId,
        string version = null,
        CancellationToken cancellationToken = default);

    /// <summary>Installs and validates a Skill package stream.</summary>
    /// <param name="package">The package stream.</param>
    /// <param name="fileName">The source file name.</param>
    /// <param name="sourceUrl">Optional source URL.</param>
    /// <param name="cancellationToken">Token used to cancel installation.</param>
    /// <returns>A task containing the installed Skill result.</returns>
    Task<SkillInstallResult> InstallAsync(
        Stream package,
        string fileName,
        string sourceUrl = null,
        CancellationToken cancellationToken = default);

    /// <summary>Downloads and installs a Skill package from a URL.</summary>
    /// <param name="packageUrl">The package URL.</param>
    /// <param name="cancellationToken">Token used to cancel download/install.</param>
    /// <returns>A task containing the installed Skill result.</returns>
    Task<SkillInstallResult> InstallFromUrlAsync(
        string packageUrl,
        CancellationToken cancellationToken = default);

    /// <summary>Builds a bounded preview of a Skill package's manifest and content files.</summary>
    /// <param name="skillId">The Skill identifier.</param>
    /// <param name="version">Optional version selector.</param>
    /// <param name="cancellationToken">Token used to cancel preview generation.</param>
    /// <returns>A task containing preview metadata and files.</returns>
    Task<SkillContentPreview> PreviewAsync(
        string skillId,
        string version = null,
        CancellationToken cancellationToken = default);

    /// <summary>Generates or improves a Skill definition using the configured AI model.</summary>
    /// <param name="request">The generation description and optional existing Skill identity.</param>
    /// <param name="cancellationToken">Token used to cancel generation.</param>
    /// <returns>A task containing generated content and approval state.</returns>
    Task<SkillGenerationResult> GenerateAsync(
        SkillGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Publishes a draft Skill version for production catalog use.</summary>
    /// <param name="skillId">The Skill identifier.</param>
    /// <param name="version">The version to publish.</param>
    /// <param name="cancellationToken">Token used to cancel publication.</param>
    /// <returns>A task containing the published descriptor.</returns>
    Task<SkillDescriptor> PublishAsync(
        string skillId,
        string version,
        CancellationToken cancellationToken = default);
}
