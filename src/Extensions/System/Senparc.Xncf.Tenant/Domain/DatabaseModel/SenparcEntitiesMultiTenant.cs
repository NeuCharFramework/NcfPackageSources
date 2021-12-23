using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.Tenant.Domain.DatabaseModel
{
    /* 说明：
     * 把 SenparcEntitiesMultiTenantBase 独立出来做一层的意义在于剥离必须需要支持多租户的对象（如系统表），
     * 这样可以单独初始化一个只有 TenantInfo，而没有任何其他系统表的 DbContext。
     * 这么做可以避免一种死循环的情况：
     *      1、在缓存第一次录入租户信息（TenantInfos）的过程中，需要初始化所有表，并执行 OnModelCreating()，
     *          而 OnModelCreating() 在整个系统服务生命周期中只执行一次，而此时马上要确定依赖多租户对象（如系统表）的租户信息，
     *      2、但多租户信息此时尚未生成，如果尝试获取，就进入到 1 的循环中。
     */

    /// <summary>
    /// 多租户 EF Core DbContext
    /// </summary>
    public sealed class SenparcEntitiesMultiTenant : SenparcEntitiesDbContextBase, ISenparcEntitiesDbContext
    {
        /// <summary>
        /// 多租户信息
        /// </summary>
        public DbSet<TenantInfo> TenantInfos { get; set; }


        public SenparcEntitiesMultiTenant(DbContextOptions options, IServiceProvider serviceProvider) : base(options, serviceProvider)
        {
        }

    }
}
