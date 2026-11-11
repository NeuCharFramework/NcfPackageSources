/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ConnectorAppService.cs
    文件功能描述：连接器配置与调用的应用服务
    
    
    创建标识：Senparc - 20261111
    
    修改标识：Senparc - 20261111
    修改描述：v0.1.0 提供连接器配置与调用的应用服务

----------------------------------------------------------------*/

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.Connectors.Abstractions;
using Senparc.Xncf.Connectors.Domain.Services;

namespace Senparc.Xncf.Connectors.OHS.Local;

public sealed class ConnectorAppService : AppServiceBase
{
    private readonly IConnectorGateway _gateway;

    public ConnectorAppService(IServiceProvider serviceProvider, IConnectorGateway gateway)
        : base(serviceProvider)
    {
        _gateway = gateway;
    }

    [FunctionRender(
        typeof(ConnectorsResource),
        "Function.Connectors.Invoke.Name",
        "Function.Connectors.Invoke.Description",
        typeof(Register),
        "调用连接器",
        "调用一个已启用的连接器动作。在 Workflow 中添加 Function 节点后，选择连接器和动作即可。",
        AllowAiInvocation = true)]
    public Task<StringAppResponse> InvokeConfiguredConnector(InvokeConnectorRequest request)
    {
        return this.GetStringResponseAsync(async (_, logger) =>
        {
            var arguments = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["startTime"] = request.StartTime,
                ["endTime"] = request.EndTime,
                ["userIds"] = request.UserIds,
                ["checkinType"] = request.CheckinType
            };
            MergeArguments(arguments, request.ArgumentsJson);
            logger.Append($"[Connector] workflow invoke instance={request.InstanceId} action={request.ActionKey}");
            Console.WriteLine($"[Connector] workflow invoke instance={request.InstanceId} action={request.ActionKey}");
            var result = await _gateway.InvokeAsync(
                request.InstanceId,
                request.ActionKey,
                arguments,
                CancellationToken).ConfigureAwait(false);
            return result.Success
                ? result.Output
                : result.Error ?? "连接器调用失败。";
        });
    }

    private static void MergeArguments(Dictionary<string, string?> arguments, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            var extra = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (extra == null)
            {
                return;
            }

            foreach (var pair in extra)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key)
                    && ( !arguments.TryGetValue(pair.Key, out var current) || string.IsNullOrWhiteSpace(current)))
                {
                    arguments[pair.Key] = pair.Value;
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            arguments["argumentsJson"] = json;
        }
    }
}

public sealed class InvokeConnectorRequest : FunctionAppRequestBase
{
    [Required]
    [Description("连接器||选择已经配置并启用的连接器")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(InstanceOptions), Filterable = true)]
    public string InstanceId { get; set; } = string.Empty;

    [JsonIgnore]
    public SelectionList InstanceOptions { get; set; } = new(SelectionType.DropDownList);

    [Required]
    [Description("动作||选择要执行的连接器动作")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(ActionOptions), Filterable = true)]
    public string ActionKey { get; set; } = string.Empty;

    [JsonIgnore]
    public SelectionList ActionOptions { get; set; } = new(SelectionType.DropDownList);

    [MaxLength(40)]
    [Description("开始时间||打卡记录开始时间，例如 2026-11-01 或 2026-11-01 09:00。留空则从今天 0 点开始")]
    public string? StartTime { get; set; }

    [MaxLength(40)]
    [Description("结束时间||打卡记录结束时间。留空则到当前时间，且与开始时间最多间隔 30 天")]
    public string? EndTime { get; set; }

    [MaxLength(2000)]
    [Description("成员账号||企业微信 userid，多个用逗号或空格分隔")]
    public string? UserIds { get; set; }

    [MaxLength(10)]
    [Description("打卡类型||1 上下班，2 外出，3 全部。留空默认全部")]
    public string? CheckinType { get; set; }

    [MaxLength(4000)]
    [Description("附加参数||JSON 对象。已填写的专用字段优先，适合其他连接器动作")]
    public string? ArgumentsJson { get; set; }

    public override async Task LoadData(IServiceProvider serviceProvider)
    {
        await base.LoadData(serviceProvider).ConfigureAwait(false);
        var gateway = serviceProvider.GetService<IConnectorGateway>();
        if (gateway == null)
        {
            return;
        }

        var configured = await gateway.ListEnabledAsync(CancellationToken.None).ConfigureAwait(false);
        InstanceOptions.Items = configured
            .Select(item => new SelectionItem(item.InstanceId, $"{item.Name} · {item.ConnectorName}", item.Description))
            .ToList();
        ActionOptions.Items = configured
            .SelectMany(item => item.Actions.Select(action => new SelectionItem(
                action.Key,
                $"{item.ConnectorName} / {action.Name}",
                action.Description)))
            .GroupBy(item => item.Value, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }
}
