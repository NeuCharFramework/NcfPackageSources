/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SkillContracts.cs
    文件功能描述：PromptRange Skill 跨模块契约
    
    
    创建标识：Senparc - 20261016
    
    修改标识：Senparc - 20261021
    修改描述：v0.3.0-preview6 扩展 Skill 类型、测试隔离、风险策略与 Agent 绑定

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

public sealed record SkillTestRequest(
    string SkillId,
    string Version = null,
    string Input = null,
    SkillTestIsolation Isolation = SkillTestIsolation.PromptOnly,
    string WorkspaceId = null,
    string CallerModule = "PromptRange");

public sealed record SkillTestResult(
    string SkillId,
    string Version,
    SkillTestIsolation Isolation,
    bool Succeeded,
    bool Isolated,
    string Output = null,
    string Error = null,
    IReadOnlyList<string> Diagnostics = null);

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

public sealed record AgentSkillBinding(
    string SkillId,
    string Version = null,
    bool Enabled = true,
    bool AutoLoad = true,
    SkillExecutionPolicy Policy = null);

public sealed record SkillQuery(
    string SkillId = null,
    string Keyword = null,
    bool PublishedOnly = true,
    string CallerModule = null,
    IReadOnlyCollection<string> AllowedSkillIds = null);

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
    IReadOnlyList<SkillToolDefinition> Tools,
    SkillKind Kind = SkillKind.Skill,
    SkillExecutionPolicy Policy = null);

public sealed record SkillExecutionContext(
    string CallerModule,
    string UserId = null,
    string TenantId = null,
    string ConversationId = null,
    SkillExecutionMode Mode = SkillExecutionMode.Production,
    IReadOnlyDictionary<string, object> Arguments = null,
    SkillExecutionPolicy Policy = null);

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

public interface ISkillTestRunner
{
    Task<SkillTestResult> TestAsync(
        SkillTestRequest request,
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
