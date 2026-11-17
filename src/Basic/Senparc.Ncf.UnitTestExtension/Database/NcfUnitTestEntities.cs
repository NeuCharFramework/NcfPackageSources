/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfUnitTestEntities.cs
    File description: NcfUnitTestEntities related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public class NcfUnitTestEntities : BasePoolEntities //DbContext
    {
        DataList _dataList;
        private readonly Dictionary<Type, object> _dbSets = new();

        public NcfUnitTestEntities(DbContextOptions dbContextOptions, IServiceProvider serviceProvider, DataList dataList) : base(dbContextOptions, serviceProvider)
        {
            _dataList = dataList;
        }

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


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Console.WriteLine("NcfUnitTestEntities OnConfiguring");

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Console.WriteLine("NcfUnitTestEntities OnModelCreating");

            base.OnModelCreating(modelBuilder);
        }
    }
}
