/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfDevelopmentJobService.cs
    文件功能描述：隔离 XNCF 创建、修改、Sandbox 预览和人工合入工作流

    创建标识：Senparc - 20260814

    修改标识：Senparc - 20260815
    修改描述：v0.41.0 增强隔离开发任务与 Sandbox 预览流程

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.XncfBuilder.Domain.Services.Preview;
using Senparc.Xncf.XncfBuilder.Domain.Services.Workspace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Development
{
    /// <summary>
    /// The only service that coordinates the AI-capable development workflow. It never receives
    /// an arbitrary shell command and it never writes a target checkout before an explicit Admin
    /// approval. All AI edits are further restricted by <see cref="XncfWorkspaceFileService"/>.
    /// </summary>
    public sealed class XncfDevelopmentJobService : IXncfDevelopmentJobService
    {
        private readonly IXncfDevelopmentJobStateStore _stateStore;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _jobLocks = new(StringComparer.OrdinalIgnoreCase);

        public XncfDevelopmentJobService(
            IXncfDevelopmentJobStateStore stateStore,
            IServiceProvider serviceProvider)
        {
            _stateStore = stateStore;
            _serviceProvider = serviceProvider;
        }

        public async Task<XncfDevelopmentJobInfo> CreateAsync(
            XncfDevelopmentCreateOptions options,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            var targetSolution = ValidateSolutionPath(options.SolutionFilePath);
            var moduleName = ResolveModuleProjectName(options);
            var now = DateTimeOffset.UtcNow;
            var snapshot = new XncfDevelopmentJobSnapshot
            {
                JobId = "xncfdev-" + Guid.NewGuid().ToString("N"),
                OwnerAdminUserId = Math.Max(options.OwnerAdminUserId, 0),
                Mode = options.Mode,
                ModuleProjectName = moduleName,
                TargetSolutionFilePath = targetSolution,
                Requirement = Truncate(options.Requirement, 4000),
                Stage = XncfDevelopmentJobStage.Snapshotting,
                StatusMessage = "正在创建不含密钥与构建产物的源码快照。",
                CreatedAt = now,
                UpdatedAt = now
            };

            // Persist the intent first. If the XncfBuilder table has not been migrated, no source
            // copy or template command is performed, which is safer than silently falling back to
            // an in-memory task that could later be merged without an audit trail.
            await _stateStore.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
            try
            {
                var workspace = await XncfDevelopmentWorkspaceService.CreateSnapshotAsync(
                        targetSolution,
                        snapshot.JobId,
                        cancellationToken)
                    .ConfigureAwait(false);
                snapshot.WorkspaceRootPath = workspace.WorkspaceRootPath;
                snapshot.WorkspaceSolutionFilePath = workspace.WorkspaceSolutionFilePath;

                if (options.Mode == XncfDevelopmentJobMode.CreateNew)
                {
                    await CreateModuleInWorkspaceAsync(workspace.WorkspaceSolutionFilePath, moduleName, options, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    _ = XncfWorkspaceFileService.ResolveModuleDirectory(workspace.WorkspaceSolutionFilePath, moduleName);
                    var targetModule = XncfWorkspaceFileService.ResolveModuleDirectory(targetSolution, moduleName);
                    snapshot.TargetModuleFingerprint = XncfPreviewService.ComputeSourceFingerprint(targetModule);
                }

                var diff = XncfDevelopmentWorkspaceService.BuildModuleDiff(
                    targetSolution,
                    workspace.WorkspaceSolutionFilePath,
                    moduleName,
                    options.Mode == XncfDevelopmentJobMode.CreateNew);
                snapshot.WorkspaceModuleFingerprint = diff.WorkspaceModuleFingerprint;
                snapshot.DiffSummary = diff.Summary;
                snapshot.Stage = XncfDevelopmentJobStage.ReadyForCode;
                snapshot.StatusMessage = options.Mode == XncfDevelopmentJobMode.CreateNew
                    ? "模块模板已在隔离工作区生成；可由 AI 或开发者仅修改模块代码文件。"
                    : "现有模块已复制到隔离工作区；可由 AI 或开发者仅修改模块代码文件。";
                snapshot.UpdatedAt = DateTimeOffset.UtcNow;
                await _stateStore.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
                return ToInfo(snapshot);
            }
            catch (Exception ex)
            {
                await MarkFailedAsync(snapshot, ex, cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        public async Task<XncfDevelopmentFileReadResult> ReadFileAsync(
            string jobId,
            string relativeFilePath,
            CancellationToken cancellationToken = default)
        {
            var snapshot = await GetRequiredSnapshotAsync(jobId, cancellationToken).ConfigureAwait(false);
            EnsureWorkspaceAvailable(snapshot);
            var moduleDirectory = XncfWorkspaceFileService.ResolveModuleDirectory(
                snapshot.WorkspaceSolutionFilePath,
                snapshot.ModuleProjectName);
            var result = await XncfWorkspaceFileService.ReadTextAsync(moduleDirectory, relativeFilePath, cancellationToken)
                .ConfigureAwait(false);
            return new XncfDevelopmentFileReadResult(relativeFilePath, result.Content, result.Sha256);
        }

        public async Task<XncfDevelopmentFileWriteResult> WriteFileAsync(
            string jobId,
            string relativeFilePath,
            string content,
            string expectedSha256 = null,
            CancellationToken cancellationToken = default)
        {
            return await RunLockedAsync(jobId, async snapshot =>
            {
                if (snapshot.Stage is XncfDevelopmentJobStage.AwaitingHumanApproval
                    or XncfDevelopmentJobStage.Applied
                    or XncfDevelopmentJobStage.Discarded)
                {
                    throw new InvalidOperationException("该开发任务已进入人工审批或终态，不能继续修改。请创建新任务。");
                }

                EnsureWorkspaceAvailable(snapshot);
                var moduleDirectory = XncfWorkspaceFileService.ResolveModuleDirectory(
                    snapshot.WorkspaceSolutionFilePath,
                    snapshot.ModuleProjectName);
                var result = await XncfWorkspaceFileService.WriteTextAtomicAsync(
                        moduleDirectory,
                        relativeFilePath,
                        content,
                        expectedSha256,
                        cancellationToken)
                    .ConfigureAwait(false);
                await RefreshDiffAsync(snapshot, cancellationToken).ConfigureAwait(false);
                snapshot.Stage = XncfDevelopmentJobStage.ReadyForCode;
                snapshot.ValidationSummary = null;
                snapshot.SandboxSessionId = null;
                snapshot.PreviewUrl = null;
                snapshot.StatusMessage = "隔离工作区代码已更新；请执行校验并在 Sandbox 中预览。";
                snapshot.ErrorMessage = null;
                await SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
                return new XncfDevelopmentFileWriteResult(
                    relativeFilePath,
                    result.IsNewFile,
                    result.PreviousSha256,
                    result.Sha256);
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<XncfDevelopmentJobInfo> ValidateAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            return await RunLockedAsync(jobId, async snapshot =>
            {
                EnsureWorkspaceAvailable(snapshot);
                snapshot.Stage = XncfDevelopmentJobStage.Validating;
                snapshot.StatusMessage = "正在校验隔离工作区布局、模块路径和宿主源码引用。";
                snapshot.ErrorMessage = null;
                await SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);

                try
                {
                    var paths = XncfPreviewService.ResolveProjectPaths(
                        snapshot.WorkspaceSolutionFilePath,
                        snapshot.ModuleProjectName);
                    XncfPreviewService.ValidateHostProjectReference(paths);
                    await RefreshDiffAsync(snapshot, cancellationToken).ConfigureAwait(false);
                    snapshot.ValidationSummary =
                        "结构校验通过：隔离 Senparc.Web 直接引用当前模块源码。完整还原、编译和运行将仅在受控 Sandbox 预览中执行。";
                    snapshot.Stage = XncfDevelopmentJobStage.ReadyForReview;
                    snapshot.StatusMessage = "结构校验完成，已可启动 Sandbox 预览或查看差异。";
                    await SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
                    return ToInfo(snapshot);
                }
                catch (Exception ex)
                {
                    await MarkFailedAsync(snapshot, ex, cancellationToken).ConfigureAwait(false);
                    throw;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<XncfDevelopmentJobInfo> StartSandboxPreviewAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            return await RunLockedAsync(jobId, async snapshot =>
            {
                EnsureWorkspaceAvailable(snapshot);
                if (snapshot.Stage is XncfDevelopmentJobStage.Applied or XncfDevelopmentJobStage.Discarded)
                {
                    throw new InvalidOperationException("已结束的开发任务不能启动预览。");
                }

                // Validate in the same per-job lock so the host reference and the diff belong to
                // exactly the workspace tree that will be copied into Sandbox.
                var paths = XncfPreviewService.ResolveProjectPaths(
                    snapshot.WorkspaceSolutionFilePath,
                    snapshot.ModuleProjectName);
                XncfPreviewService.ValidateHostProjectReference(paths);
                EnsureSandboxPathBaseSupport(paths.WebProjectFilePath);
                await RefreshDiffAsync(snapshot, cancellationToken).ConfigureAwait(false);

                var sandbox = _serviceProvider.GetService<IXncfSandboxPreviewService>();
                if (sandbox == null)
                {
                    throw new InvalidOperationException(
                        "未安装或未启动 Senparc.Xncf.Sandbox；为避免在主站执行未审查代码，隔离开发不会回退到本机进程预览。");
                }

                snapshot.Stage = XncfDevelopmentJobStage.Previewing;
                snapshot.StatusMessage = "正在将已清洗的工作区副本交给 Sandbox 构建并启动预览。";
                snapshot.ErrorMessage = null;
                await SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);

                try
                {
                    var workspaceSolutionRelativePath = Path.GetRelativePath(
                        snapshot.WorkspaceRootPath,
                        snapshot.WorkspaceSolutionFilePath);
                    var preview = await sandbox.StartAsync(
                            new XncfSandboxPreviewRequest
                            {
                                SourceWorkspacePath = snapshot.WorkspaceRootPath,
                                SolutionRelativePath = workspaceSolutionRelativePath,
                                ModuleProjectName = snapshot.ModuleProjectName,
                                // Ownership is frozen when the audit record is created; later
                                // tool calls cannot move a task into somebody else's quota.
                                OwnerUserId = snapshot.OwnerAdminUserId,
                                // Sandbox keeps this denied unless its administrator has enabled a
                                // dedicated package-mirror network. AI cannot add PackageReference
                                // or NuGet.config files, so an enabled restore only resolves the
                                // reviewed template/baseline dependency graph.
                                AllowDependencyRestoreNetwork = true
                            },
                            cancellationToken)
                        .ConfigureAwait(false);

                    snapshot.SandboxSessionId = preview.SandboxSessionId;
                    snapshot.PreviewUrl = preview.AccessUrl;
                    snapshot.Stage = XncfDevelopmentJobStage.ReadyForReview;
                    snapshot.ValidationSummary = "Sandbox 已接受隔离工作区并完成受控预览启动。";
                    snapshot.StatusMessage = preview.StatusMessage ?? "Sandbox 预览已启动；请在代理地址中验证模块功能。";
                    await SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
                    return ToInfo(snapshot);
                }
                catch (Exception ex)
                {
                    await MarkFailedAsync(snapshot, ex, cancellationToken).ConfigureAwait(false);
                    throw;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<XncfDevelopmentJobInfo> RequestMergeApprovalAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            return await RunLockedAsync(jobId, async snapshot =>
            {
                if (snapshot.Stage != XncfDevelopmentJobStage.ReadyForReview)
                {
                    throw new InvalidOperationException("只有完成结构校验（并可选完成 Sandbox 预览）的任务才能请求人工合入。");
                }

                await RefreshDiffAsync(snapshot, cancellationToken).ConfigureAwait(false);
                snapshot.Stage = XncfDevelopmentJobStage.AwaitingHumanApproval;
                snapshot.MergeRequestedAt = DateTimeOffset.UtcNow;
                snapshot.StatusMessage = "等待管理员在预览监控页查看差异并输入确认短语后合入目标源码。";
                await SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
                return ToInfo(snapshot);
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<XncfDevelopmentJobInfo> ApplyApprovedJobAsync(
            string jobId,
            string confirmationPhrase,
            CancellationToken cancellationToken = default)
        {
            return await RunLockedAsync(jobId, async snapshot =>
            {
                if (snapshot.Stage != XncfDevelopmentJobStage.AwaitingHumanApproval)
                {
                    throw new InvalidOperationException("该任务尚未进入人工合入审批阶段。");
                }

                var expectedConfirmation = "APPLY " + snapshot.ModuleProjectName;
                if (!string.Equals(confirmationPhrase?.Trim(), expectedConfirmation, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"确认短语不匹配。请输入：{expectedConfirmation}");
                }

                EnsureWorkspaceAvailable(snapshot);
                try
                {
                    await ApplyWorkspaceModuleAsync(snapshot, cancellationToken).ConfigureAwait(false);
                    snapshot.Stage = XncfDevelopmentJobStage.Applied;
                    snapshot.AppliedAt = DateTimeOffset.UtcNow;
                    snapshot.CompletedAt = snapshot.AppliedAt;
                    snapshot.StatusMessage = "管理员确认后已将受控模块差异合入目标源码；请在正常开发流程中审查并提交版本控制。";
                    snapshot.ErrorMessage = null;
                    await SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
                    return ToInfo(snapshot);
                }
                catch (Exception ex)
                {
                    await MarkFailedAsync(snapshot, ex, cancellationToken).ConfigureAwait(false);
                    throw;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<XncfDevelopmentJobInfo> DiscardAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            return await RunLockedAsync(jobId, async snapshot =>
            {
                if (!string.IsNullOrWhiteSpace(snapshot.SandboxSessionId))
                {
                    var sandbox = _serviceProvider.GetService<IXncfSandboxPreviewService>();
                    if (sandbox != null)
                    {
                        await sandbox.StopAsync(snapshot.SandboxSessionId, cancellationToken).ConfigureAwait(false);
                    }
                }

                XncfDevelopmentWorkspaceService.TryDeleteWorkspace(snapshot.JobId);
                snapshot.Stage = XncfDevelopmentJobStage.Discarded;
                snapshot.CompletedAt = DateTimeOffset.UtcNow;
                snapshot.StatusMessage = "隔离工作区和对应 Sandbox 预览已请求回收；目标源码未被修改。";
                await SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
                return ToInfo(snapshot);
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<XncfDevelopmentJobInfo> GetAsync(string jobId, CancellationToken cancellationToken = default) =>
            ToInfo(await GetRequiredSnapshotAsync(jobId, cancellationToken).ConfigureAwait(false));

        public async Task<IReadOnlyList<XncfDevelopmentJobInfo>> GetRecentAsync(
            int maxCount = 100,
            CancellationToken cancellationToken = default)
        {
            var jobs = await _stateStore.GetRecentAsync(Math.Clamp(maxCount, 1, 200), cancellationToken).ConfigureAwait(false);
            return jobs.Select(ToInfo).ToArray();
        }

        public XncfDevelopmentPersistenceInfo GetPersistenceStatus() =>
            _stateStore.GetPersistenceStatus();

        private async Task ApplyWorkspaceModuleAsync(XncfDevelopmentJobSnapshot snapshot, CancellationToken cancellationToken)
        {
            var workspaceModule = XncfWorkspaceFileService.ResolveModuleDirectory(
                snapshot.WorkspaceSolutionFilePath,
                snapshot.ModuleProjectName);
            var targetSolutionDirectory = Path.GetDirectoryName(snapshot.TargetSolutionFilePath)
                ?? throw new InvalidOperationException("无法获取目标解决方案目录。");
            var targetModule = Path.Combine(targetSolutionDirectory, snapshot.ModuleProjectName);
            var backupRoot = Path.Combine(
                Path.GetTempPath(), "Senparc.Ncf", "XncfDevelopment", snapshot.JobId, "apply-backup");
            Directory.CreateDirectory(backupRoot);

            if (snapshot.Mode == XncfDevelopmentJobMode.ModifyExisting)
            {
                var resolvedTargetModule = XncfWorkspaceFileService.ResolveModuleDirectory(
                    snapshot.TargetSolutionFilePath,
                    snapshot.ModuleProjectName);
                var currentFingerprint = XncfPreviewService.ComputeSourceFingerprint(resolvedTargetModule);
                if (!string.Equals(currentFingerprint, snapshot.TargetModuleFingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("目标模块在创建隔离任务后已变化，拒绝覆盖。请创建新的开发任务并重新生成差异。");
                }

                EnsureProjectFileUnchanged(resolvedTargetModule, workspaceModule, snapshot.ModuleProjectName);
                await CopyAllowedModuleFilesWithRollbackAsync(workspaceModule, resolvedTargetModule, backupRoot, cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            if (Directory.Exists(targetModule))
            {
                throw new InvalidOperationException("目标解决方案中已存在同名模块，拒绝覆盖。");
            }

            var stagingDirectory = targetModule + ".xncfbuilder-staging-" + Guid.NewGuid().ToString("N");
            var webProject = Path.Combine(targetSolutionDirectory, "Senparc.Web", "Senparc.Web.csproj");
            var solutionBackup = Path.Combine(backupRoot, Path.GetFileName(snapshot.TargetSolutionFilePath));
            var webProjectBackup = Path.Combine(backupRoot, Path.GetFileName(webProject));
            try
            {
                await CopyNewModuleToStagingAsync(workspaceModule, stagingDirectory, cancellationToken).ConfigureAwait(false);
                File.Copy(snapshot.TargetSolutionFilePath, solutionBackup, overwrite: false);
                File.Copy(webProject, webProjectBackup, overwrite: false);
                Directory.Move(stagingDirectory, targetModule);

                var moduleProject = Path.Combine(targetModule, snapshot.ModuleProjectName + ".csproj");
                await RunDotNetAsync(
                        targetSolutionDirectory,
                        new[] { "add", webProject, "reference", moduleProject },
                        cancellationToken)
                    .ConfigureAwait(false);
                await RunDotNetAsync(
                        targetSolutionDirectory,
                        new[] { "sln", snapshot.TargetSolutionFilePath, "add", moduleProject, "--solution-folder", "XncfModules" },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                if (File.Exists(solutionBackup)) File.Copy(solutionBackup, snapshot.TargetSolutionFilePath, overwrite: true);
                if (File.Exists(webProjectBackup)) File.Copy(webProjectBackup, webProject, overwrite: true);
                if (Directory.Exists(targetModule)) Directory.Delete(targetModule, recursive: true);
                throw;
            }
            finally
            {
                if (Directory.Exists(stagingDirectory)) Directory.Delete(stagingDirectory, recursive: true);
            }
        }

        private static async Task CopyAllowedModuleFilesWithRollbackAsync(
            string workspaceModule,
            string targetModule,
            string backupRoot,
            CancellationToken cancellationToken)
        {
            var rollback = new List<(string Target, string Backup, bool Existed)>();
            try
            {
                foreach (var sourceFile in EnumerateMergeableFiles(workspaceModule, includeProjectFile: false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var relative = Path.GetRelativePath(workspaceModule, sourceFile);
                    var target = XncfWorkspaceFileService.ResolveFilePath(targetModule, relative);
                    var targetDirectory = Path.GetDirectoryName(target)!;
                    Directory.CreateDirectory(targetDirectory);
                    var backup = Path.Combine(backupRoot, "files", relative);
                    var existed = File.Exists(target);
                    if (existed)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                        File.Copy(target, backup, overwrite: false);
                    }

                    var temp = target + ".xncfbuilder-" + Guid.NewGuid().ToString("N") + ".tmp";
                    await using (var source = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    await using (var destination = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        await source.CopyToAsync(destination, 81920, cancellationToken).ConfigureAwait(false);
                    }
                    File.Move(temp, target, overwrite: true);
                    rollback.Add((target, backup, existed));
                }
            }
            catch
            {
                foreach (var item in rollback.AsEnumerable().Reverse())
                {
                    if (item.Existed && File.Exists(item.Backup))
                    {
                        File.Copy(item.Backup, item.Target, overwrite: true);
                    }
                    else if (!item.Existed && File.Exists(item.Target))
                    {
                        File.Delete(item.Target);
                    }
                }
                throw;
            }
        }

        private static async Task CopyNewModuleToStagingAsync(string workspaceModule, string stagingDirectory, CancellationToken cancellationToken)
        {
            foreach (var sourceFile in EnumerateMergeableFiles(workspaceModule, includeProjectFile: true))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relative = Path.GetRelativePath(workspaceModule, sourceFile);
                var destination = Path.Combine(stagingDirectory, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                await using var source = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                await using var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await source.CopyToAsync(target, 81920, cancellationToken).ConfigureAwait(false);
            }
        }

        private static IEnumerable<string> EnumerateMergeableFiles(string moduleDirectory, bool includeProjectFile)
        {
            var pending = new Stack<string>();
            pending.Push(moduleDirectory);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                foreach (var directory in Directory.EnumerateDirectories(current))
                {
                    var info = new DirectoryInfo(directory);
                    if (!info.Attributes.HasFlag(FileAttributes.ReparsePoint)
                        && info.Name is not "bin" and not "obj" and not ".git")
                    {
                        pending.Push(info.FullName);
                    }
                }

                foreach (var file in Directory.EnumerateFiles(current))
                {
                    var info = new FileInfo(file);
                    if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        throw new UnauthorizedAccessException("合入时拒绝模块内的符号链接文件。");
                    }

                    if (includeProjectFile && info.Extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return info.FullName;
                        continue;
                    }

                    var isMergeable = true;
                    try
                    {
                        XncfWorkspaceFileService.ValidateWritableCodeFile(
                            Path.GetRelativePath(moduleDirectory, info.FullName));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Ignore files that the AI tool could never have changed. Existing target
                        // metadata and arbitrary binaries remain untouched on merge.
                        isMergeable = false;
                    }

                    if (isMergeable)
                    {
                        yield return info.FullName;
                    }
                }
            }
        }

        private static void EnsureProjectFileUnchanged(string targetModule, string workspaceModule, string moduleName)
        {
            var fileName = moduleName + ".csproj";
            var targetProject = Path.Combine(targetModule, fileName);
            var workspaceProject = Path.Combine(workspaceModule, fileName);
            if (!File.Exists(targetProject) || !File.Exists(workspaceProject)
                || !string.Equals(
                    XncfWorkspaceFileService.ComputeFileSha256(targetProject),
                    XncfWorkspaceFileService.ComputeFileSha256(workspaceProject),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("隔离工作区的模块项目文件发生变化，拒绝合入。请仅通过受控工具编辑模块代码文件。");
            }
        }

        private static async Task CreateModuleInWorkspaceAsync(
            string workspaceSolutionFilePath,
            string moduleName,
            XncfDevelopmentCreateOptions options,
            CancellationToken cancellationToken)
        {
            var solutionDirectory = Path.GetDirectoryName(workspaceSolutionFilePath)!;
            var webProject = Path.Combine(solutionDirectory, "Senparc.Web", "Senparc.Web.csproj");
            if (!File.Exists(webProject))
            {
                throw new FileNotFoundException("隔离工作区中未找到 Senparc.Web.csproj。", webProject);
            }

            var destination = Path.Combine(solutionDirectory, moduleName);
            if (Directory.Exists(destination))
            {
                throw new InvalidOperationException("隔离工作区中已存在同名模块，拒绝模板覆盖。");
            }

            var (organization, xncfName) = SplitModuleName(moduleName, options);
            var args = new List<string>
            {
                "new", "XNCF", "-n", moduleName, "-o", destination,
                "--IntegrationToNcf", "true",
                "--TargetFramework", options.TargetFramework,
                "--OrgName", organization,
                "--XncfName", xncfName,
                "--Guid", Guid.NewGuid().ToString().ToUpperInvariant(),
                "--Icon", options.Icon,
                "--Description", options.Description ?? options.Requirement ?? moduleName,
                "--Version", options.Version,
                "--MenuName", options.MenuName ?? xncfName
            };
            if (options.IncludeSample) args.AddRange(new[] { "--Sample", "true" });
            if (options.IncludeFunction) args.AddRange(new[] { "--Function", "true" });
            if (options.IncludeWeb) args.AddRange(new[] { "--Web", "true" });
            if (options.IncludeDatabase) args.AddRange(new[] { "--Database", "true" });
            if (options.IncludeWebApi) args.AddRange(new[] { "--UseWebApi", "true" });

            // No `dotnet new install` is ever performed here. The administrator controls the
            // locally installed XNCF template; unavailable templates fail before any target source
            // can be affected.
            await RunDotNetAsync(solutionDirectory, args, cancellationToken).ConfigureAwait(false);
            var moduleProject = Path.Combine(destination, moduleName + ".csproj");
            if (!File.Exists(moduleProject))
            {
                throw new FileNotFoundException("dotnet new 已结束，但隔离工作区中未生成模块项目文件。", moduleProject);
            }

            await RunDotNetAsync(solutionDirectory, new[] { "add", webProject, "reference", moduleProject }, cancellationToken)
                .ConfigureAwait(false);
            await RunDotNetAsync(solutionDirectory, new[] { "sln", workspaceSolutionFilePath, "add", moduleProject, "--solution-folder", "XncfModules" }, cancellationToken)
                .ConfigureAwait(false);
        }

        private static async Task RunDotNetAsync(string workingDirectory, IEnumerable<string> arguments, CancellationToken cancellationToken)
        {
            var args = arguments.ToArray();
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                throw new InvalidOperationException("无法启动 dotnet CLI。");
            }
            var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            if (process.ExitCode != 0)
            {
                var detail = (await error.ConfigureAwait(false)).Trim();
                if (string.IsNullOrWhiteSpace(detail)) detail = (await output.ConfigureAwait(false)).Trim();
                throw new InvalidOperationException($"dotnet {string.Join(' ', args)} 失败（退出码 {process.ExitCode}）：{Truncate(detail, 1600)}");
            }
        }

        private async Task<T> RunLockedAsync<T>(
            string jobId,
            Func<XncfDevelopmentJobSnapshot, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("必须提供开发任务 ID。", nameof(jobId));
            }
            var normalizedJobId = jobId.Trim();
            var gate = _jobLocks.GetOrAdd(normalizedJobId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var snapshot = await GetRequiredSnapshotAsync(normalizedJobId, cancellationToken).ConfigureAwait(false);
                return await operation(snapshot).ConfigureAwait(false);
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task RefreshDiffAsync(XncfDevelopmentJobSnapshot snapshot, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var diff = XncfDevelopmentWorkspaceService.BuildModuleDiff(
                snapshot.TargetSolutionFilePath,
                snapshot.WorkspaceSolutionFilePath,
                snapshot.ModuleProjectName,
                snapshot.Mode == XncfDevelopmentJobMode.CreateNew);
            snapshot.WorkspaceModuleFingerprint = diff.WorkspaceModuleFingerprint;
            snapshot.DiffSummary = diff.Summary;
        }

        private async Task<XncfDevelopmentJobSnapshot> GetRequiredSnapshotAsync(string jobId, CancellationToken cancellationToken)
        {
            var snapshot = await _stateStore.GetAsync(jobId, cancellationToken).ConfigureAwait(false);
            return snapshot ?? throw new InvalidOperationException("开发任务不存在，或任务记录已过期。");
        }

        private async Task SaveAsync(XncfDevelopmentJobSnapshot snapshot, CancellationToken cancellationToken)
        {
            snapshot.UpdatedAt = DateTimeOffset.UtcNow;
            await _stateStore.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
        }

        private async Task MarkFailedAsync(XncfDevelopmentJobSnapshot snapshot, Exception ex, CancellationToken cancellationToken)
        {
            snapshot.Stage = XncfDevelopmentJobStage.Failed;
            snapshot.ErrorMessage = Truncate(ex.Message, 4000);
            snapshot.StatusMessage = "隔离开发任务失败；目标源码未被该失败步骤直接修改。";
            snapshot.CompletedAt = DateTimeOffset.UtcNow;
            try
            {
                await SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Preserve the original operation failure if database persistence itself is down.
            }
        }

        private static string ValidateSolutionPath(string solutionFilePath)
        {
            if (string.IsNullOrWhiteSpace(solutionFilePath)
                || !string.Equals(Path.GetExtension(solutionFilePath), ".sln", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(solutionFilePath))
            {
                throw new FileNotFoundException("未找到目标解决方案文件。", solutionFilePath);
            }
            return Path.GetFullPath(solutionFilePath);
        }

        private static string ResolveModuleProjectName(XncfDevelopmentCreateOptions options)
        {
            var name = options.Mode == XncfDevelopmentJobMode.CreateNew
                ? $"{options.OrganizationName}.Xncf.{options.XncfName}"
                : options.ModuleProjectName;
            if (string.IsNullOrWhiteSpace(name)
                || name is "." or ".."
                || !string.Equals(name, Path.GetFileName(name), StringComparison.Ordinal)
                || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || name.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_')))
            {
                throw new ArgumentException("模块项目名称无效，必须为完整名称，例如 Senparc.Xncf.Sample。", nameof(options));
            }
            return name;
        }

        private static (string Organization, string XncfName) SplitModuleName(
            string moduleName,
            XncfDevelopmentCreateOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.OrganizationName) && !string.IsNullOrWhiteSpace(options.XncfName))
            {
                return (options.OrganizationName, options.XncfName);
            }

            const string marker = ".Xncf.";
            var markerIndex = moduleName.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex <= 0 || markerIndex + marker.Length >= moduleName.Length)
            {
                throw new ArgumentException("新模块必须提供组织名和模块名，或使用 {Org}.Xncf.{Name} 完整项目名称。", nameof(options));
            }
            return (moduleName[..markerIndex], moduleName[(markerIndex + marker.Length)..]);
        }

        private static void EnsureWorkspaceAvailable(XncfDevelopmentJobSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(snapshot.WorkspaceRootPath)
                || string.IsNullOrWhiteSpace(snapshot.WorkspaceSolutionFilePath)
                || !Directory.Exists(snapshot.WorkspaceRootPath)
                || !File.Exists(snapshot.WorkspaceSolutionFilePath))
            {
                throw new InvalidOperationException("隔离工作区不可用（可能已被系统清理或任务已丢弃）。目标源码不会自动回退为工作区。");
            }
        }

        private static void EnsureSandboxPathBaseSupport(string webProjectFilePath)
        {
            var hostDirectory = Path.GetDirectoryName(webProjectFilePath)
                ?? throw new InvalidOperationException("无法获取 Sandbox 预览宿主目录。");
            var supportsPathBase = Directory.EnumerateFiles(hostDirectory, "*.cs", SearchOption.TopDirectoryOnly)
                .Any(path => File.ReadAllText(path).Contains("NCF_XNCF_PREVIEW_PATH_BASE", StringComparison.Ordinal));
            if (!supportsPathBase)
            {
                throw new InvalidOperationException(
                    "目标 Senparc.Web 尚未声明 NCF_XNCF_PREVIEW_PATH_BASE 支持，不能安全挂载到 Sandbox 代理前缀。请先按当前 Senparc.Web 的 Program.cs 增加 opt-in 中间件后再预览。");
            }
        }

        private static XncfDevelopmentJobInfo ToInfo(XncfDevelopmentJobSnapshot snapshot)
        {
            return new XncfDevelopmentJobInfo
            {
                JobId = snapshot.JobId,
                OwnerAdminUserId = snapshot.OwnerAdminUserId,
                Mode = snapshot.Mode,
                ModuleProjectName = snapshot.ModuleProjectName,
                TargetSolutionFilePath = snapshot.TargetSolutionFilePath,
                WorkspaceRootPath = snapshot.WorkspaceRootPath,
                WorkspaceSolutionFilePath = snapshot.WorkspaceSolutionFilePath,
                Requirement = snapshot.Requirement,
                Stage = snapshot.Stage,
                StatusMessage = snapshot.StatusMessage,
                ErrorMessage = snapshot.ErrorMessage,
                TargetModuleFingerprint = snapshot.TargetModuleFingerprint,
                WorkspaceModuleFingerprint = snapshot.WorkspaceModuleFingerprint,
                ValidationSummary = snapshot.ValidationSummary,
                DiffSummary = snapshot.DiffSummary,
                PreviewSessionId = snapshot.PreviewSessionId,
                SandboxSessionId = snapshot.SandboxSessionId,
                PreviewUrl = snapshot.PreviewUrl,
                CreatedAt = snapshot.CreatedAt,
                UpdatedAt = snapshot.UpdatedAt,
                CompletedAt = snapshot.CompletedAt,
                MergeRequestedAt = snapshot.MergeRequestedAt,
                AppliedAt = snapshot.AppliedAt
            };
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
            return value[..maxLength] + "…";
        }
    }
}
