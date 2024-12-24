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
