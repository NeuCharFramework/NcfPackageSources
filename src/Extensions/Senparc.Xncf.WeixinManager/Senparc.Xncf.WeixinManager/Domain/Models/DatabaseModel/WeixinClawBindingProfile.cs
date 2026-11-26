/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawBindingProfile.cs
    File description: WeixinClawBindingProfile.cs implementation and project behavior.

    Created by: Senparc - 20260928

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 add WeixinClawBindingProfile management and UI enhancements

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;

[Table(Register.DATABASE_PREFIX + nameof(WeixinClawBindingProfile))]
[Serializable]
public sealed class WeixinClawBindingProfile : EntityBase<int>
{
    [Required]
    public int WeixinClawAccountId { get; private set; }

    [Required, MaxLength(150)]
    public string Name { get; private set; }

    [Required]
    public int AdminUserId { get; private set; }

    public int? WorkflowId { get; private set; }
    public int AiModelId { get; private set; }
    public int Mode { get; private set; }
    public bool EnableNeuBell { get; private set; }
    public bool EnableWorkflow { get; private set; }
    public bool Enabled { get; private set; }

    [Required, MaxLength(128)]
    public string BindingCodeHash { get; private set; }

    /// <summary>
    /// Reserved for EF Core materialization.
    /// </summary>
    private WeixinClawBindingProfile()
    {
    }

    /// <summary>
    /// Initializes a user-owned Weixin Claw binding profile.
    /// </summary>
    /// <param name="accountId">The Weixin Claw account identifier.</param>
    /// <param name="name">The profile display name.</param>
    /// <param name="adminUserId">The owning administrator identifier.</param>
    /// <param name="bindingCodeHash">The hash of the binding code.</param>
    /// <param name="workflowId">Optional workflow identifier.</param>
    /// <param name="aiModelId">The selected AI model identifier.</param>
    /// <param name="mode">The profile operation mode.</param>
    /// <param name="enableNeuBell">Whether NeuBell notifications are enabled.</param>
    /// <param name="enableWorkflow">Whether workflow execution is enabled.</param>
    public WeixinClawBindingProfile(
        int accountId,
        string name,
        int adminUserId,
        string bindingCodeHash,
        int? workflowId,
        int aiModelId,
        int mode,
        bool enableNeuBell,
        bool enableWorkflow)
    {
        WeixinClawAccountId = accountId;
        Name = NormalizeName(name);
        AdminUserId = adminUserId;
        BindingCodeHash = bindingCodeHash;
        WorkflowId = workflowId;
        AiModelId = aiModelId;
        Mode = mode;
        EnableNeuBell = enableNeuBell;
        EnableWorkflow = enableWorkflow;
        Enabled = true;
        SetUpdateTime();
    }

    /// <summary>
    /// Updates profile settings while preserving the existing binding hash when no new hash is supplied.
    /// </summary>
    /// <param name="name">The profile display name.</param>
    /// <param name="adminUserId">The owning administrator identifier.</param>
    /// <param name="bindingCodeHash">The replacement binding-code hash, when changed.</param>
    /// <param name="workflowId">Optional workflow identifier.</param>
    /// <param name="aiModelId">The selected AI model identifier.</param>
    /// <param name="mode">The profile operation mode.</param>
    /// <param name="enableNeuBell">Whether NeuBell notifications are enabled.</param>
    /// <param name="enableWorkflow">Whether workflow execution is enabled.</param>
    /// <param name="enabled">Whether this profile is active.</param>
    public void Update(
        string name,
        int adminUserId,
        string bindingCodeHash,
        int? workflowId,
        int aiModelId,
        int mode,
        bool enableNeuBell,
        bool enableWorkflow,
        bool enabled)
    {
        Name = NormalizeName(name);
        AdminUserId = adminUserId;
        if (!string.IsNullOrWhiteSpace(bindingCodeHash))
        {
            BindingCodeHash = bindingCodeHash;
        }
        WorkflowId = workflowId;
        AiModelId = aiModelId;
        Mode = mode;
        EnableNeuBell = enableNeuBell;
        EnableWorkflow = enableWorkflow;
        Enabled = enabled;
        SetUpdateTime();
    }

    /// <summary>
    /// Trims a profile name, supplies the default display name for blank input,
    /// and caps the value at the entity's 150-character persistence limit.
    /// </summary>
    /// <param name="name">The caller-supplied profile name.</param>
    /// <returns>A non-empty, trimmed name no longer than 150 characters.</returns>
    private static string NormalizeName(string name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? "个人微信绑定"
            : name.Trim().Length > 150
                ? name.Trim()[..150]
                : name.Trim();
    }
}
