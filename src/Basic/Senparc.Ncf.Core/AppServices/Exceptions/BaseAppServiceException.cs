/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: BaseAppServiceException.cs

    File description: BaseAppServiceException related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
        /// Status Code
        /// </summary>
        public int StateCode { get; set; }
        public BaseAppServiceException(int stateCode, string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
            StateCode = stateCode;
        }
    }
}
