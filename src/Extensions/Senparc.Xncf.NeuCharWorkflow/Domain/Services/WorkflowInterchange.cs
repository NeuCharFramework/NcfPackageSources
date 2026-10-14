using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

public enum WorkflowInterchangeFormat
{
    Json,
    Yaml
}

public static class WorkflowInterchangeConstants
{
    public const string Format = "ncf.workflow";
    public const int SchemaVersion = 1;
    public const int MaxDocumentLength = 1_000_000;
}

public sealed class NeuCharWorkflowDefinition
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = WorkflowInterchangeConstants.Format;

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = WorkflowInterchangeConstants.SchemaVersion;

    [JsonPropertyName("metadata")]
    public WorkflowDefinitionMetadata Metadata { get; set; } = new();

    [JsonPropertyName("compatibility")]
    public WorkflowDefinitionCompatibility Compatibility { get; set; } = new();

    [JsonPropertyName("trigger")]
    public WorkflowDefinitionTrigger Trigger { get; set; } = new();

    [JsonPropertyName("graph")]
    public NeuCharWorkflowGraph Graph { get; set; } = new();

    [JsonPropertyName("requirements")]
    public WorkflowDefinitionRequirements Requirements { get; set; } = new();

    [JsonPropertyName("secrets")]
    public List<WorkflowSecretRequirement> Secrets { get; set; } = new();
}

public sealed class WorkflowDefinitionMetadata
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}

public sealed class WorkflowDefinitionCompatibility
{
    [JsonPropertyName("minEngineVersion")]
    public string? MinEngineVersion { get; set; }
}

public sealed class WorkflowDefinitionTrigger
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "manual";

    [JsonPropertyName("config")]
    public JsonObject Config { get; set; } = new();
}

public sealed class WorkflowDefinitionRequirements
{
    [JsonPropertyName("functions")]
    public List<WorkflowFunctionRequirement> Functions { get; set; } = new();

    [JsonPropertyName("objects")]
    public List<WorkflowObjectRequirement> Objects { get; set; } = new();

    [JsonPropertyName("workflows")]
    public List<WorkflowReferenceRequirement> Workflows { get; set; } = new();
}

public sealed class WorkflowFunctionRequirement
{
    [JsonPropertyName("moduleUid")]
    public string ModuleUid { get; set; } = string.Empty;

    [JsonPropertyName("functionKey")]
    public string FunctionKey { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class WorkflowObjectRequirement
{
    [JsonPropertyName("providerId")]
    public string ProviderId { get; set; } = string.Empty;

    [JsonPropertyName("objectId")]
    public string ObjectId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class WorkflowReferenceRequirement
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("sourceId")]
    public int? SourceId { get; set; }
}

public sealed class WorkflowSecretRequirement
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("requiredOnImport")]
    public bool RequiredOnImport { get; set; } = true;
}

public sealed record WorkflowInterchangeDocument(
    NeuCharWorkflowDefinition Definition,
    WorkflowInterchangeFormat Format);

public static class WorkflowInterchangeCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public static WorkflowInterchangeDocument Parse(
        string content,
        WorkflowInterchangeFormat? format = null)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new WorkflowInterchangeException("Workflow 文件内容不能为空。");
        }

        if (content.Length > WorkflowInterchangeConstants.MaxDocumentLength)
        {
            throw new WorkflowInterchangeException("Workflow 文件不能超过 1000000 个字符。");
        }

        var actualFormat = format ?? DetectFormat(content);
        JsonNode root = actualFormat == WorkflowInterchangeFormat.Json
            ? ParseJson(content)
            : ParseYaml(content);

        var definition = ParseDefinition(root);
        ValidateDefinition(definition);
        return new WorkflowInterchangeDocument(definition, actualFormat);
    }

    public static string Serialize(
        NeuCharWorkflowDefinition definition,
        WorkflowInterchangeFormat format)
    {
        ValidateDefinition(definition);
        return format == WorkflowInterchangeFormat.Json
            ? JsonSerializer.Serialize(definition, JsonOptions)
            : YamlSerializer.Serialize(ToPlainObject(
                JsonSerializer.SerializeToNode(definition, JsonOptions)!));
    }

    public static void ValidateDefinition(NeuCharWorkflowDefinition? definition)
    {
        if (definition == null)
        {
            throw new WorkflowInterchangeException("Workflow Definition 不能为空。");
        }

        if (!string.Equals(definition.Format, WorkflowInterchangeConstants.Format, StringComparison.OrdinalIgnoreCase))
        {
            throw new WorkflowInterchangeException(
                $"Workflow 文件格式必须为 {WorkflowInterchangeConstants.Format}。");
        }

        if (definition.SchemaVersion != WorkflowInterchangeConstants.SchemaVersion)
        {
            throw new WorkflowInterchangeException(
                $"不支持的 Workflow Schema 版本：{definition.SchemaVersion}。当前版本为 {WorkflowInterchangeConstants.SchemaVersion}。");
        }

        definition.Metadata ??= new WorkflowDefinitionMetadata();
        definition.Compatibility ??= new WorkflowDefinitionCompatibility();
        definition.Trigger ??= new WorkflowDefinitionTrigger();
        definition.Trigger.Config ??= new JsonObject();
        definition.Graph ??= new NeuCharWorkflowGraph();
        definition.Requirements ??= new WorkflowDefinitionRequirements();
        definition.Secrets ??= new List<WorkflowSecretRequirement>();
        definition.Metadata.Tags ??= new List<string>();
        definition.Requirements.Functions ??= new List<WorkflowFunctionRequirement>();
        definition.Requirements.Objects ??= new List<WorkflowObjectRequirement>();
        definition.Requirements.Workflows ??= new List<WorkflowReferenceRequirement>();

        if (definition.Metadata.Name?.Trim().Length > 200)
        {
            throw new WorkflowInterchangeException("Workflow 名称不能超过 200 个字符。");
        }

        if (definition.Metadata.Description?.Length > 10_000)
        {
            throw new WorkflowInterchangeException("Workflow 说明不能超过 10000 个字符。");
        }

        if (definition.Metadata.Tags.Count > 50 ||
            definition.Metadata.Tags.Any(tag => tag?.Length > 100))
        {
            throw new WorkflowInterchangeException("Workflow 标签数量或长度超过限制。");
        }

        var triggerType = definition.Trigger.Type?.Trim().ToLowerInvariant();
        if (triggerType is not ("manual" or "interval" or "webhook" or "chat"))
        {
            throw new WorkflowInterchangeException("Workflow 触发方式只能是 manual、interval、webhook 或 chat。");
        }

        definition.Trigger.Type = triggerType;
    }

    private static NeuCharWorkflowDefinition ParseDefinition(JsonNode root)
    {
        if (root is not JsonObject rootObject)
        {
            throw new WorkflowInterchangeException("Workflow 文件根节点必须是对象。");
        }

        if (rootObject["format"] == null && rootObject["nodes"] != null)
        {
            var legacyGraph = root.Deserialize<NeuCharWorkflowGraph>(JsonOptions) ?? new NeuCharWorkflowGraph();
            return new NeuCharWorkflowDefinition
            {
                Metadata = new WorkflowDefinitionMetadata { Name = "Imported Workflow" },
                Graph = legacyGraph,
                Trigger = new WorkflowDefinitionTrigger
                {
                    Type = InferTriggerType(legacyGraph)
                }
            };
        }

        try
        {
            return root.Deserialize<NeuCharWorkflowDefinition>(JsonOptions)
                ?? throw new WorkflowInterchangeException("Workflow Definition 为空。");
        }
        catch (JsonException ex)
        {
            throw new WorkflowInterchangeException("Workflow Definition 结构无效。", ex);
        }
    }

    private static string InferTriggerType(NeuCharWorkflowGraph graph)
    {
        var trigger = graph.Nodes?.SingleOrDefault(node =>
            node.Type?.EndsWith("trigger", StringComparison.OrdinalIgnoreCase) == true);
        return trigger?.Type?.ToLowerInvariant() switch
        {
            "interval-trigger" => "interval",
            "webhook-trigger" => "webhook",
            "chat-trigger" => "chat",
            _ => "manual"
        };
    }

    private static JsonNode ParseJson(string content)
    {
        try
        {
            return JsonNode.Parse(content)
                ?? throw new WorkflowInterchangeException("JSON 文件为空。");
        }
        catch (JsonException ex)
        {
            throw new WorkflowInterchangeException("Workflow 文件不是有效的 JSON。", ex);
        }
    }

    private static WorkflowInterchangeFormat DetectFormat(string content)
    {
        var first = content.TrimStart();
        return first.StartsWith("{", StringComparison.Ordinal) ||
               first.StartsWith("[", StringComparison.Ordinal)
            ? WorkflowInterchangeFormat.Json
            : WorkflowInterchangeFormat.Yaml;
    }

    private static JsonNode ParseYaml(string content)
    {
        try
        {
            using var reader = new StringReader(content);
            var stream = new YamlStream();
            stream.Load(reader);
            if (stream.Documents.Count != 1)
            {
                throw new WorkflowInterchangeException("YAML Workflow 文件只能包含一个文档。");
            }

            return ConvertYamlNode(stream.Documents[0].RootNode);
        }
        catch (WorkflowInterchangeException)
        {
            throw;
        }
        catch (YamlException ex)
        {
            throw new WorkflowInterchangeException("Workflow 文件不是有效的 YAML。", ex);
        }
    }

    private static JsonNode ConvertYamlNode(YamlNode node)
    {
        if (node is YamlMappingNode mapping)
        {
            var result = new JsonObject();
            foreach (var pair in mapping.Children)
            {
                if (pair.Key is not YamlScalarNode key || string.IsNullOrWhiteSpace(key.Value))
                {
                    throw new WorkflowInterchangeException("Workflow YAML 对象键必须是非空文本。");
                }

                if (result.ContainsKey(key.Value))
                {
                    throw new WorkflowInterchangeException($"Workflow YAML 包含重复键：{key.Value}。");
                }

                result[key.Value] = ConvertYamlNode(pair.Value);
            }

            return result;
        }

        if (node is YamlSequenceNode sequence)
        {
            var result = new JsonArray();
            foreach (var child in sequence.Children)
            {
                result.Add(ConvertYamlNode(child));
            }

            return result;
        }

        if (node is YamlScalarNode scalar)
        {
            var tag = scalar.Tag.ToString();
            if (!string.IsNullOrWhiteSpace(tag) &&
                !tag.EndsWith(":str", StringComparison.OrdinalIgnoreCase) &&
                !tag.EndsWith(":int", StringComparison.OrdinalIgnoreCase) &&
                !tag.EndsWith(":float", StringComparison.OrdinalIgnoreCase) &&
                !tag.EndsWith(":bool", StringComparison.OrdinalIgnoreCase) &&
                !tag.EndsWith(":null", StringComparison.OrdinalIgnoreCase))
            {
                throw new WorkflowInterchangeException("Workflow YAML 不允许自定义类型标签。");
            }

            return ParseYamlScalar(scalar.Value, tag);
        }

        throw new WorkflowInterchangeException("Workflow YAML 包含不支持的节点类型。");
    }

    private static JsonNode? ParseYamlScalar(string? value, string? tag)
    {
        if (string.IsNullOrEmpty(value) || tag?.EndsWith(":null", StringComparison.OrdinalIgnoreCase) == true)
        {
            return null;
        }

        if (tag?.EndsWith(":bool", StringComparison.OrdinalIgnoreCase) == true &&
            bool.TryParse(value, out var boolean))
        {
            return JsonValue.Create(boolean);
        }

        if (tag?.EndsWith(":int", StringComparison.OrdinalIgnoreCase) == true &&
            long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
        {
            return JsonValue.Create(integer);
        }

        if (tag?.EndsWith(":float", StringComparison.OrdinalIgnoreCase) == true &&
            decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var decimalValue))
        {
            return JsonValue.Create(decimalValue);
        }

        if (bool.TryParse(value, out var implicitBoolean))
        {
            return JsonValue.Create(implicitBoolean);
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var implicitInteger))
        {
            return JsonValue.Create(implicitInteger);
        }

        if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var implicitDecimal))
        {
            return JsonValue.Create(implicitDecimal);
        }

        return JsonValue.Create(value);
    }

    private static object? ToPlainObject(JsonNode node)
    {
        return node switch
        {
            JsonObject obj => obj.ToDictionary(
                pair => pair.Key,
                pair => ToPlainObject(pair.Value),
                StringComparer.Ordinal),
            JsonArray array => array.Select(item => ToPlainObject(item)).ToList(),
            JsonValue value => GetJsonValue(value),
            _ => null
        };
    }

    private static object? GetJsonValue(JsonValue value)
    {
        if (value.TryGetValue<string>(out var text)) return text;
        if (value.TryGetValue<bool>(out var boolean)) return boolean;
        if (value.TryGetValue<int>(out var integer)) return integer;
        if (value.TryGetValue<long>(out var longValue)) return longValue;
        if (value.TryGetValue<decimal>(out var decimalValue)) return decimalValue;
        if (value.TryGetValue<double>(out var doubleValue)) return doubleValue;
        return value.ToJsonString();
    }
}

public sealed class WorkflowInterchangeException : InvalidOperationException
{
    public WorkflowInterchangeException(string message)
        : base(message) { }

    public WorkflowInterchangeException(string message, Exception innerException)
        : base(message, innerException) { }
}

public sealed record WorkflowImportIssue(
    string Severity,
    string Code,
    string Message,
    string? Path = null);

public sealed record WorkflowImportPreview(
    bool Valid,
    string Name,
    string TriggerType,
    IReadOnlyList<WorkflowImportIssue> Errors,
    IReadOnlyList<WorkflowImportIssue> Warnings,
    IReadOnlyList<WorkflowSecretRequirement> Secrets,
    NeuCharWorkflowDefinition? Definition = null);
