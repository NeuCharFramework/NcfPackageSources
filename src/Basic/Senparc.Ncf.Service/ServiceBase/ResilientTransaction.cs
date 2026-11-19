/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ResilientTransaction.cs
    File description: ResilientTransaction related implementation

    Created by: Senparc - 20201023

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Service.ServiceBase
{
    /// <summary>
    ///Legacy comment translated to English.
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
