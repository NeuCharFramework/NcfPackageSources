/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FunctionRenderSchemaAIFunction.cs
    文件功能描述：Agent FunctionRender schema 元数据包装

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using System.Collections.Generic;
using System.Text.Json;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// Preserves the standard FunctionRender invocation while enriching its AI schema.
/// </summary>
internal sealed class FunctionRenderSchemaAIFunction : DelegatingAIFunction
{
    private readonly JsonElement _jsonSchema;

    public FunctionRenderSchemaAIFunction(
        AIFunction innerFunction,
        IReadOnlyList<FunctionParameterInfo> parameterInfos)
        : base(innerFunction)
    {
        _jsonSchema = FunctionRenderAiSchemaBuilder.ApplySelectionMetadata(
            innerFunction.JsonSchema,
            parameterInfos);
    }

    public override JsonElement JsonSchema => _jsonSchema;
}
