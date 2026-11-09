/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveConfigurationMappings.cs
    文件功能描述：NeuCharBox Live 领域与数据库（Models）：LiveConfigurationMappings


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Xncf.NeuCharBoxLive.Api;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;

namespace Senparc.Xncf.NeuCharBoxLive.Models
{
    /// <summary>Column sizes. The apply fields match the apply endpoint's caps, so whatever it accepts fits.</summary>
    public static class LiveColumns
    {
        public const int Code = 32;
        public const int Email = LiveApiController.MaxEmail;
        public const int Name = LiveApiController.MaxName;
        public const int WouldTry = LiveApiController.MaxWouldTry;
        public const int Scene = LiveApiController.MaxScene;

        /// <summary>A code made on approval is labelled with the applicant's email.</summary>
        public const int Label = Email;

        public const int Interest = 16;
        public const int Status = 16;
        public const int Model = 100;
        public const int Device = 64;
        public const int Outcome = 32;
    }

    [XncfAutoConfigurationMapping]
    public class InviteCodeConfigurationMapping : ConfigurationMappingWithIdBase<InviteCodeEntity, int>
    {
        public override void Configure(EntityTypeBuilder<InviteCodeEntity> builder)
        {
            base.Configure(builder);
            builder.Property(e => e.Code).IsRequired().HasMaxLength(LiveColumns.Code);
            builder.Property(e => e.Normalized).IsRequired().HasMaxLength(LiveColumns.Code);
            builder.Property(e => e.Label).IsRequired().HasMaxLength(LiveColumns.Label);
            builder.HasIndex(e => e.Normalized).IsUnique();
        }
    }

    [XncfAutoConfigurationMapping]
    public class ApplicationConfigurationMapping : ConfigurationMappingWithIdBase<ApplicationEntity, int>
    {
        public override void Configure(EntityTypeBuilder<ApplicationEntity> builder)
        {
            base.Configure(builder);
            builder.Property(e => e.Email).IsRequired().HasMaxLength(LiveColumns.Email);
            builder.Property(e => e.Name).HasMaxLength(LiveColumns.Name);
            builder.Property(e => e.Interest).IsRequired().HasMaxLength(LiveColumns.Interest);
            builder.Property(e => e.WouldTry).IsRequired().HasMaxLength(LiveColumns.WouldTry);
            builder.Property(e => e.Scene).IsRequired().HasMaxLength(LiveColumns.Scene);
            builder.Property(e => e.Status).IsRequired().HasMaxLength(LiveColumns.Status);
        }
    }

    [XncfAutoConfigurationMapping]
    public class RunConfigurationMapping : ConfigurationMappingWithIdBase<RunEntity, int>
    {
        public override void Configure(EntityTypeBuilder<RunEntity> builder)
        {
            base.Configure(builder);
            builder.Property(e => e.Code).IsRequired().HasMaxLength(LiveColumns.Code);
            builder.Property(e => e.Scene).IsRequired().HasMaxLength(LiveColumns.Scene);
            builder.Property(e => e.Text).IsRequired(); // the request's length limit is config (MaxChars), so no cap here
            builder.Property(e => e.WhatIfDevice).HasMaxLength(LiveColumns.Device);
            builder.Property(e => e.Model).IsRequired().HasMaxLength(LiveColumns.Model);
            builder.Property(e => e.Outcome).HasMaxLength(LiveColumns.Outcome);
            builder.HasIndex(e => e.Day);
        }
    }
}
