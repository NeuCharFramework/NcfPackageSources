/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxAppService.cs
    文件功能描述：沙箱 Function / OHS 入口

    创建标识：Senparc - 20260808
    修改标识：Senparc - 20260817
    修改描述：v0.2.0 支持创建与更新沙箱会话 TTL/永久保持

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 增强沙箱预览、Jupyter 工作区与会话生命周期管理

    修改标识：Senparc - 20260829
    修改描述：v0.3.0 支持从全局 NeuCharPivot 受控调用 Sandbox Function

    修改标识：Senparc - 20260829
    修改描述：补充 Lab 文件列举的树状 Data 和执行日志

----------------------------------------------------------------*/

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Application.DTOs.Request;
using Senparc.Xncf.Sandbox.Domain.Services;
using Senparc.Xncf.Sandbox.Domain.Services.Runtime;

namespace Senparc.Xncf.Sandbox.Application.AppServices;

public class SandboxAppService : AppServiceBase
{
    private readonly SandboxOrchestrator _orchestrator;

    public SandboxAppService(IServiceProvider serviceProvider, SandboxOrchestrator orchestrator)
        : base(serviceProvider)
    {
        _orchestrator = orchestrator;
    }

    [FunctionRender(
        "创建沙箱",
        "按模板创建独立沙箱会话（Docker / Wasm Stub）",
        typeof(Register),
        AllowGlobalPivot = true)]
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
                    keepAlive: request.KeepAlive)
                .ConfigureAwait(false);
            logger.Append($"SessionId={info.SessionId}, Status={info.Status}");
            logger.Append(FormatTtl(info));
            if (!string.IsNullOrWhiteSpace(info.AccessUrl))
            {
                logger.Append($"AccessUrl={info.AccessUrl}");
            }

            response.Data = FormatSession(info);
            return null;
        });
    }

    [FunctionRender("沙箱列表", "查看最近沙箱会话", typeof(Register))]
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

    [FunctionRender("沙箱状态", "按 SessionId 查询", typeof(Register))]
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

    [FunctionRender(
        "执行代码",
        "在 Exec 模板会话中运行代码片段",
        typeof(Register),
        AllowGlobalPivot = true)]
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

    [FunctionRender("执行 Lab 命令", "在运行中的持久化 JupyterLab 容器工作区内执行受限时长的 Shell 命令", typeof(Register), AllowAiInvocation = true)]
    public async Task<StringAppResponse> LabExec(Sandbox_LabCommandRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var result = await _orchestrator.ExecInteractiveAsync(
                    request.SessionId,
                    request.Command,
                    request.WorkingDirectory,
                    request.TimeoutSeconds)
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

    [FunctionRender("上传 Lab 文件", "把 Base64 文件内容写入运行中的持久化 JupyterLab 工作区", typeof(Register), AllowAiInvocation = true)]
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

    [FunctionRender("下载 Lab 文件", "读取运行中的持久化 JupyterLab 工作区文件并返回 Base64 内容", typeof(Register), AllowAiInvocation = true)]
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

    [FunctionRender("列举 Lab 文件", "列举运行中的持久化 JupyterLab 工作区文件", typeof(Register), AllowAiInvocation = true)]
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

    [FunctionRender(
        "销毁沙箱",
        "停止并清理指定会话",
        typeof(Register),
        AllowGlobalPivot = true)]
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

    [FunctionRender("删除沙箱记录", "永久删除已停止、已过期或已清理完成的会话记录", typeof(Register))]
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

    [FunctionRender("修改 TTL", "延长、缩短或设为永久保持", typeof(Register))]
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

    private static string FormatSession(SandboxSessionInfo info)
    {
        return
            $"SessionId: {WebUtility.HtmlEncode(info.SessionId)}<br/>" +
            $"Template: {WebUtility.HtmlEncode(info.TemplateKey)}<br/>" +
            $"Runtime: {info.RuntimeKind}<br/>" +
            $"Status: {info.Status}<br/>" +
            $"HostPort(loopback): {info.HostPort?.ToString() ?? "-"}<br/>" +
            $"Url(proxy): {WebUtility.HtmlEncode(info.AccessUrl ?? "-")}<br/>" +
            $"{FormatTtl(info)}<br/>" +
            $"Message: {WebUtility.HtmlEncode(info.StatusMessage ?? "-")}";
    }

    private static string FormatTtl(SandboxSessionInfo info)
    {
        return info.IsTtlUnlimited
            ? "TTL: 永久保持（仅管理员销毁）"
            : $"Expires(UTC): {info.ExpiresAtUtc:u}";
    }

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

    internal static string FormatWorkspaceFileTree(SandboxWorkspaceFileTreeNode root)
    {
        var builder = new StringBuilder();
        builder.Append(root.Name).Append('/').AppendLine();
        AppendWorkspaceFileTree(builder, root.Children ?? new List<SandboxWorkspaceFileTreeNode>(), string.Empty);
        return builder.ToString();
    }

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
