/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: WeixinClawBindingProfileDto.cs
    File description: WeixinClawBindingProfileDto.cs implementation and project behavior.


    Created by: Senparc - 20260928

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 add WeixinClawBindingProfile management and UI enhancements

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
