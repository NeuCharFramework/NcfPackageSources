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
