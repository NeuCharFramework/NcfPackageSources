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
