/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WorkflowFunctionSchemaBuilder.cs
    文件功能描述：领域服务与业务流程实现


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

----------------------------------------------------------------*/

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
        descriptor.Parameters.Select((z, index) =>
        {
            var hasSyntheticName = string.IsNullOrWhiteSpace(z.Name);
            return new WorkflowFunctionParameterSchema
            {
                // Name is the persisted configuration key. A bad third-party Function descriptor
                // must still be editable as a draft, but it cannot be executed until the module
                // restores its real field name.
                Name = hasSyntheticName ? $"parameter_{index + 1}" : z.Name.Trim(),
                Title = z.Title,
                Description = z.Description,
                Required = z.IsRequired,
                ParameterType = (int)z.ParameterType,
                SystemType = z.SystemType,
                MaxLength = z.MaxLength,
                Filterable = z.Filterable,
                AllowCreate = z.AllowCreate,
                HasSyntheticName = hasSyntheticName,
                MetadataError = hasSyntheticName
                    ? "Function 参数元数据缺少字段名；当前仅可保存草稿，修复或更新模块后才能运行。"
                    : null,
                DefaultValue = z.ParameterType == ParameterType.Password ? null : z.Value,
                Options = z.SelectionList?.Items?.Select(item => new WorkflowFunctionParameterOption(
                    item.Value,
                    item.Text,
                    item.Note,
                    item.DefaultSelected)).ToList() ?? new List<WorkflowFunctionParameterOption>()
            };
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
    public bool HasSyntheticName { get; set; }
    public string MetadataError { get; set; }
    public object DefaultValue { get; set; }
    public List<WorkflowFunctionParameterOption> Options { get; set; } = new();
}

public sealed record WorkflowFunctionParameterOption(
    string Value,
    string Text,
    string Note,
    bool DefaultSelected);
