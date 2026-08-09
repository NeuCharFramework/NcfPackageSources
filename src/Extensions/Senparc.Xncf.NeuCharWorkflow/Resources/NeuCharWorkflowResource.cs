/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowResource.cs
    文件功能描述：NeuChar Workflow 模块拥有的本地化资源入口
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
