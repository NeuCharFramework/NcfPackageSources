/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DatabaseAutoBackup_IsAutoBackupResponse.cs
    文件功能描述：DatabaseAutoBackup_IsAutoBackupResponse 相关实现
    
    
    创建标识：Senparc - 20211012
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
