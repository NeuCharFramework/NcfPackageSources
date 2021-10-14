using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.DatabaseToolkit.Domain.Services;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.PL;
using System;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.AppService
{
    internal class DatabaseAutoBackupAppService : AppServiceBase
    {
        private readonly DbConfigQueryService _dbConfigQueryService;

        public DatabaseAutoBackupAppService(IServiceProvider serviceProvider, DbConfigQueryService dbConfigQueryService) : base(serviceProvider)
        {
            this._dbConfigQueryService = dbConfigQueryService;
        }

        [ApiBind]
        public DatabaseAutoBackup_IsAutoBackupResponse IsAutoBackup()
        {
            return this.GetResponse<DatabaseAutoBackup_IsAutoBackupResponse, bool>((response, logger) =>
                {
                    return _dbConfigQueryService.IsAutoBackup();
                });
        }
    }
}
