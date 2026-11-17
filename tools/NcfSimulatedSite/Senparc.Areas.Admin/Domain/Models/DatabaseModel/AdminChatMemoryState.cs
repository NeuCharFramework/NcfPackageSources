/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatMemoryState.cs
    File description: AdminChatMemoryState related definitions and implementation

    Created by: Senparc - 20261103

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

[Table(Register.DATABASE_PREFIX + nameof(AdminChatMemoryState))]
[Index(nameof(TenantId), nameof(UserId), IsUnique = true)]
public class AdminChatMemoryState : EntityBase<int>
{
    public int UserId { get; set; }
    [Required, MaxLength(32), ConcurrencyCheck]
    public string Revision { get; set; } = "";
    [Required]
    public byte[] Payload { get; set; } = Array.Empty<byte>();
}
