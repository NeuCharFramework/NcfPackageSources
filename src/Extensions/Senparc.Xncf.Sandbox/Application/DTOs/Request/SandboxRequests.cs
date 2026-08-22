/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SandboxRequests.cs
    文件功能描述：SandboxRequests.cs 功能实现
    
    
    创建标识：Senparc - 20260808
    
    修改标识：Senparc - 20260817
    修改描述：v0.2.0 增强 jupyter-csharp 模板与沙箱会话管理
-

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 增加 TTL 与永久保持请求参数

----------------------------------------------------------------*/

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Services;

namespace Senparc.Xncf.Sandbox.Application.DTOs.Request;

public class Sandbox_CreateRequest : FunctionAppRequestBase
{
    [Required]
    [Description("模板||选择沙箱模板（下拉一项即可）")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(TemplateOptions))]
    public string TemplateKey { get; set; } = SandboxTemplateKeys.PythonExec;

    /// <summary>
    /// 下拉数据源：只读 + JsonIgnore，避免前端 JSON 反序列化 SelectionList 失败。
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

    [Description("运行时||一期请选 Docker；Wasm 尚未可用")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(RuntimeOptions))]
    public string RuntimeKind { get; set; } = nameof(SandboxRuntimeKind.Docker);

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public SelectionList RuntimeOptions { get; } = new SelectionList(SelectionType.DropDownList, new[]
    {
        new SelectionItem(nameof(SandboxRuntimeKind.Docker), "Docker", "需要本机 Docker", true),
        new SelectionItem(nameof(SandboxRuntimeKind.Wasm), "Wasm (Stub)", "一期占位，尚未可用")
    });

    [Description("TTL（分钟）||留空时保持模板当前默认值；正整数最多 240 分钟")]
    public int? TtlMinutes { get; set; }

    [Description("永久保持||勾选后不自动过期；须由管理员手动销毁")]
    public bool KeepAlive { get; set; }
}

public class Sandbox_SessionIdRequest : FunctionAppRequestBase
{
    [Required]
    [Description("SessionId")]
    public string SessionId { get; set; } = string.Empty;
}

public class Sandbox_ExecRequest : FunctionAppRequestBase
{
    [Required]
    [Description("SessionId")]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [Description("代码||短任务源码。Python 示例：print('hello from NCF Sandbox')；C# 示例：Console.WriteLine(\"hello from NCF Sandbox\");（C# 字符串须用双引号）")]
    public string Code { get; set; } = SandboxExecCodeDefaults.PythonHello;
}

public class Sandbox_LabCommandRequest : FunctionAppRequestBase
{
    [Required]
    [MaxLength(64)]
    [Description("SessionId||必须是运行中的 JupyterLab 会话")]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(8000)]
    [Description("命令||在 Lab 容器内通过 /bin/sh -lc 执行的命令；工作目录限制在 Lab 工作区")]
    public string Command { get; set; } = string.Empty;

    [MaxLength(512)]
    [Description("工作目录||相对于 Lab 工作区的目录，留空表示工作区根目录")]
    public string WorkingDirectory { get; set; } = string.Empty;

    [Description("超时（秒）||单次命令最长执行时间，默认 30 秒，最多 120 秒")]
    public int TimeoutSeconds { get; set; } = 30;
}

public class Sandbox_LabUploadFileRequest : FunctionAppRequestBase
{
    [Required]
    [MaxLength(64)]
    [Description("SessionId||必须是运行中的 JupyterLab 会话")]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    [Description("工作区文件路径||只能填写工作区内的相对路径，例如 data/input.json")]
    public string RelativePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(4_194_304)]
    [Description("Base64 内容||文件内容经过 Base64 编码，单个文件最多约 3 MB")]
    public string ContentBase64 { get; set; } = string.Empty;

    [Description("覆盖已有文件||关闭后，目标文件存在时操作失败")]
    public bool Overwrite { get; set; } = true;
}

public class Sandbox_LabFileRequest : FunctionAppRequestBase
{
    [Required]
    [MaxLength(64)]
    [Description("SessionId||必须是运行中的 JupyterLab 会话")]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    [Description("工作区文件路径||只能填写工作区内的相对路径")]
    public string RelativePath { get; set; } = string.Empty;

    [Description("读取上限（字节）||留空或 0 使用系统默认上限")]
    public long MaxBytes { get; set; }
}

public class Sandbox_LabListFilesRequest : FunctionAppRequestBase
{
    [Required]
    [MaxLength(64)]
    [Description("SessionId||必须是运行中的 JupyterLab 会话")]
    public string SessionId { get; set; } = string.Empty;

    [MaxLength(512)]
    [Description("工作区目录||相对于 Lab 工作区的目录，留空表示根目录")]
    public string RelativeDirectory { get; set; } = string.Empty;

    [Description("递归列举||是否递归列举子目录")]
    public bool Recursive { get; set; }

    [Description("最多返回数量||留空或 0 使用系统默认上限")]
    public int MaxItems { get; set; }
}

public class Sandbox_ListRequest : FunctionAppRequestBase
{
}

public class Sandbox_UpdateTtlRequest : FunctionAppRequestBase
{
    [Required]
    [Description("SessionId")]
    public string SessionId { get; set; } = string.Empty;

    [Description("TTL（分钟）||从当前时刻重新计算；正整数最多 240 分钟")]
    public int? TtlMinutes { get; set; }

    [Description("永久保持||勾选后不自动过期；须由管理员手动销毁")]
    public bool KeepAlive { get; set; }
}
