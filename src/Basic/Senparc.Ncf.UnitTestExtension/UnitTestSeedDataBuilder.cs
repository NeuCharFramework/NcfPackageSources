/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: UnitTestSeedDataBuilder.cs
    File description: UnitTestSeedDataBuilder related implementation

    Created by: Senparc - 20240801

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.3.29 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.3.29.2 Improve source documentation, headers, and module metadata consistency.

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
    /// Defines the two-phase contract used to create and finalize test seed data.
    /// </summary>
    public abstract class UnitTestSeedDataBuilder
    {


        /// <summary>
        /// Creates the data set that should be available to a test service provider.
        /// </summary>
        /// <param name="serviceProvider">The initialized test service provider.</param>
        /// <returns>
        /// A task containing the grouped seed data. The returned
        /// <see cref="DataList.UUID"/> is used by the test host to deduplicate execution.
        /// </returns>
        public abstract Task<DataList> ExecuteAsync(IServiceProvider serviceProvider);

        /// <summary>
        /// Runs after the generated seed data has been added to the shared test data set.
        /// </summary>
        /// <param name="serviceProvider">The initialized test service provider.</param>
        /// <param name="dataList">The data set produced by <see cref="ExecuteAsync(IServiceProvider)"/>.</param>
        /// <returns>A task that completes after post-seeding initialization is finished.</returns>
        public abstract Task OnExecutedAsync(IServiceProvider serviceProvider, DataList dataList);
    }
}
