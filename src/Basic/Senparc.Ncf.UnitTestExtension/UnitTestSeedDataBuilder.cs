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
        public abstract Task ExecuteAsync(IServiceProvider serviceProvider, DataList dataList);

        public abstract Task OnExecutedAsync(IServiceProvider serviceProvider, DataList dataList);
    }
}
