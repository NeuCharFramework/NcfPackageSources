/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：BlankEntityTypeConfiguration.cs
    文件功能描述：BlankEntityTypeConfiguration 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
