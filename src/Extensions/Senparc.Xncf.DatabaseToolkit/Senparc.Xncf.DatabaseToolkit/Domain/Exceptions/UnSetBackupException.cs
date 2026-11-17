/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: UnSetBackupException.cs

    File description: UnSetBackupException related implementation

    

    

    Created by: Senparc - 20211012

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
