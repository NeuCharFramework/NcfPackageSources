/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DbConfigQueryService.cs
    File description: DbConfigQueryService related implementation

    Created by: Senparc - 20211012

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
        public DbConfigQueryService(IClientRepositoryBase<DbConfig> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

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
