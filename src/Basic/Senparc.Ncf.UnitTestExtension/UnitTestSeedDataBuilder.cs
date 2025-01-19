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
        /// <returns></returns>
        public abstract Task<DataList> ExecuteAsync(IServiceProvider serviceProvider);

        /// <summary>
        /// 填充种子数据后的操作
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="dataList">从 ExecuteAsync 中返回的 dataList</param>
        /// <returns></returns>
        public abstract Task OnExecutedAsync(IServiceProvider serviceProvider, DataList dataList);
    }
}
