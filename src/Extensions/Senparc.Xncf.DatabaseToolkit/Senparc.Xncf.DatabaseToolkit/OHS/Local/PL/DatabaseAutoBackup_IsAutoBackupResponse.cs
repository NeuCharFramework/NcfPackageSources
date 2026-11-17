/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: DatabaseAutoBackup_IsAutoBackupResponse.cs

    File description: DatabaseAutoBackup_IsAutoBackupResponse related implementation

    

    

    Created by: Senparc - 20211012

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Senparc.Ncf.Core.AppServices;
using System;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.PL
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class DatabaseAutoBackup_IsAutoBackupResponse : AppResponseBase<bool>
    {
        public bool IsAutoBackup => base.Data;

        public DatabaseAutoBackup_IsAutoBackupResponse() { }

        //public DatabaseAutoBackupStateAppServiceResponse(bool isAutoBackup, int stateCode = 0, bool success = true, string errorMessage = null)
        //    : base(stateCode, success, errorMessage, isAutoBackup)
        //{
        //}
    }
}
