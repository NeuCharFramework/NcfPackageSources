using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.UnitTestExtension.Entities;
using Senparc.Xncf.SystemCore.Domain.Database;

namespace Senparc.Ncf.UnitTestExtension.Database
{
    /// <summary>
    /// 模拟 SenparcEntities 或各个 XNCF 模块中的 xxxSenparcEntities。
    /// 在单元测试过程中，这个类将取代 NCF 运行时的 SenparcEntities，集成所有模块的 DbSet 对象
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
