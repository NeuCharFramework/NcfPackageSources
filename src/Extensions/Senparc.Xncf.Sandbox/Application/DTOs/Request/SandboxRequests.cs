using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.Sandbox.Abstractions;

namespace Senparc.Xncf.Sandbox.Application.DTOs.Request;

public class Sandbox_CreateRequest : FunctionAppRequestBase
{
    [Required]
    [Description("模板||选择沙箱模板（下拉一项即可）")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(TemplateOptions))]
    public string TemplateKey { get; set; } = SandboxTemplateKeys.PythonExec;

    /// <summary>下拉数据源；必须 JsonIgnore，否则会被当成第 2 个参数画到界面上。</summary>
    [JsonIgnore]
    public SelectionList TemplateOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[]
    {
        new SelectionItem(SandboxTemplateKeys.PythonExec, "Python Exec", "短任务执行 Python（Docker）", true),
        new SelectionItem(SandboxTemplateKeys.CsharpExec, "C# Exec", "短任务执行 C#（Docker SDK 镜像）"),
        new SelectionItem(SandboxTemplateKeys.JupyterPython, "JupyterLab Python", "交互式 Notebook（较耗内存）")
    });

    [Description("运行时||一期请选 Docker；Wasm 尚未可用")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(RuntimeOptions))]
    public string RuntimeKind { get; set; } = nameof(SandboxRuntimeKind.Docker);

    [JsonIgnore]
    public SelectionList RuntimeOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[]
    {
        new SelectionItem(nameof(SandboxRuntimeKind.Docker), "Docker", "需要本机 Docker", true),
        new SelectionItem(nameof(SandboxRuntimeKind.Wasm), "Wasm (Stub)", "一期占位，尚未可用")
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
    [Description("代码||短任务执行的源代码")]
    public string Code { get; set; } = "print('hello from NCF Sandbox')";
}

public class Sandbox_ListRequest : FunctionAppRequestBase
{
}
