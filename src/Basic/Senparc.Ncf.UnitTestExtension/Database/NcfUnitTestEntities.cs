/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfUnitTestEntities.cs
    File description: NcfUnitTestEntities related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.3.29 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.3.29.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.UnitTestExtension.Entities;
using Senparc.Xncf.SystemCore.Domain.Database;

namespace Senparc.Ncf.UnitTestExtension.Database
{
    /// <summary>
    /// EF Core context used by the NCF unit-test data provider and its typed
    /// in-memory entity-set projections.
    /// </summary>
    public class NcfUnitTestEntities : BasePoolEntities //DbContext
    {
        DataList _dataList;
        private readonly Dictionary<Type, object> _dbSets = new();

        /// <summary>
        /// Initializes the test context with EF options, service access, and seed data.
        /// </summary>
        /// <param name="dbContextOptions">The EF Core context options.</param>
        /// <param name="serviceProvider">The test service provider.</param>
        /// <param name="dataList">The grouped seed-data collection.</param>
        public NcfUnitTestEntities(DbContextOptions dbContextOptions, IServiceProvider serviceProvider, DataList dataList) : base(dbContextOptions, serviceProvider)
        {
            _dataList = dataList;
        }

        /// <summary>
        /// Converts each item in an untyped list to a constructed generic list
        /// of <paramref name="outType"/>.
        /// </summary>
        /// <param name="list">The source values.</param>
        /// <param name="outType">The desired element type.</param>
        /// <returns>A non-generic <see cref="IList"/> containing converted values.</returns>
        public static IList ConvertList(IList list, Type outType)
        {
            var genericListType = typeof(List<>).MakeGenericType(outType);
            var convertedList = (IList)Activator.CreateInstance(genericListType);

            foreach (var item in list)
            {
                convertedList.Add(Convert.ChangeType(item, outType));
            }
            return convertedList;
        }


        /// <inheritdoc />
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Console.WriteLine("NcfUnitTestEntities OnConfiguring");

            base.OnConfiguring(optionsBuilder);
        }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Console.WriteLine("NcfUnitTestEntities OnModelCreating");

            base.OnModelCreating(modelBuilder);
        }
    }
}
