using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.DatabaseToolkit.Domain.Services;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.PL;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.AppService
{
    [ApiBind()]
    internal class DatabaseAutoBackupAppService : BaseAppService
    {
        private readonly DbConfigQueryService _dbConfigQueryService;

        public DatabaseAutoBackupAppService(DbConfigQueryService dbConfigQueryService)
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
