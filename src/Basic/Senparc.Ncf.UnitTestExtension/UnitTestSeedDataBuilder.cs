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
    /// 创建种子数据
    /// </summary>
    public abstract class UnitTestSeedDataBuilder
    {
        /// <summary>
        /// 填充种子数据前的操作
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="dataList"></param>
        /// <returns></returns>
        public abstract Task ExecuteAsync(IServiceProvider serviceProvider, DataList dataList);

        /// <summary>
        /// 填充种子数据后的操作
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="dataList"></param>
        /// <returns></returns>
        public abstract Task OnExecutedAsync(IServiceProvider serviceProvider, DataList dataList);
    }
}
