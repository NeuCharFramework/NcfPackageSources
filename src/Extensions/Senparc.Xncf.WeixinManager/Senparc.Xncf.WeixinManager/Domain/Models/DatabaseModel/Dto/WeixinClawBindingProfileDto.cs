/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawBindingProfileDto.cs
    文件功能描述：WeixinClawBindingProfileDto.cs implementation and project behavior.


    创建标识：Senparc - 20260928

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

----------------------------------------------------------------*/

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
