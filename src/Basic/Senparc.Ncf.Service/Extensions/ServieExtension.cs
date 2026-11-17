/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ServieExtension.cs
    File description: ServieExtension related implementation

    Created by: Senparc - 20250112

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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