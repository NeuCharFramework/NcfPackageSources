using Senparc.Ncf.Core.Models.AppServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.PL
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    internal class DatabaseAutoBackupStateAppServiceResponse : BaseAppResponse
    {
        //链式编程
        //扩展属性

        public bool IsAutoBackup => (bool)base.Data;


        public DatabaseAutoBackupStateAppServiceResponse(bool isAutoBackup, int stateCode = 0, bool success = true, string errorMessage = null)
            : base(stateCode, success, errorMessage, isAutoBackup)
        {
        }
    }
}
