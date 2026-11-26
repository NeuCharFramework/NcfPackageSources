/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SysRolePermissionService.cs
    File description: SysRolePermissionService related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Senparc.Areas.Admin.ACL.Repository;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;

namespace Senparc.Areas.Admin.Domain.Services
{
    public class SysRolePermissionService : BaseClientService<SysRolePermission>
    {
        private readonly ISysRolePermissionRepository _repo;

        /// <summary>
        /// Initializes the role-permission service with its repository and service provider.
        /// </summary>
        /// <param name="repo">The repository used for permission persistence and queries.</param>
        /// <param name="serviceProvider">The request service provider used for caching services.</param>
        public SysRolePermissionService(ISysRolePermissionRepository repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            _repo = repo;
        }

        /// <summary>
        /// Determines whether an administrator has at least one of the requested resource codes,
        /// using the distributed cache when available and refreshing it from the repository otherwise.
        /// </summary>
        /// <param name="resourceCodes">The resource codes to test.</param>
        /// <param name="adminUserInfoId">The administrator identifier whose permissions are checked.</param>
        /// <returns><see langword="true"/> when any requested code is granted.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourceCodes"/> is null.</exception>
        internal async Task<bool> HasPermissionAsync(IEnumerable<string> resourceCodes, int adminUserInfoId)
        {
            if (!resourceCodes.Any())
            {
                return false;
            }
            string cacheKey = string.Format("adminUserInfoPermission:{0}", adminUserInfoId); //Legacy comment translated to English.
            var distributedCache = _serviceProvider.GetService<IDistributedCache>();
            string codesJsonValue = await distributedCache.GetStringAsync(cacheKey); //Legacy comment translated to English.
            IEnumerable<string> codes = null;
            if (string.IsNullOrEmpty(codesJsonValue))
            {
                codes = await _repo.GetAllResouceCodesByAccountIdAsync(adminUserInfoId);
                codesJsonValue = Newtonsoft.Json.JsonConvert.SerializeObject(codes);
                await distributedCache.SetStringAsync(cacheKey, codesJsonValue, new DistributedCacheEntryOptions()
                {
                    SlidingExpiration = TimeSpan.FromHours(8)
                }); //Legacy comment translated to English.
            }
            else
            {
                codes = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<string>>(codesJsonValue);
            }
            if (codes.Any())
            {
                return false;
            }
            //Legacy comment translated to English.
            return codes.Intersect(resourceCodes).Any();
        }
    }
}
