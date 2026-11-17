/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatSessionWorkflow.cs
    File description: AdminChatSessionWorkflow.cs related implementation

    Created by: Senparc - 20260821

    Modified by: Senparc - 20260822
    Change description: v0.6.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

/// <summary>
///Legacy comment translated to English.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(AdminChatSessionWorkflow))]
[Serializable]
public class AdminChatSessionWorkflow : EntityBase<int>
{
    [Required]
    public int SessionId { get; private set; }

    [Required]
    public int WorkflowId { get; private set; }

    [Required, MaxLength(200)]
    public string WorkflowName { get; private set; }

    [MaxLength(400)]
    public string WorkflowDescription { get; private set; }

    [Required]
    public DateTime AddedTime { get; private set; }

    [ForeignKey(nameof(SessionId))]
    public virtual AdminChatSession Session { get; private set; }

    private AdminChatSessionWorkflow() { }

    public AdminChatSessionWorkflow(
        int sessionId,
        int workflowId,
        string workflowName,
        string workflowDescription)
    {
        if (sessionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionId));
        }
        if (workflowId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workflowId));
        }

        SessionId = sessionId;
        WorkflowId = workflowId;
        WorkflowName = string.IsNullOrWhiteSpace(workflowName) ? $"Workflow #{workflowId}" : workflowName.Trim();
        WorkflowDescription = workflowDescription?.Trim();
        AddedTime = DateTime.Now;
    }
}
