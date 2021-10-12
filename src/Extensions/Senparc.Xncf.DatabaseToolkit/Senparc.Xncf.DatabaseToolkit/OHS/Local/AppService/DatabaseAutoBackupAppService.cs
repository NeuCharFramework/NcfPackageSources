using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.DatabaseToolkit.Domain.Services;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.PL;
using System;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.AppService
{
    [ApiBind()]
    internal class DatabaseAutoBackupAppService : AppServiceBase
    {
        private readonly DbConfigQueryService _dbConfigQueryService;

        public DatabaseAutoBackupAppService(IServiceProvider serviceProvider, DbConfigQueryService dbConfigQueryService) : base(serviceProvider)
        {
            this._dbConfigQueryService = dbConfigQueryService;
        }

        public DatabaseAutoBackup_IsAutoBackupResponse IsAutoBackup()
        {
            return AppServiceHelper.GetResponse<DatabaseAutoBackup_IsAutoBackupResponse, bool>(response =>
                {
                    return _dbConfigQueryService.IsAutoBackup();
                });
        }
    }
}
