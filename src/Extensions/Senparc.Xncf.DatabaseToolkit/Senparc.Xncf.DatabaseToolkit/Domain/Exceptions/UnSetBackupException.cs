/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：UnSetBackupException.cs
    文件功能描述：UnSetBackupException 相关实现
    
    
    创建标识：Senparc - 20211012
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.AppServices.Exceptions;
using System;

namespace Senparc.Xncf.DatabaseToolkit.Domain.Exceptions
{
    internal class UnSetBackupException : BaseAppServiceException
    {
        public UnSetBackupException(int stateCode = 201, string message = "未找到数据库配置，请先进行配置，在进行查询！", Exception inner = null, bool logged = false) : base(stateCode, message, inner, logged)
        {
        }
    }
}
