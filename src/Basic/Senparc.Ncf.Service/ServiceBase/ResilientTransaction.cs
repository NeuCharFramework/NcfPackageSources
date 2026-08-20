/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ResilientTransaction.cs
    文件功能描述：ResilientTransaction 相关实现
    
    
    创建标识：Senparc - 20201023
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Service.ServiceBase
{
    /// <summary>
    /// 默认事务策略,微软文档用法
    /// <seealso cref="https://docs.microsoft.com/ef/core/miscellaneous/connection-resiliency"/>
    /// </summary>
    public class ResilientTransaction
    {
        private DbContext _context;
        private ResilientTransaction(DbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        public static ResilientTransaction New(DbContext context) =>
            new ResilientTransaction(context);

        public async Task ExecuteAsync(Func<Task> action)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    await action();
                    transaction.Commit();
                }
            });
        }
    }
}
