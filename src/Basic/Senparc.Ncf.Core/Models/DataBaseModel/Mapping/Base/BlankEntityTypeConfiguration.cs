using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>
    /// 空的实现 IEntityTypeConfiguration 接口的类
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class BlankEntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
         where TEntity : class
    {
        /// <summary>
        /// 设置 TEntity 的实体
        /// </summary>
        /// <param name="builder">用于设置实体类型（entity type）的 builder</param>
        public void Configure(EntityTypeBuilder<TEntity> builder)
        {
            
        }
    }
}
