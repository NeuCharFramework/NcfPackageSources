using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.DatabaseToolkit.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit.Domain.Services
{
    internal class DbConfigQueryService : ClientServiceBase<DbConfig>
    {
        public DbConfigQueryService(IClientRepositoryBase<DbConfig> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        internal bool IsAutoBackup()
        {
            var obj = base.GetObject(z => true);
            if (obj == null)
            {
                throw new UnSetBackupException();
            }
            return obj.IsAutoBackup();// TO DTO
        }
    }
}
