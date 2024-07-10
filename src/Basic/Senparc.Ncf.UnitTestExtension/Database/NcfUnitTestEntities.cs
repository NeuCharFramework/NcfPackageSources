using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Senparc.Xncf.SystemCore.Domain.Database;

namespace Senparc.Ncf.UnitTestExtension.Database
{
    /// <summary>
    /// 模拟 SenparcEntities 或各个 XNCF 模块中的 xxxSenparcEntities
    /// </summary>
    public class NcfUnitTestEntities : BasePoolEntities //DbContext
    {
        public NcfUnitTestEntities(DbContextOptions dbContextOptions, IServiceProvider serviceProvider) : base(dbContextOptions, serviceProvider)
        {
        }
    }
}
