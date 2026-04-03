using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.UnitTestExtension.Entities;

namespace Senparc.Ncf.UnitTestExtension
{
    /// <summary>
    /// Create seed data
    /// </summary>
    public abstract class UnitTestSeedDataBuilder
    {


        /// <summary>
        /// Operations before filling in seed data
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public abstract Task<DataList> ExecuteAsync(IServiceProvider serviceProvider);

        /// <summary>
        /// What to do after filling in seed data
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="dataList">dataList returned from ExecuteAsync</param>
        /// <returns></returns>
        public abstract Task OnExecutedAsync(IServiceProvider serviceProvider, DataList dataList);
    }
}
