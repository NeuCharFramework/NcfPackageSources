using Senparc.Ncf.Core.AppServices;
using System;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.PL
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    internal class DatabaseAutoBackup_IsAutoBackupResponse : AppResponseBase<bool>
    {
        public bool IsAutoBackup => base.Data;

        public DatabaseAutoBackup_IsAutoBackupResponse() { }

        //public DatabaseAutoBackupStateAppServiceResponse(bool isAutoBackup, int stateCode = 0, bool success = true, string errorMessage = null)
        //    : base(stateCode, success, errorMessage, isAutoBackup)
        //{
        //}
    }
}
