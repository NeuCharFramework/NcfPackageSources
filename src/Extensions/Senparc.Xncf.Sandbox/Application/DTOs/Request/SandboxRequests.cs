/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxRequests.cs
    File description: Related implementation.
    -

    Created by: Senparc - 20260808

    Modified by: Senparc - 20260817
    Change description: v0.2.0 enhance the jupyter-csharp template and sandbox session management

    Modified by: Senparc - 20260817
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

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

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Services;
using Senparc.Xncf.Sandbox;
using Senparc.Ncf.XncfBase.Functions.Files;

namespace Senparc.Xncf.Sandbox.Application.DTOs.Request;

public class Sandbox_CreateRequest : FunctionAppRequestBase
{
    [Required]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Template", "Parameter.Sandbox.Template.Help")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(TemplateOptions))]
    public string TemplateKey { get; set; } = string.Empty;

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public SelectionList TemplateOptions { get; } = new SelectionList(SelectionType.DropDownList, new[]
    {
        new SelectionItem(SandboxTemplateKeys.PythonExec, "Python Exec", "短任务执行 Python（Docker）", true),
        new SelectionItem(SandboxTemplateKeys.CsharpExec, "C# Exec", "短任务执行 C#（Docker SDK 镜像）"),
        new SelectionItem(SandboxTemplateKeys.JupyterPython, "JupyterLab Python", "交互式 Python Notebook（较耗内存）"),
        new SelectionItem(SandboxTemplateKeys.JupyterCsharp, "JupyterLab C#", "交互式 C# Notebook（需先构建/配置镜像）")
    });

    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Runtime", "Parameter.Sandbox.Runtime.Help")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(RuntimeOptions))]
    public string RuntimeKind { get; set; } = nameof(SandboxRuntimeKind.Docker);

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public SelectionList RuntimeOptions { get; } = new SelectionList(SelectionType.DropDownList, new[]
    {
        new SelectionItem(nameof(SandboxRuntimeKind.Docker), "Docker", "需要本机 Docker", true),
        new SelectionItem(nameof(SandboxRuntimeKind.Wasm), "Wasm (Stub)", "一期占位，尚未可用")
    });

    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.TtlMinutes", "Parameter.Sandbox.TtlMinutes.Help")]
    public int? TtlMinutes { get; set; }

    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.KeepAlive", "Parameter.Sandbox.KeepAlive.Help")]
    public bool KeepAlive { get; set; }

    [MaxLength(128)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Alias", "Parameter.Sandbox.Alias.Help")]
    public string Alias { get; set; } = string.Empty;

    [MaxLength(512)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.ExtraPortMappings", "Parameter.Sandbox.ExtraPortMappings.Help")]
    public string ExtraPortMappings { get; set; } = string.Empty;
}

public class Sandbox_SessionIdRequest : FunctionAppRequestBase
{
    [Required]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.SessionId", "Parameter.Sandbox.SessionId.Help")]
    public string SessionId { get; set; } = string.Empty;
}

public class Sandbox_ExecRequest : FunctionAppRequestBase
{
    [Required]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.SessionId", "Parameter.Sandbox.SessionId.Help")]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [LocalizedDescription(typeof(SandboxResource), "Frontend.Sandbox.Code", "Frontend.Sandbox.ShortTaskSourceCodePythonExamplePrintHelloFromNCFSandboxC")]
    public string Code { get; set; } = SandboxExecCodeDefaults.PythonHello;
}

public class Sandbox_LabCommandRequest : FunctionAppRequestBase
{
    [Required]
    [MaxLength(64)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.SessionId", "Parameter.Sandbox.SessionId.Help")]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(8000)]
    [LocalizedDescription(typeof(SandboxResource), "Frontend.Sandbox.Command", "Frontend.Sandbox.CommandExecutedThroughBinShLcInsideTheLabContainerTheWorking")]
    public string Command { get; set; } = string.Empty;

    [MaxLength(512)]
    [LocalizedDescription(typeof(SandboxResource), "Frontend.Sandbox.WorkingDirectory", "Frontend.Sandbox.DirectoryRelativeToTheLabWorkspaceLeaveEmptyForTheWorkspaceRoot")]
    public string WorkingDirectory { get; set; } = string.Empty;

    [LocalizedDescription(typeof(SandboxResource), "Frontend.Sandbox.TimeoutSeconds", "Frontend.Sandbox.MaximumDurationOfOneCommand30SecondsByDefaultAnd120Seconds")]
    public int TimeoutSeconds { get; set; } = 30;

    [MaxLength(32_768)]
    [LocalizedDescription(typeof(SandboxResource), "Frontend.Sandbox.StandardInput", "Frontend.Sandbox.OptionalWhenProvidedTheCommandRunsWithDockerExecIAndThe")]
    public string StdinContent { get; set; } = string.Empty;
}

public class Sandbox_LabUploadFileRequest : FunctionAppRequestBase
{
    [Required]
    [MaxLength(64)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.SessionId", "Parameter.Sandbox.SessionId.Help")]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.RelativePath", "Parameter.Sandbox.RelativePath.Help")]
    public string RelativePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(4_194_304)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Base64", "Parameter.Sandbox.Base64.Help")]
    public string ContentBase64 { get; set; } = string.Empty;

    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Overwrite", "Parameter.Sandbox.Overwrite.Help")]
    public bool Overwrite { get; set; } = true;
}

public class Sandbox_LabUploadFilesRequest : FunctionAppRequestBase
{
    [Required]
    [MaxLength(64)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.SessionId", "Parameter.Sandbox.SessionId.Help")]
    public string SessionId { get; set; } = string.Empty;

    [MaxLength(512)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.RelativeDirectory", "Parameter.Sandbox.RelativeDirectory.Help")]
    public string RelativeDirectory { get; set; } = string.Empty;

    [Required]
    [FunctionParameterUi(ParameterType.File, AllowMultiple = true, MaxFiles = 16)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Files", "Parameter.Sandbox.Files.Help")]
    public FunctionFileReference[] Files { get; set; } = Array.Empty<FunctionFileReference>();

    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Overwrite", "Parameter.Sandbox.Overwrite.Help")]
    public bool Overwrite { get; set; } = true;
}

public class Sandbox_LabFileRequest : FunctionAppRequestBase
{
    [Required]
    [MaxLength(64)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.SessionId", "Parameter.Sandbox.SessionId.Help")]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.RelativePath", "Parameter.Sandbox.RelativePath.Help")]
    public string RelativePath { get; set; } = string.Empty;

    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.MaxBytes", "Parameter.Sandbox.MaxBytes.Help")]
    public long MaxBytes { get; set; }
}

public class Sandbox_LabListFilesRequest : FunctionAppRequestBase
{
    [Required]
    [MaxLength(64)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.SessionId", "Parameter.Sandbox.SessionId.Help")]
    public string SessionId { get; set; } = string.Empty;

    [MaxLength(512)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.ListDirectory", "Parameter.Sandbox.ListDirectory.Help")]
    public string RelativeDirectory { get; set; } = string.Empty;

    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Recursive", "Parameter.Sandbox.Recursive.Help")]
    public bool Recursive { get; set; }

    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.MaxItems", "Parameter.Sandbox.MaxItems.Help")]
    public int MaxItems { get; set; }
}

public class Sandbox_LabCreateNotebookRequest : FunctionAppRequestBase
{
    [Required]
    [MaxLength(64)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.SessionId", "Parameter.Sandbox.SessionId.Help")]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.NotebookPath", "Parameter.Sandbox.NotebookPath.Help")]
    public string RelativePath { get; set; } = string.Empty;

    [Required]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Language", "Parameter.Sandbox.Language.Help")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(LanguageOptions))]
    public string Language { get; set; } = string.Empty;

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public SelectionList LanguageOptions { get; } = new SelectionList(SelectionType.DropDownList, new[]
    {
        new SelectionItem("python", "Python 3", "标准 Python 3 内核", true),
        new SelectionItem("csharp", "C# .NET SDK", "dotnet-interactive C# 内核（需 C# Jupyter 镜像）")
    });

    [MaxLength(200)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Title", "Parameter.Sandbox.Title.Help")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(100_000)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Cells", "Parameter.Sandbox.Cells.Help")]
    public string Cells { get; set; } = string.Empty;

    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Overwrite", "Parameter.Sandbox.Overwrite.Help")]
    public bool Overwrite { get; set; } = true;
}

public class Sandbox_UpdateAliasRequest : FunctionAppRequestBase
{
    [Required]
    [MaxLength(64)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.SessionId", "Parameter.Sandbox.SessionId.Help")]
    public string SessionId { get; set; } = string.Empty;

    [MaxLength(128)]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Alias", "Parameter.Sandbox.Alias.Help")]
    public string Alias { get; set; } = string.Empty;
}

public class Sandbox_ListRequest : FunctionAppRequestBase
{
}

public class Sandbox_UpdateTtlRequest : FunctionAppRequestBase
{
    [Required]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.SessionId", "Parameter.Sandbox.SessionId.Help")]
    public string SessionId { get; set; } = string.Empty;

    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.TtlMinutes", "Parameter.Sandbox.TtlMinutes.Help")]
    public int? TtlMinutes { get; set; }

    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.KeepAlive", "Parameter.Sandbox.KeepAlive.Help")]
    public bool KeepAlive { get; set; }
}
