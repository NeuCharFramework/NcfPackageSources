/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawBindingProfileContracts.cs
    文件功能描述：WeixinClawBindingProfileContracts.cs implementation and project behavior.


    创建标识：Senparc - 20260928

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

----------------------------------------------------------------*/

using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public interface IWeixinClawBindingProfileResolver
{
    Task<WeixinClawBindingProfileResolution> ResolveAsync(
        int accountId,
        string bindingCode,
        CancellationToken cancellationToken = default);
}

public sealed class WeixinClawBindingProfileResolution
{
    public int Id { get; init; }
    public int AccountId { get; init; }
    public string Name { get; init; }
    public int AdminUserId { get; init; }
    public int? WorkflowId { get; init; }
    public int AiModelId { get; init; }
    public int Mode { get; init; }
    public bool EnableNeuBell { get; init; }
    public bool EnableWorkflow { get; init; }
}
