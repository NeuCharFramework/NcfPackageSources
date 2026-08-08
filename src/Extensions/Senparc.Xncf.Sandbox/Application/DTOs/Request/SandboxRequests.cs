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
        new SelectionItem(SandboxTemplateKeys.JupyterPython, "JupyterLab Python", "交互式 Notebook（较耗内存）")
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

public class Sandbox_ListRequest : FunctionAppRequestBase
{
}
