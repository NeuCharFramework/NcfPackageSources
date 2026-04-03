using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>
    /// ConfigurationMapping base class containing Id (Key)
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class ConfigurationMappingWithIdBase<TEntity, TKey> : ConfigurationMappingBase<TEntity>, IEntityTypeConfiguration<TEntity>
        where TEntity : EntityBase<TKey>
    {
        /// <summary>
        /// Configure <typeparamref name="TEntity"/> instance
        /// </summary>
        /// <param name="builder"></param>
        public override void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.HasKey(z => z.Id);
            base.Configure(builder);
        }
    }

    /// <summary>
    /// ConfigurationMapping base class that does not contain Id (Key)
    /// </summary>
    public class ConfigurationMappingBase<TEntity> : IEntityTypeConfiguration<TEntity>
        where TEntity : EntityBase
    {
        /// <summary>
        /// Configure <typeparamref name="TEntity"/> instance
        /// </summary>
        /// <param name="builder"></param>
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            //builder.Property(e => e.AddTime).HasColumnType("datetime").IsRequired();
            //builder.Property(e => e.LastUpdateTime).HasColumnType("datetime").IsRequired();
        }

    }
}
