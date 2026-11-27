/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DbConfigQueryService.cs
    File description: DbConfigQueryService related implementation

    Created by: Senparc - 20211012

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.27.13 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.27.13.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.DatabaseToolkit.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DatabaseToolkit.Domain.Services
{
    public class DbConfigQueryService : ClientServiceBase<DbConfig>
    {
        /// <summary>
        /// Initializes the database configuration query service.
        /// </summary>
        /// <param name="repo">The client repository for database configuration.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public DbConfigQueryService(IClientRepositoryBase<DbConfig> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Reads the persisted automatic-backup flag.
        /// </summary>
        /// <returns>A task returning whether automatic backup is enabled.</returns>
        /// <exception cref="UnSetBackupException">Thrown when no database backup configuration exists.</exception>
        internal async Task<bool> IsAutoBackup()
        {
            var obj = await base.GetObjectAsync(z => true);
            if (obj == null)
            {
                throw new UnSetBackupException();
            }
            return obj.IsAutoBackup();// TO DTO
        }
    }
}
