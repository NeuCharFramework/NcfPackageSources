/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FunctionRenderSchemaAIFunction.cs
    File description: Related implementation.

    Created by: Senparc - 20260918

    Modified by: Senparc - 20260917
    Change description: v0.18.0 Updated module behavior and related support.

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
