/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: WeixinClawBindingProfileContracts.cs
    File description: WeixinClawBindingProfileContracts.cs implementation and project behavior.


    Created by: Senparc - 20260928

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 add WeixinClawBindingProfile management and UI enhancements

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
