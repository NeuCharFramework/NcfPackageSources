/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IXncfDevelopmentJobService.cs
    文件功能描述：受控 XNCF 开发工作流契约

    创建标识：Senparc - 20260814

    修改标识：Senparc - 20260815
    修改描述：v0.41.0 增强隔离开发任务与 Sandbox 预览流程

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Development
{
    /// <summary>
    /// The workflow never writes its target solution during creation, editing, validation or
    /// preview. Only an administrator-only, explicit approval operation may apply a job.
    /// </summary>
    public enum XncfDevelopmentJobStage
    {
        Snapshotting = 0,
        ReadyForCode = 1,
        Validating = 2,
        Previewing = 3,
        ReadyForReview = 4,
        AwaitingHumanApproval = 5,
        Applied = 6,
        Discarded = 7,
        Failed = 8
    }

    public enum XncfDevelopmentJobMode
    {
        CreateNew = 0,
        ModifyExisting = 1
    }

    public sealed class XncfDevelopmentCreateOptions
    {
        public int OwnerAdminUserId { get; init; }
        public string SolutionFilePath { get; init; }
        public XncfDevelopmentJobMode Mode { get; init; }
        public string ModuleProjectName { get; init; }
        public string OrganizationName { get; init; }
        public string XncfName { get; init; }
        public string TargetFramework { get; init; } = "net10.0";
        public string Version { get; init; } = "0.1.0";
        public string MenuName { get; init; }
        public string Icon { get; init; } = "fa fa-puzzle-piece";
        public string Description { get; init; }
        public string Requirement { get; init; }
        public bool IncludeFunction { get; init; }
        public bool IncludeDatabase { get; init; }
        public bool IncludeWeb { get; init; }
        public bool IncludeWebApi { get; init; }
        public bool IncludeSample { get; init; }
    }

    public sealed class XncfDevelopmentJobInfo
    {
        public string JobId { get; init; }
        public int OwnerAdminUserId { get; init; }
        public XncfDevelopmentJobMode Mode { get; init; }
        public string ModuleProjectName { get; init; }
        public string TargetSolutionFilePath { get; init; }
        public string WorkspaceRootPath { get; init; }
        public string WorkspaceSolutionFilePath { get; init; }
        public string Requirement { get; init; }
        public XncfDevelopmentJobStage Stage { get; init; }
        public string StatusMessage { get; init; }
        public string ErrorMessage { get; init; }
        public string TargetModuleFingerprint { get; init; }
        public string WorkspaceModuleFingerprint { get; init; }
        public string ValidationSummary { get; init; }
        public string DiffSummary { get; init; }
        public string PreviewSessionId { get; init; }
        public string SandboxSessionId { get; init; }
        public string PreviewUrl { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public DateTimeOffset? CompletedAt { get; init; }
        public DateTimeOffset? MergeRequestedAt { get; init; }
        public DateTimeOffset? AppliedAt { get; init; }
        public bool IsTerminal => Stage is XncfDevelopmentJobStage.Applied
            or XncfDevelopmentJobStage.Discarded
            or XncfDevelopmentJobStage.Failed;
    }

    public sealed record XncfDevelopmentFileReadResult(string RelativeFilePath, string Content, string Sha256);

    public sealed record XncfDevelopmentFileWriteResult(
        string RelativeFilePath,
        bool IsNewFile,
        string PreviousSha256,
        string Sha256);

    public sealed class XncfDevelopmentPersistenceInfo
    {
        public bool IsAvailable { get; init; }
        public string StatusMessage { get; init; }
        public string ErrorMessage { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public DateTimeOffset? RetryAfter { get; init; }
    }

    public interface IXncfDevelopmentJobService
    {
        Task<XncfDevelopmentJobInfo> CreateAsync(
            XncfDevelopmentCreateOptions options,
            CancellationToken cancellationToken = default);

        Task<XncfDevelopmentFileReadResult> ReadFileAsync(
            string jobId,
            string relativeFilePath,
            CancellationToken cancellationToken = default);

        Task<XncfDevelopmentFileWriteResult> WriteFileAsync(
            string jobId,
            string relativeFilePath,
            string content,
            string expectedSha256 = null,
            CancellationToken cancellationToken = default);

        Task<XncfDevelopmentJobInfo> ValidateAsync(
            string jobId,
            CancellationToken cancellationToken = default);

        Task<XncfDevelopmentJobInfo> StartSandboxPreviewAsync(
            string jobId,
            CancellationToken cancellationToken = default);

        Task<XncfDevelopmentJobInfo> RequestMergeApprovalAsync(
            string jobId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// This method is intentionally not a FunctionRender. It is invoked only from an
        /// antiforgery-protected Admin page after the administrator types the confirmation phrase.
        /// </summary>
        Task<XncfDevelopmentJobInfo> ApplyApprovedJobAsync(
            string jobId,
            string confirmationPhrase,
            CancellationToken cancellationToken = default);

        Task<XncfDevelopmentJobInfo> DiscardAsync(
            string jobId,
            CancellationToken cancellationToken = default);

        Task<XncfDevelopmentJobInfo> GetAsync(string jobId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<XncfDevelopmentJobInfo>> GetRecentAsync(
            int maxCount = 100,
            CancellationToken cancellationToken = default);

        XncfDevelopmentPersistenceInfo GetPersistenceStatus();
    }
}
