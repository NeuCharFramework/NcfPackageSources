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
    修改描述：新增“发送 WebHook 测试”操作，可从 Function 直接向启用的 NeuBell WebHook 端点发送 test 事件

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
    public const string WebHookTestAction = "webhook-test";

    [Required]
    [Description("操作")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(ActionOptions))]
    public string Action { get; set; } = SendAction;

    [JsonIgnore]
    public SelectionList ActionOptions { get; set; } = new(
        SelectionType.DropDownList,
        [
            new SelectionItem(SendAction, "发送提醒", "新增一条可在 Footer 弹窗和徽标中看到的测试提醒。", true),
            new SelectionItem(ConsumeOneAction, "消费最新一条", "只消费最近由此 Function 发送的一条测试提醒。"),
            new SelectionItem(ConsumeAllAction, "消费全部提醒", "清除当前订阅下所有由此 Function 发送的测试提醒。"),
            new SelectionItem(WebHookTestAction, "发送 WebHook 测试", "向全部启用中的 NeuBell WebHook（WebAPI）端点发送一条 test 事件，用于验证连通性与签名配置。")
        ]);
}
