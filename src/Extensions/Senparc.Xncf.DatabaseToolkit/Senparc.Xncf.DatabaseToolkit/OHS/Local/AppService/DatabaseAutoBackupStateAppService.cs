using Senparc.Xncf.DatabaseToolkit.Domain.Exceptions;
using Senparc.Xncf.DatabaseToolkit.Domain.Services;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.AppService
{
    //[ApiBind()]
    internal class DatabaseAutoBackupStateAppService//:BaseAppService
    {
        private readonly DbConfigQueryService _dbConfigQueryService;

        public DatabaseAutoBackupStateAppService(DbConfigQueryService dbConfigQueryService)
        {
            this._dbConfigQueryService = dbConfigQueryService;
        }

        public DatabaseAutoBackupStateAppServiceResponse IsAutoBackup()
        {
            DatabaseAutoBackupStateAppServiceResponse response = new DatabaseAutoBackupStateAppServiceResponse(false);
            try
            {
                response.Data = _dbConfigQueryService.IsAutoBackup();
                response.Success = true;
            }
            catch (UnSetBackupException unSetEx)
            {
                response.Success = false;
                response.StateCode = unSetEx.StateCode;
                response.ErrorMessage = unSetEx.Message;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.StateCode = 100;
                response.ErrorMessage = ex.Message;
            }

            return response;
        }
    }
}
