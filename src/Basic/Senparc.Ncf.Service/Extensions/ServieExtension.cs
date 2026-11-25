/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ServieExtension.cs
    File description: ServieExtension related implementation

    Created by: Senparc - 20250112

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

----------------------------------------------------------------*/

using System.Linq;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Ncf.Service
{
    public static class ServiceExtension
    {
        /// <summary>
        /// Maps each entity in a paged result to a DTO while preserving paging metadata.
        /// </summary>
        /// <typeparam name="T">The source entity type.</typeparam>
        /// <typeparam name="TDto">The destination DTO type.</typeparam>
        /// <param name="pagedList">The source page.</param>
        /// <param name="serviceBase">The service whose mapper performs the conversion.</param>
        /// <returns>A DTO page with the same page index, size, and total count.</returns>
        public static PagedList<TDto> ToDtoPagedList<T, TDto>(this PagedList<T> pagedList, IServiceBase<T> serviceBase)
            where T : class, IEntityBase
        {
            return new PagedList<TDto>(pagedList.Select(item => serviceBase.Mapping<TDto>(item)).ToList(), pagedList.PageIndex, pagedList.PageCount, pagedList.TotalCount);
        }
    }
}
