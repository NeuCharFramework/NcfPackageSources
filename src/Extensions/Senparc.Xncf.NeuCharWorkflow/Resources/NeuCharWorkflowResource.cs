/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowResource.cs
    File description: Related implementation.

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.5.0.0 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.NeuCharWorkflow;

/// <summary>
/// Localization catalog packaged with the NeuChar Workflow XNCF module.
/// </summary>
public sealed class NeuCharWorkflowResource
{
    /// <summary>
    /// Resolves a workflow localization key and returns the fallback when the key is unavailable.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <param name="fallback">The text returned when the key has no localized value.</param>
    /// <returns>The localized text or the fallback text.</returns>
    public static string Get(string key, string? fallback = null) =>
        ResourceStringLocalizer.Get(typeof(NeuCharWorkflowResource), key, fallback);
}
