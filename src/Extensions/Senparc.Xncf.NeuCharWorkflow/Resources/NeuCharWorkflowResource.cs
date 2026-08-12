/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowResource.cs
    文件功能描述：NeuChar Workflow 模块拥有的本地化资源入口


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

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
