/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConnectorAppService.cs
    File description: application service for connector configuration and invocation

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 provide application services for connector configuration and invocation

----------------------------------------------------------------*/

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
    [LocalizedDescription(typeof(ConnectorsResource), "Parameter.Connectors.Instance")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(InstanceOptions), Filterable = true)]
    public string InstanceId { get; set; } = string.Empty;

    [JsonIgnore]
    public SelectionList InstanceOptions { get; set; } = new(SelectionType.DropDownList);

    [Required]
    [LocalizedDescription(typeof(ConnectorsResource), "Parameter.Connectors.Action")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(ActionOptions), Filterable = true)]
    public string ActionKey { get; set; } = string.Empty;

    [JsonIgnore]
    public SelectionList ActionOptions { get; set; } = new(SelectionType.DropDownList);

    [MaxLength(40)]
    [LocalizedDescription(typeof(ConnectorsResource), "Parameter.Connectors.StartTime")]
    public string? StartTime { get; set; }

    [MaxLength(40)]
    [LocalizedDescription(typeof(ConnectorsResource), "Parameter.Connectors.EndTime")]
    public string? EndTime { get; set; }

    [MaxLength(2000)]
    [LocalizedDescription(typeof(ConnectorsResource), "Parameter.Connectors.UserIds")]
    public string? UserIds { get; set; }

    [MaxLength(10)]
    [LocalizedDescription(typeof(ConnectorsResource), "Parameter.Connectors.CheckinType")]
    public string? CheckinType { get; set; }

    [MaxLength(4000)]
    [LocalizedDescription(typeof(ConnectorsResource), "Parameter.Connectors.Arguments")]
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
