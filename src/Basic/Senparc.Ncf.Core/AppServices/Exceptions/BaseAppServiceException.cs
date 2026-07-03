/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：BaseAppServiceException.cs
    文件功能描述：BaseAppServiceException 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.CO2NET.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.AppServices.Exceptions
{
    public class BaseAppServiceException : BaseException
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public int StateCode { get; set; }
        public BaseAppServiceException(int stateCode, string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
            StateCode = stateCode;
        }
    }
}
