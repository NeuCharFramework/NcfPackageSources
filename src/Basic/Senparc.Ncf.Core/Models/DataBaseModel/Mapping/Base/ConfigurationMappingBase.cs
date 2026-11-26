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
    /// Defines the base mapping hook for entities with typed identifiers.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <typeparam name="TKey">The entity identifier type.</typeparam>
    public class ConfigurationMappingWithIdBase<TEntity, TKey> : ConfigurationMappingBase<TEntity>, IEntityTypeConfiguration<TEntity>
        where TEntity : EntityBase<TKey>
    {
        /// <inheritdoc />
        /// <param name="builder">The EF Core entity builder.</param>
        public override void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.HasKey(z => z.Id);
            base.Configure(builder);
        }
    }

    /// <summary>
    /// Provides the default mapping hook for NCF entities.
    /// </summary>
    public class ConfigurationMappingBase<TEntity> : IEntityTypeConfiguration<TEntity>
        where TEntity : EntityBase
    {
        /// <summary>
        /// Applies shared entity mapping conventions. The base implementation is intentionally empty.
        /// </summary>
        /// <param name="builder">The EF Core entity builder.</param>
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            //builder.Property(e => e.AddTime).HasColumnType("datetime").IsRequired();
            //builder.Property(e => e.LastUpdateTime).HasColumnType("datetime").IsRequired();
        }

    }
}
