namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;

public sealed class WeixinClawBindingProfileDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string Name { get; set; }
    public int AdminUserId { get; set; }
    public int? WorkflowId { get; set; }
    public int AiModelId { get; set; }
    public int Mode { get; set; }
    public bool EnableNeuBell { get; set; } = true;
    public bool EnableWorkflow { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public string BindingCode { get; set; }
    public bool HasBindingCode { get; set; }
}
