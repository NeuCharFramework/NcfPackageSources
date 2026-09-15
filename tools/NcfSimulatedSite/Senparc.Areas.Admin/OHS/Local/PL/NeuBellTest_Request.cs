/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellTest_Request.cs
    文件功能描述：纽铃可见提醒示例 Function 请求参数


    创建标识：Senparc - 20260805

    修改标识：Senparc - 20260808
    修改描述：v0.4.0 新增纽铃示例 Function 请求参数

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

    修改标识：Senparc - 20260910
    修改描述：新增 WebHookUrl 参数，发送提醒（创建 NeuBell）时可指定 WebHook 地址异步通知，
    请求数据与结果记录到 WebHook 请求日志列表

    修改标识：Senparc - 20260914
    修改描述：新增 WebHookMethod 参数（GET/POST/PUT），WebHook 地址支持 {{title}} 等占位符

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Senparc.Areas.Admin.OHS.PL;

public sealed class NeuBellTest_Request : FunctionAppRequestBase
{
    public const string SendAction = "send";
    public const string ConsumeOneAction = "consume-one";
    public const string ConsumeAllAction = "consume-all";

    [Required]
    [Description("操作")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(ActionOptions))]
    public string Action { get; set; } = SendAction;

    [Description("WebHook 地址（可选），支持 {{title}} 等占位符，渲染值自动 URL 编码")]
    [MaxLength(1000)]
    public string WebHookUrl { get; set; }

    [Description("WebHook 请求方式（可选）：POST / GET / PUT，默认 POST；GET 不发送请求体，数据通过地址占位符传递")]
    [MaxLength(10)]
    public string WebHookMethod { get; set; }

    [JsonIgnore]
    public SelectionList ActionOptions { get; set; } = new(
        SelectionType.DropDownList,
        [
            new SelectionItem(SendAction, "发送提醒", "新增一条可在 Footer 弹窗和徽标中看到的测试提醒。", true),
            new SelectionItem(ConsumeOneAction, "消费最新一条", "只消费最近由此 Function 发送的一条测试提醒。"),
            new SelectionItem(ConsumeAllAction, "消费全部提醒", "清除当前订阅下所有由此 Function 发送的测试提醒。")
        ]);
}
