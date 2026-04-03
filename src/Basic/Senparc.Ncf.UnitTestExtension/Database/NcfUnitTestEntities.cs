using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.UnitTestExtension.Entities;
using Senparc.Xncf.SystemCore.Domain.Database;

namespace Senparc.Ncf.UnitTestExtension.Database
{
    /// <summary>
    /// Emulate SenparcEntities or xxxSenparcEntities in various XNCF modules.
    /// During unit testing, this class will replace the NCF runtime's SenparcEntities, integrating all modules' DbSet objects
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
