/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ServieExtension.cs
    文件功能描述：ServieExtension 相关实现
    
    
    创建标识：Senparc - 20250112
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System.Linq;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Ncf.Service
{
    public static class ServiceExtension
    {
        public static PagedList<TDto> ToDtoPagedList<T, TDto>(this PagedList<T> pagedList, IServiceBase<T> serviceBase)
            where T : class, IEntityBase
        {
            return new PagedList<TDto>(pagedList.Select(item => serviceBase.Mapping<TDto>(item)).ToList(), pagedList.PageIndex, pagedList.PageCount, pagedList.TotalCount);
        }
    }
}