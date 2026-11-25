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
    /// Executes Entity Framework Core operations through the provider's execution strategy,
    /// creating and committing a transaction for each retry attempt.
    /// <seealso cref="https://docs.microsoft.com/ef/core/miscellaneous/connection-resiliency"/>
    /// </summary>
    public class ResilientTransaction
    {
        private DbContext _context;
        /// <summary>
        /// Associates the wrapper with one Entity Framework Core context.
        /// </summary>
        /// <param name="context">The context used to create execution strategies and transactions.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
        private ResilientTransaction(DbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        /// <summary>
        /// Creates a resilient transaction wrapper for an Entity Framework Core context.
        /// </summary>
        /// <param name="context">The context whose database and execution strategy are used.</param>
        /// <returns>A transaction wrapper associated with <paramref name="context"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
        public static ResilientTransaction New(DbContext context) =>
            new ResilientTransaction(context);

        /// <summary>
        /// Executes an asynchronous operation inside a transaction managed by the context's
        /// execution strategy. Provider-defined transient failures may cause the complete
        /// operation, including transaction creation, to be retried.
        /// </summary>
        /// <param name="action">The asynchronous operation to execute and commit.</param>
        /// <returns>A task that completes after the operation and transaction commit succeed.</returns>
        /// <exception cref="ArgumentNullException">Thrown by delegate invocation when <paramref name="action"/> is <see langword="null"/>.</exception>
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
