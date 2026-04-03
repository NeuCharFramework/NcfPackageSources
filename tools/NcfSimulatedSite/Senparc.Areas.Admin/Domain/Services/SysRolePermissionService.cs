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

        public SysRolePermissionService(ISysRolePermissionRepository repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            _repo = repo;
        }

        /// <summary>
        /// Check whether the current user has permissions
        /// </summary>
        /// <param name="resourceCodes">Resource codes</param>
        /// <param name="adminUserInfoId">Current user Id</param>
        /// <returns></returns>
        internal async Task<bool> HasPermissionAsync(IEnumerable<string> resourceCodes, int adminUserInfoId)
        {
            if (!resourceCodes.Any())
            {
                return false;
            }
            string cacheKey = string.Format("adminUserInfoPermission:{0}", adminUserInfoId); // Cache all resources of the current user
            var distributedCache = _serviceProvider.GetService<IDistributedCache>();
            string codesJsonValue = await distributedCache.GetStringAsync(cacheKey); // Try to read the user's resource from cache
            IEnumerable<string> codes = null;
            if (string.IsNullOrEmpty(codesJsonValue))
            {
                codes = await _repo.GetAllResouceCodesByAccountIdAsync(adminUserInfoId);
                codesJsonValue = Newtonsoft.Json.JsonConvert.SerializeObject(codes);
                await distributedCache.SetStringAsync(cacheKey, codesJsonValue, new DistributedCacheEntryOptions()
                {
                    SlidingExpiration = TimeSpan.FromHours(8)
                }); // Cache for 8 hours
            }
            else
            {
                codes = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<string>>(codesJsonValue);
            }
            if (codes.Any())
            {
                return false;
            }
            //Get all resource codes of the current user
            return codes.Intersect(resourceCodes).Any();
        }
    }
}
