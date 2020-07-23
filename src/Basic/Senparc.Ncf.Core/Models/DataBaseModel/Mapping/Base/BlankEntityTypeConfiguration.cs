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
        public void Configure(EntityTypeBuilder<TEntity> builder)
        {
            
        }
    }
}
