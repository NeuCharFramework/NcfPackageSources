/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowResource.cs
    File description: Related implementation.

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance workflow orchestration, replay, Webhook, and parallel execution

----------------------------------------------------------------*/

#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.NeuCharWorkflow;

/// <summary>
/// Localization catalog packaged with the NeuChar Workflow XNCF module.
/// </summary>
public sealed class NeuCharWorkflowResource
{
    public static string Get(string key, string? fallback = null) =>
        ResourceStringLocalizer.Get(typeof(NeuCharWorkflowResource), key, fallback);
}
