/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Sandbox_SandboxSessionConfigurationMapping.cs
    File description: Sandbox_SandboxSessionConfigurationMapping.cs implementation and project behavior.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20261005
    Change description: v0.3.4 0.3.4 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.4.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.Sandbox.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.Sandbox.Models;

[XncfAutoConfigurationMapping]
public class Sandbox_SandboxSessionConfigurationMapping : ConfigurationMappingWithIdBase<SandboxSession, int>
{
    public override void Configure(EntityTypeBuilder<SandboxSession> builder)
    {
        //Legacy comment translated to English.
        builder.Property(e => e.SessionId).IsRequired().HasMaxLength(64);
        builder.Property(e => e.TemplateKey).IsRequired().HasMaxLength(64);
        builder.Property(e => e.RuntimeHandle).HasMaxLength(128);
        builder.Property(e => e.AccessUrl).HasMaxLength(500);
        builder.Property(e => e.AccessToken).HasMaxLength(128);
        builder.Property(e => e.StatusMessage).HasMaxLength(1000);
        builder.Property(e => e.Alias).HasMaxLength(128);
        builder.Property(e => e.ExtraPorts).HasMaxLength(500);
        builder.Property(e => e.AdminRemark).HasMaxLength(300);
        builder.Property(e => e.Remark).HasMaxLength(300);
        builder.HasIndex(e => e.SessionId).IsUnique();
        builder.HasIndex(e => new { e.OwnerUserId, e.Status });
        builder.HasIndex(e => e.ExpiresAtUtc);
    }
}
