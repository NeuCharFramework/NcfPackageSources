/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: NeuBellTest_Request.cs
    File description: Related implementation.


    Created by: Senparc - 20260805

    Modified by: Senparc - 20260808
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.5.0 Updated module behavior and related support.

    Modified by: Senparc - 20260910
    Change description: Updated module behavior and related support.
    Legacy comment translated to English.

    Modified by: Senparc - 20260914
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20260916
    Change description: v0.9.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

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

    [Description("WebHook 地址||可选，支持 {{title}} 等占位符；地址中的占位符渲染值会自动 URL 编码")]
    [MaxLength(1000)]
    public string WebHookUrl { get; set; }

    [Description("WebHook 请求方式||可选；GET / POST / PUT，默认 POST。GET 不发送请求体，数据通过地址占位符传递")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(WebHookMethodOptions))]
    [MaxLength(10)]
    public string WebHookMethod { get; set; } = "POST";

    [Description("WebHook Body||可选的多行请求体模板。支持 {{payload}}（完整默认 JSON）、{{kind}}（事件类型）、{{action}} / {{actionName}} / {{actionStatus}}（本次动作、动作名称、success 或 empty）、{{operation}}（created / added / removed / changed / none）、{{operationStatus}}（创建 / 新增 / 移除 / 变更 / 无变化）、{{provider}}、{{providerName}}、{{time}}、{{moduleUid}}、{{tenantId}}、{{id}}、{{title}}、{{summary}}、{{link}}、{{status}}（条目严重级别）、{{count}}、{{updated}}、{{addedCount}}、{{removedCount}}、{{addedTitles}}、{{removedTitles}}。POST / PUT 默认发送系统 JSON 报文，GET 忽略此参数")]
    [FunctionParameterUi(ParameterType.TextArea)]
    [MaxLength(20000)]
    public string WebHookBody { get; set; }

    [JsonIgnore]
    public SelectionList ActionOptions { get; set; } = new(
        SelectionType.DropDownList,
        [
            new SelectionItem(SendAction, "发送提醒", "新增一条可在 Footer 弹窗和徽标中看到的测试提醒。", true),
            new SelectionItem(ConsumeOneAction, "消费最新一条", "只消费最近由此 Function 发送的一条测试提醒。"),
            new SelectionItem(ConsumeAllAction, "消费全部提醒", "清除当前订阅下所有由此 Function 发送的测试提醒。")
        ]);

    [JsonIgnore]
    public SelectionList WebHookMethodOptions { get; set; } = new(
        SelectionType.DropDownList,
        [
            new SelectionItem("POST", "POST", "携带请求体，默认发送 JSON 报文。", true),
            new SelectionItem("GET", "GET", "不发送请求体，数据通过地址占位符传递。"),
            new SelectionItem("PUT", "PUT", "携带请求体。")
        ]);
}
