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

    private WeixinClawBindingProfile()
    {
    }

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

    private static string NormalizeName(string name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? "个人微信绑定"
            : name.Trim().Length > 150
                ? name.Trim()[..150]
                : name.Trim();
    }
}
