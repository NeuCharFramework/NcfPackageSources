/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxAppService.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20260817
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

    Modified by: Senparc - 20260829
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20260829
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20260918
    Change description: v0.3.3 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.3.4 0.3.4 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261018
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261021
    Change description: v0.3.5 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.4.0 add bulk file upload functionality to Sandbox Lab

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

----------------------------------------------------------------*/

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.XncfBase.Functions.Files;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Application.DTOs.Request;
using Senparc.Xncf.Sandbox.Domain.Services;
using Senparc.Xncf.Sandbox.Domain.Services.Runtime;
using Senparc.Xncf.Sandbox;

namespace Senparc.Xncf.Sandbox.Application.AppServices;

/// <summary>
/// Function-rendered application service for isolated Sandbox creation,
/// execution, persistent workspace file operations, and session lifecycle.
/// </summary>
public class SandboxAppService : AppServiceBase
{
    private const int MaxReferencedFilesPerUpload = 16;
    private readonly SandboxOrchestrator _orchestrator;
    private readonly IFunctionFileStore _fileStore;

    /// <summary>
    /// Initializes the Sandbox application service.
    /// </summary>
    /// <param name="serviceProvider">Dependency-injection provider.</param>
    /// <param name="orchestrator">Sandbox session/runtime orchestrator.</param>
    /// <param name="fileStore">Function file store used by bulk workspace uploads.</param>
    public SandboxAppService(
        IServiceProvider serviceProvider,
        SandboxOrchestrator orchestrator,
        IFunctionFileStore fileStore)
        : base(serviceProvider)
    {
        _orchestrator = orchestrator;
        _fileStore = fileStore;
    }

    /// <summary>
    /// Creates an isolated Sandbox session from a template and reports its access information.
    /// </summary>
    /// <param name="request">The runtime, template, TTL, alias, and port mapping options.</param>
    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.Create.Name", "Function.Sandbox.Create.Description",
        typeof(Register), "Create sandbox", "Create an isolated sandbox session from a template.", AllowGlobalPivot = true)]
    public async Task<StringAppResponse> Create(Sandbox_CreateRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var runtime = Enum.TryParse<SandboxRuntimeKind>(request.RuntimeKind, true, out var kind)
                ? kind
                : SandboxRuntimeKind.Docker;

            logger.Append($"创建沙箱 Template={request.TemplateKey}, Runtime={runtime}");
            var info = await _orchestrator.CreateAsync(
                    ownerUserId: 0,
                    templateKey: request.TemplateKey,
                    preferredRuntime: runtime,
                    ttlMinutes: request.TtlMinutes,
                    keepAlive: request.KeepAlive,
                    alias: request.Alias,
                    extraPortMappings: request.ExtraPortMappings)
                .ConfigureAwait(false);
            logger.Append($"SessionId={info.SessionId}, Status={info.Status}");
            logger.Append(FormatTtl(info));
            if (!string.IsNullOrWhiteSpace(info.Alias))
            {
                logger.Append($"Alias={info.Alias}");
            }

            if (!string.IsNullOrWhiteSpace(info.ExtraPorts))
            {
                logger.Append($"ExtraPorts={info.ExtraPorts}");
            }

            if (!string.IsNullOrWhiteSpace(info.AccessUrl))
            {
                logger.Append($"AccessUrl={info.AccessUrl}");
            }

            response.Data = FormatSession(info);
            return null;
        });
    }

    /// <summary>
    /// Lists recent Sandbox sessions and their formatted status.
    /// </summary>
    /// <param name="request">The list request.</param>
    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.List.Name", "Function.Sandbox.List.Description",
        typeof(Register), "List sandboxes", "View recent sandbox sessions.")]
    public async Task<StringAppResponse> List(Sandbox_ListRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var list = await _orchestrator.ListAsync().ConfigureAwait(false);
            logger.Append($"共 {list.Count} 条");
            var sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.Append(FormatSession(item)).Append("<hr/>");
            }

            response.Data = sb.Length == 0 ? "暂无会话" : sb.ToString();
            return null;
        });
    }

    /// <summary>
    /// Returns the status of one Sandbox session.
    /// </summary>
    /// <param name="request">The session identifier request.</param>
    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.Status.Name", "Function.Sandbox.Status.Description",
        typeof(Register), "Sandbox status", "Query a sandbox session by SessionId.")]
    public async Task<StringAppResponse> Status(Sandbox_SessionIdRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var info = await _orchestrator.GetAsync(request.SessionId).ConfigureAwait(false);
            if (info == null)
            {
                response.Success = false;
                response.ErrorMessage = "会话不存在";
                return null;
            }

            response.Data = FormatSession(info);
            return null;
        });
    }

    /// <summary>
    /// Executes code in an Exec-template Sandbox and returns bounded stdout/stderr.
    /// </summary>
    /// <param name="request">The session and code request.</param>
    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.Exec.Name", "Function.Sandbox.Exec.Description",
        typeof(Register), "Execute code", "Run a code snippet in an Exec template session.", AllowGlobalPivot = true)]
    public async Task<StringAppResponse> Exec(Sandbox_ExecRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var result = await _orchestrator.ExecAsync(request.SessionId, request.Code).ConfigureAwait(false);
            logger.Append($"ExitCode={result.ExitCode}");
            response.Data =
                $"ExitCode: {result.ExitCode}<br/><b>stdout</b><pre>{WebUtility.HtmlEncode(result.StdOut)}</pre><b>stderr</b><pre>{WebUtility.HtmlEncode(result.StdErr)}</pre>";
            return null;
        });
    }

    /// <summary>
    /// Executes a bounded interactive command in a persistent Lab workspace.
    /// </summary>
    /// <param name="request">The session, command, working directory, timeout, and stdin.</param>
    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.LabExec.Name", "Function.Sandbox.LabExec.Description",
        typeof(Register), "Execute Lab command", "Run a limited-duration Shell command in a running persistent JupyterLab workspace.", AllowAiInvocation = true)]
    public async Task<StringAppResponse> LabExec(Sandbox_LabCommandRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var result = await _orchestrator.ExecInteractiveAsync(
                    request.SessionId,
                    request.Command,
                    request.WorkingDirectory,
                    request.TimeoutSeconds,
                    request.StdinContent)
                .ConfigureAwait(false);
            logger.Append($"Lab SessionId={request.SessionId}, ExitCode={result.ExitCode}");
            response.Data = JsonSerializer.Serialize(new
            {
                sessionId = request.SessionId,
                exitCode = result.ExitCode,
                stdout = result.StdOut,
                stderr = result.StdErr
            });
            return null;
        });
    }

    /// <summary>
    /// Decodes Base64 content and writes one file into a persistent Lab workspace.
    /// </summary>
    /// <param name="request">The session, relative path, content, and overwrite options.</param>
    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.LabUploadFile.Name", "Function.Sandbox.LabUploadFile.Description",
        typeof(Register), "Upload Lab file", "Write Base64 file content into a running persistent JupyterLab workspace.", AllowAiInvocation = true)]
    public async Task<StringAppResponse> LabUploadFile(Sandbox_LabUploadFileRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            byte[] content;
            try
            {
                content = Convert.FromBase64String(request.ContentBase64 ?? string.Empty);
            }
            catch (FormatException)
            {
                response.Success = false;
                response.ErrorMessage = "ContentBase64 不是有效的 Base64 内容。";
                return null;
            }

            var file = await _orchestrator.UploadWorkspaceFileAsync(
                    request.SessionId,
                    request.RelativePath,
                    content,
                    request.Overwrite)
                .ConfigureAwait(false);
            logger.Append($"Lab file uploaded: SessionId={request.SessionId}, Path={file.RelativePath}, Bytes={file.Length}");
            response.Data = JsonSerializer.Serialize(new
            {
                sessionId = request.SessionId,
                file.RelativePath,
                file.Length,
                file.LastWriteTimeUtc
            });
            return null;
        });
    }

    /// <summary>
    /// Imports up to the service limit of FileManager-backed files into a persistent Lab workspace.
    /// </summary>
    /// <param name="request">The session, target directory, file references, and overwrite policy.</param>
    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.LabUploadFiles.Name", "Function.Sandbox.LabUploadFiles.Description",
        typeof(Register), "Upload Lab files", "Write multiple uploaded files into a running persistent JupyterLab workspace.", AllowAiInvocation = true)]
    public async Task<StringAppResponse> LabUploadFiles(Sandbox_LabUploadFilesRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            if (request.Files == null || request.Files.Length == 0)
            {
                response.Success = false;
                response.ErrorMessage = "Files 至少需要包含一个文件。";
                return null;
            }

            if (request.Files.Length > MaxReferencedFilesPerUpload)
            {
                response.Success = false;
                response.ErrorMessage = $"一次最多上传 {MaxReferencedFilesPerUpload} 个文件。";
                return null;
            }

            var relativeDirectory = (request.RelativeDirectory ?? string.Empty).Trim();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var uploaded = new List<object>(request.Files.Length);
            foreach (var fileReference in request.Files)
            {
                if (fileReference == null || string.IsNullOrWhiteSpace(fileReference.FileId))
                {
                    response.Success = false;
                    response.ErrorMessage = "Files 中包含无效的文件引用。";
                    return null;
                }

                await using var file = await _fileStore.OpenReadAsync(
                        fileReference.FileId,
                        CancellationToken.None)
                    .ConfigureAwait(false);
                var fileName = Path.GetFileName(file.Reference.FileName);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = file.Reference.FileId + ".bin";
                }

                if (!names.Add(fileName))
                {
                    response.Success = false;
                    response.ErrorMessage = $"批量上传中存在重复File name: {fileName}";
                    return null;
                }

                var relativePath = string.IsNullOrWhiteSpace(relativeDirectory)
                    ? fileName
                    : Path.Combine(relativeDirectory, fileName);
                var result = await _orchestrator.UploadWorkspaceFileAsync(
                        request.SessionId,
                        relativePath,
                        file.Content,
                        file.Reference.Length,
                        request.Overwrite,
                        CancellationToken.None)
                    .ConfigureAwait(false);
                uploaded.Add(new
                {
                    result.RelativePath,
                    result.Length,
                    result.LastWriteTimeUtc,
                    fileId = file.Reference.FileId,
                    fileName = fileName
                });
            }

            logger.Append($"Lab files uploaded: SessionId={request.SessionId}, Count={uploaded.Count}");
            response.Data = JsonSerializer.Serialize(new
            {
                sessionId = request.SessionId,
                files = uploaded
            });
            return null;
        });
    }

    /// <summary>
    /// Reads one bounded file from a persistent Lab workspace and returns Base64 content.
    /// </summary>
    /// <param name="request">The session, relative path, and maximum byte count.</param>
    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.LabDownloadFile.Name", "Function.Sandbox.LabDownloadFile.Description",
        typeof(Register), "Download Lab file", "Read a file from a running persistent JupyterLab workspace and return Base64 content.", AllowAiInvocation = true)]
    public async Task<StringAppResponse> LabDownloadFile(Sandbox_LabFileRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var file = await _orchestrator.ReadWorkspaceFileAsync(
                    request.SessionId,
                    request.RelativePath,
                    request.MaxBytes)
                .ConfigureAwait(false);
            logger.Append($"Lab file downloaded: SessionId={request.SessionId}, Path={file.File.RelativePath}, Bytes={file.File.Length}");
            response.Data = JsonSerializer.Serialize(new
            {
                sessionId = request.SessionId,
                file = new
                {
                    file.File.RelativePath,
                    file.File.Length,
                    file.File.LastWriteTimeUtc
                },
                contentBase64 = Convert.ToBase64String(file.Content)
            });
            return null;
        });
    }

    /// <summary>
    /// Lists persistent Lab workspace files and returns both flat entries and a tree representation.
    /// </summary>
    /// <param name="request">The session, directory, recursion, and item-limit options.</param>
    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.LabListFiles.Name", "Function.Sandbox.LabListFiles.Description",
        typeof(Register), "List Lab files", "List files in a running persistent JupyterLab workspace.", AllowAiInvocation = true)]
    public async Task<StringAppResponse> LabListFiles(Sandbox_LabListFilesRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var files = await _orchestrator.ListWorkspaceFilesAsync(
                    request.SessionId,
                    request.RelativeDirectory,
                    request.Recursive,
                    request.MaxItems)
                .ConfigureAwait(false);
            var tree = BuildWorkspaceFileTree(files, request.RelativeDirectory);
            var treeText = FormatWorkspaceFileTree(tree);

            logger.Append($"Lab files listed: SessionId={request.SessionId}, Count={files.Count}");
            foreach (var line in treeText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            {
                logger.Append(line);
            }

            response.Data = JsonSerializer.Serialize(new
            {
                sessionId = request.SessionId,
                directory = request.RelativeDirectory ?? string.Empty,
                recursive = request.Recursive,
                count = files.Count,
                tree,
                files
            }, LabListJsonOptions);
            return null;
        });
    }

    /// <summary>
    /// Validates notebook language/path, creates notebook JSON, and writes it into the Lab workspace.
    /// </summary>
    /// <param name="request">The session, notebook path, language, title, cells, and overwrite policy.</param>
    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.CreateNotebook.Name", "Function.Sandbox.CreateNotebook.Description",
        typeof(Register), "Create Notebook", "Create a Python or C# .ipynb file in a running JupyterLab workspace.", AllowAiInvocation = true)]
    public async Task<StringAppResponse> LabCreateNotebook(Sandbox_LabCreateNotebookRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var relativePath = (request.RelativePath ?? string.Empty).Trim();
            if (!relativePath.EndsWith(".ipynb", StringComparison.OrdinalIgnoreCase))
            {
                response.Success = false;
                response.ErrorMessage = "RelativePath 必须以 .ipynb 结尾。";
                return null;
            }

            if (string.IsNullOrWhiteSpace(request.Language))
            {
                response.Success = false;
                response.ErrorMessage = "Language 不能为空，请明确填写 python 或 csharp。";
                return null;
            }

            string language;
            try
            {
                language = SandboxNotebookBuilder.NormalizeLanguage(request.Language);
            }
            catch (InvalidOperationException ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;
                return null;
            }

            string notebookJson;
            try
            {
                notebookJson = SandboxNotebookBuilder.Build(language, request.Title, request.Cells);
            }
            catch (InvalidOperationException ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;
                return null;
            }

            var file = await _orchestrator.UploadWorkspaceFileAsync(
                    request.SessionId,
                    relativePath,
                    Encoding.UTF8.GetBytes(notebookJson),
                    request.Overwrite)
                .ConfigureAwait(false);
            logger.Append($"Notebook created: SessionId={request.SessionId}, Path={file.RelativePath}, Language={language}, Bytes={file.Length}");
            response.Data = JsonSerializer.Serialize(new
            {
                sessionId = request.SessionId,
                language,
                file.RelativePath,
                file.Length,
                file.LastWriteTimeUtc
            });
            return null;
        });
    }

    /// <summary>
    /// Sets or clears the display alias of a Sandbox session.
    /// </summary>
    /// <param name="request">The session identifier and alias value.</param>
    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.UpdateAlias.Name", "Function.Sandbox.UpdateAlias.Description",
        typeof(Register), "Update alias", "Set or clear the alias for a sandbox session.")]
    public async Task<StringAppResponse> UpdateAlias(Sandbox_UpdateAliasRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var info = await _orchestrator.UpdateAliasAsync(
                    request.SessionId,
                    request.Alias)
                .ConfigureAwait(false);
            logger.Append($"已更新 {info.SessionId} 的别名：{info.Alias ?? "(已清除)"}");
            response.Data = FormatSession(info);
            return null;
        });
    }

    /// <summary>
    /// Stops and cleans up a Sandbox runtime session.
    /// </summary>
    /// <param name="request">The session identifier.</param>
    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.Destroy.Name", "Function.Sandbox.Destroy.Description",
        typeof(Register), "Destroy sandbox", "Stop and clean up the specified session.", AllowGlobalPivot = true)]
    public async Task<StringAppResponse> Destroy(Sandbox_SessionIdRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            await _orchestrator.DestroyAsync(request.SessionId).ConfigureAwait(false);
            logger.Append($"已销毁 {request.SessionId}");
            response.Data = $"已销毁会话 {WebUtility.HtmlEncode(request.SessionId)}";
            return null;
        });
    }

    /// <summary>
    /// Permanently removes a terminal Sandbox session record after cleanup checks.
    /// </summary>
    /// <param name="request">The session identifier.</param>
    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.DeleteRecord.Name", "Function.Sandbox.DeleteRecord.Description",
        typeof(Register), "Delete sandbox record", "Permanently delete a stopped, expired, or fully cleaned session record.")]
    public async Task<StringAppResponse> DeleteRecord(Sandbox_SessionIdRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            await _orchestrator.DeleteRecordAsync(request.SessionId).ConfigureAwait(false);
            logger.Append($"已删除会话记录 {request.SessionId}");
            response.Data = $"已删除会话记录 {WebUtility.HtmlEncode(request.SessionId)}";
            return null;
        });
    }

    /// <summary>
    /// Updates Sandbox retention duration or switches the session to keep-alive mode.
    /// </summary>
    /// <param name="request">The session, TTL, and keep-alive options.</param>
    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.UpdateTtl.Name", "Function.Sandbox.UpdateTtl.Description",
        typeof(Register), "Update TTL", "Extend, shorten, or make the sandbox retention permanent.")]
    public async Task<StringAppResponse> UpdateTtl(Sandbox_UpdateTtlRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var info = await _orchestrator.UpdateTtlAsync(
                    request.SessionId,
                    request.TtlMinutes,
                    request.KeepAlive)
                .ConfigureAwait(false);
            logger.Append($"已更新 {info.SessionId} 的 {FormatTtl(info)}");
            response.Data = FormatSession(info);
            return null;
        });
    }

    /// <summary>
    /// Formats session status as HTML-safe diagnostic text for FunctionRender responses.
    /// </summary>
    /// <param name="info">The session information.</param>
    /// <returns>HTML-safe session summary text.</returns>
    private static string FormatSession(SandboxSessionInfo info)
    {
        return
            $"SessionId: {WebUtility.HtmlEncode(info.SessionId)}<br/>" +
            $"Alias: {WebUtility.HtmlEncode(info.Alias ?? "-")}<br/>" +
            $"Template: {WebUtility.HtmlEncode(info.TemplateKey)}<br/>" +
            $"Runtime: {info.RuntimeKind}<br/>" +
            $"Status: {info.Status}<br/>" +
            $"HostPort(loopback): {info.HostPort?.ToString() ?? "-"}<br/>" +
            $"ExtraPorts: {WebUtility.HtmlEncode(info.ExtraPorts ?? "-")}<br/>" +
            $"Url(proxy): {WebUtility.HtmlEncode(info.AccessUrl ?? "-")}<br/>" +
            $"{FormatTtl(info)}<br/>" +
            $"Message: {WebUtility.HtmlEncode(info.StatusMessage ?? "-")}";
    }

    /// <summary>
    /// Formats the session TTL state for user-facing output.
    /// </summary>
    /// <param name="info">The session information.</param>
    /// <returns>A keep-alive or UTC expiration message.</returns>
    private static string FormatTtl(SandboxSessionInfo info)
    {
        return info.IsTtlUnlimited
            ? "TTL: 永久保持（仅管理员销毁）"
            : $"Expires(UTC): {info.ExpiresAtUtc:u}";
    }

    /// <summary>
    /// Builds a nested directory/file tree from workspace-relative file entries.
    /// </summary>
    /// <param name="files">The workspace file entries.</param>
    /// <param name="relativeDirectory">The directory represented as the tree root.</param>
    /// <returns>A sorted workspace tree.</returns>
    internal static SandboxWorkspaceFileTreeNode BuildWorkspaceFileTree(
        IReadOnlyList<SandboxWorkspaceFileInfo> files,
        string? relativeDirectory)
    {
        var normalizedDirectory = SandboxWorkspacePaths.NormalizeRelativePath(
            relativeDirectory,
            allowEmpty: true);
        var root = new SandboxWorkspaceFileTreeNode
        {
            Name = normalizedDirectory.Length == 0 ? "." : normalizedDirectory,
            Type = "directory",
            Children = new List<SandboxWorkspaceFileTreeNode>()
        };

        foreach (var file in files)
        {
            var normalizedPath = SandboxWorkspacePaths.NormalizeRelativePath(file.RelativePath);
            var treePath = normalizedDirectory.Length == 0
                ? normalizedPath
                : normalizedPath.StartsWith(
                    normalizedDirectory + "/",
                    StringComparison.Ordinal)
                    ? normalizedPath[(normalizedDirectory.Length + 1)..]
                    : normalizedPath;
            var segments = treePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var current = root.Children!;

            for (var index = 0; index < segments.Length; index++)
            {
                var segment = segments[index];
                var isFile = index == segments.Length - 1;
                var node = current.FirstOrDefault(item =>
                    string.Equals(item.Name, segment, StringComparison.Ordinal));

                if (node == null)
                {
                    node = isFile
                        ? new SandboxWorkspaceFileTreeNode
                        {
                            Name = segment,
                            Type = "file",
                            Length = file.Length,
                            LastWriteTimeUtc = file.LastWriteTimeUtc
                        }
                        : new SandboxWorkspaceFileTreeNode
                        {
                            Name = segment,
                            Type = "directory",
                            Children = new List<SandboxWorkspaceFileTreeNode>()
                        };
                    current.Add(node);
                }

                if (!isFile)
                {
                    node.Children ??= new List<SandboxWorkspaceFileTreeNode>();
                    current = node.Children;
                }
            }
        }

        SortWorkspaceFileTree(root.Children!);
        return root;
    }

    /// <summary>
    /// Formats a workspace tree as an indented text representation.
    /// </summary>
    /// <param name="root">The tree root.</param>
    /// <returns>The formatted tree text.</returns>
    internal static string FormatWorkspaceFileTree(SandboxWorkspaceFileTreeNode root)
    {
        var builder = new StringBuilder();
        builder.Append(root.Name).Append('/').AppendLine();
        AppendWorkspaceFileTree(builder, root.Children ?? new List<SandboxWorkspaceFileTreeNode>(), string.Empty);
        return builder.ToString();
    }

    /// <summary>
    /// Sorts tree nodes with directories/files grouped deterministically and recurses into directories.
    /// </summary>
    /// <param name="nodes">The nodes to sort in place.</param>
    private static void SortWorkspaceFileTree(List<SandboxWorkspaceFileTreeNode> nodes)
    {
        nodes.Sort((left, right) =>
        {
            var typeCompare = string.Compare(
                left.Type,
                right.Type,
                StringComparison.OrdinalIgnoreCase);
            return typeCompare != 0
                ? typeCompare
                : string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        });

        foreach (var directory in nodes.Where(node =>
                     string.Equals(node.Type, "directory", StringComparison.Ordinal)))
        {
            if (directory.Children != null)
            {
                SortWorkspaceFileTree(directory.Children);
            }
        }
    }

    /// <summary>
    /// Appends one tree level to a text builder using branch markers.
    /// </summary>
    /// <param name="builder">The output builder.</param>
    /// <param name="nodes">The child nodes.</param>
    /// <param name="prefix">The visual indentation prefix.</param>
    private static void AppendWorkspaceFileTree(
        StringBuilder builder,
        IReadOnlyList<SandboxWorkspaceFileTreeNode> nodes,
        string prefix)
    {
        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            var isLast = index == nodes.Count - 1;
            builder.Append(prefix)
                .Append(isLast ? "`-- " : "|-- ")
                .Append(node.Name);
            if (string.Equals(node.Type, "directory", StringComparison.Ordinal))
            {
                builder.Append('/');
            }

            builder.AppendLine();
            if (node.Children is { Count: > 0 })
            {
                AppendWorkspaceFileTree(
                    builder,
                    node.Children,
                    prefix + (isLast ? "    " : "|   "));
            }
        }
    }

    private static readonly JsonSerializerOptions LabListJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal sealed class SandboxWorkspaceFileTreeNode
    {
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public long? Length { get; init; }
        public DateTime? LastWriteTimeUtc { get; init; }
        public List<SandboxWorkspaceFileTreeNode>? Children { get; set; }
    }
}
