using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>
/// 将 XNCF Function 的通用参数元数据转换为 Workflow Designer 可消费的最小 UI 契约。
/// 该契约由 Workflow 模块维护，避免反向依赖 Admin/NeuCharPivot 的页面模型。
/// </summary>
public static class WorkflowFunctionSchemaBuilder
{
    public static List<WorkflowFunctionParameterSchema> Build(NeuCharFunctionDescriptor descriptor) =>
        descriptor.Parameters.Select(z => new WorkflowFunctionParameterSchema
        {
            Name = z.Name,
            Title = z.Title,
            Description = z.Description,
            Required = z.IsRequired,
            ParameterType = (int)z.ParameterType,
            SystemType = z.SystemType,
            MaxLength = z.MaxLength,
            Filterable = z.Filterable,
            AllowCreate = z.AllowCreate,
            DefaultValue = z.ParameterType == ParameterType.Password ? null : z.Value,
            Options = z.SelectionList?.Items?.Select(item => new WorkflowFunctionParameterOption(
                item.Value,
                item.Text,
                item.Note,
                item.DefaultSelected)).ToList() ?? new List<WorkflowFunctionParameterOption>()
        }).ToList();

    public static Dictionary<string, object> BuildDefaults(IEnumerable<WorkflowFunctionParameterSchema> parameters)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters)
        {
            object value = parameter.DefaultValue;
            if (parameter.ParameterType == (int)ParameterType.CheckBoxList)
            {
                value = parameter.Options.Where(z => z.DefaultSelected).Select(z => z.Value).ToArray();
            }
            else if (parameter.ParameterType == (int)ParameterType.DropDownList && value == null)
            {
                value = parameter.Options.FirstOrDefault(z => z.DefaultSelected)?.Value
                    ?? parameter.Options.FirstOrDefault()?.Value;
            }
            else if (parameter.ParameterType == (int)ParameterType.CheckBox && value == null)
            {
                value = false;
            }
            result[parameter.Name] = value;
        }
        return result;
    }
}

public sealed class WorkflowFunctionParameterSchema
{
    public string Name { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool Required { get; set; }
    public int ParameterType { get; set; }
    public string SystemType { get; set; }
    public int MaxLength { get; set; }
    public bool Filterable { get; set; }
    public bool AllowCreate { get; set; }
    public object DefaultValue { get; set; }
    public List<WorkflowFunctionParameterOption> Options { get; set; } = new();
}

public sealed record WorkflowFunctionParameterOption(
    string Value,
    string Text,
    string Note,
    bool DefaultSelected);
