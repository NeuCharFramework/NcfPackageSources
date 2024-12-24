using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>
    /// 包含 Id（Key）的 ConfigurationMapping 基类
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class ConfigurationMappingWithIdBase<TEntity, TKey> : ConfigurationMappingBase<TEntity>, IEntityTypeConfiguration<TEntity>
        where TEntity : EntityBase<TKey>
    {
        /// <summary>
        /// 配置 <typeparamref name="TEntity"/> 实例
        /// </summary>
        /// <param name="builder"></param>
        public override void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.HasKey(z => z.Id);
            base.Configure(builder);
        }
    }

    /// <summary>
    /// 不包含 Id（Key）的 ConfigurationMapping 基类
    /// </summary>
    public class ConfigurationMappingBase<TEntity> : IEntityTypeConfiguration<TEntity>
        where TEntity : EntityBase
    {
        /// <summary>
        /// 配置 <typeparamref name="TEntity"/> 实例
        /// </summary>
        /// <param name="builder"></param>
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            //builder.Property(e => e.AddTime).HasColumnType("datetime").IsRequired();
            //builder.Property(e => e.LastUpdateTime).HasColumnType("datetime").IsRequired();
        }

    }
}
