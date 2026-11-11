/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ConnectorAIFunction.cs
    文件功能描述：模块功能与本地化支持
    
    创建标识：Senparc - 20261110
    
    修改标识：Senparc - 20261111
    修改描述：v0.19.3 完善 AgentsManager 模板、ChatGroup 与连接器能力兼容

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Senparc.Xncf.Connectors.Abstractions;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

internal sealed class ConnectorAIFunction : AIFunction
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IConnectorGateway _gateway;
    private readonly ConfiguredConnector _connector;
    private readonly ConnectorActionDescriptor _action;
    private readonly JsonElement _jsonSchema;

    public ConnectorAIFunction(
        IConnectorGateway gateway,
        ConfiguredConnector connector,
        ConnectorActionDescriptor action)
    {
        _gateway = gateway;
        _connector = connector;
        _action = action;
        _jsonSchema = BuildSchema(action);
    }

    public override string Name => Sanitize($"connector_{_connector.InstanceId}_{_action.Key}");

    public override string Description =>
        $"使用连接器“{_connector.Name}”（{_connector.ConnectorName}）执行“{_action.Name}”。{_action.Description}";

    public override JsonElement JsonSchema => _jsonSchema;

    public override JsonElement? ReturnJsonSchema =>
        JsonDocument.Parse("{\"type\":\"string\"}").RootElement.Clone();

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in arguments)
        {
            values[pair.Key] = ConvertToString(pair.Value);
        }

        Console.WriteLine($"[Connector] agent tool {Name} instance={_connector.InstanceId}");
        var result = await _gateway.InvokeAsync(
            _connector.InstanceId,
            _action.Key,
            values,
            cancellationToken).ConfigureAwait(false);
        return result.Success ? result.Output : result.Error ?? "连接器调用失败。";
    }

    private static JsonElement BuildSchema(ConnectorActionDescriptor action)
    {
        var properties = new Dictionary<string, object?>();
        var requiredKeys = new List<string>();
        foreach (var parameter in action.Parameters)
        {
            properties[parameter.Key] = new
            {
                type = "string",
                description = string.IsNullOrWhiteSpace(parameter.Description)
                    ? parameter.Label
                    : $"{parameter.Label}。{parameter.Description}"
            };
            if (parameter.Required)
            {
                requiredKeys.Add(parameter.Key);
            }
        }

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = requiredKeys,
            ["additionalProperties"] = false
        }, SerializerOptions));
        return document.RootElement.Clone();
    }

    private static string Sanitize(string value)
    {
        var chars = value.Select(item => char.IsLetterOrDigit(item) ? item : '_').ToArray();
        return new string(chars);
    }

    private static string ConvertToString(object value)
        => value switch
        {
            null => string.Empty,
            string text => text,
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonElement element when element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            JsonElement element => element.ToString(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
}
