/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：BasePoolEntities.cs
    文件功能描述：BasePoolEntities 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;

namespace Senparc.Xncf.SystemCore.Domain.Database
{
    //TODO: 如果后期没有明显变化（如额外扩展），考虑合并 SenparcEntities，并取代之

    /// <summary>
    /// 当前 Entities 只为帮助 SenparcEntities 生成 Migration 信息而存在，没有特别的操作意义。
    /// </summary>
    public class BasePoolEntities : SenparcEntities
    {
        private BasePoolEntities() : this(null, null) { }

        public BasePoolEntities(DbContextOptions/*<BasePoolEntities>*/ dbContextOptions, IServiceProvider serviceProvider)
            : base(dbContextOptions, serviceProvider)
        {

        }
    }
}
