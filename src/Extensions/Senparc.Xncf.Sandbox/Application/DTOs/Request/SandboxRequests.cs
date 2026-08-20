using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Services;

namespace Senparc.Xncf.Sandbox.Application.DTOs.Request;

public class Sandbox_CreateRequest : FunctionAppRequestBase
{
    [Required]
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Template")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(TemplateOptions))]
    public string TemplateKey { get; set; } = SandboxTemplateKeys.PythonExec;

    /// <summary>
    /// 下拉数据源：只读 + JsonIgnore，避免前端 JSON 反序列化 SelectionList 失败。
    /// </summary>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public SelectionList TemplateOptions { get; } = new SelectionList(SelectionType.DropDownList, new[]
    {
        new SelectionItem(SandboxTemplateKeys.PythonExec, "Python Exec", SandboxResource.Get("Selection.Sandbox.PythonExec", "短任务执行 Python（Docker）"), true),
        new SelectionItem(SandboxTemplateKeys.CsharpExec, "C# Exec", SandboxResource.Get("Selection.Sandbox.CsharpExec", "短任务执行 C#（Docker SDK 镜像）")),
        new SelectionItem(SandboxTemplateKeys.JupyterPython, "JupyterLab Python", SandboxResource.Get("Selection.Sandbox.JupyterPython", "交互式 Notebook（较耗内存）"))
    });

    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Runtime")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(RuntimeOptions))]
    public string RuntimeKind { get; set; } = nameof(SandboxRuntimeKind.Docker);

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public SelectionList RuntimeOptions { get; } = new SelectionList(SelectionType.DropDownList, new[]
    {
        new SelectionItem(nameof(SandboxRuntimeKind.Docker), "Docker", SandboxResource.Get("Selection.Sandbox.Docker", "需要本机 Docker"), true),
        new SelectionItem(nameof(SandboxRuntimeKind.Wasm), "Wasm (Stub)", SandboxResource.Get("Selection.Sandbox.Wasm", "一期占位，尚未可用"))
    });
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
    [LocalizedDescription(typeof(SandboxResource), "Parameter.Sandbox.Code")]
    public string Code { get; set; } = SandboxExecCodeDefaults.PythonHello;
}

public class Sandbox_ListRequest : FunctionAppRequestBase
{
}
