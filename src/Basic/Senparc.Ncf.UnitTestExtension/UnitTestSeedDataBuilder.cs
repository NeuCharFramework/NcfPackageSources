/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: UnitTestSeedDataBuilder.cs
    File description: UnitTestSeedDataBuilder related implementation

    Created by: Senparc - 20240801

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

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
    ///Legacy comment translated to English.
    /// </summary>
    public abstract class UnitTestSeedDataBuilder
    {


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public abstract Task<DataList> ExecuteAsync(IServiceProvider serviceProvider);

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="serviceProvider"></param>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public abstract Task OnExecutedAsync(IServiceProvider serviceProvider, DataList dataList);
    }
}
