/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConfigurationMappingBase.cs
    File description: ConfigurationMappingBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class ConfigurationMappingWithIdBase<TEntity, TKey> : ConfigurationMappingBase<TEntity>, IEntityTypeConfiguration<TEntity>
        where TEntity : EntityBase<TKey>
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="builder"></param>
        public override void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.HasKey(z => z.Id);
            base.Configure(builder);
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class ConfigurationMappingBase<TEntity> : IEntityTypeConfiguration<TEntity>
        where TEntity : EntityBase
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="builder"></param>
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            //builder.Property(e => e.AddTime).HasColumnType("datetime").IsRequired();
            //builder.Property(e => e.LastUpdateTime).HasColumnType("datetime").IsRequired();
        }

    }
}
