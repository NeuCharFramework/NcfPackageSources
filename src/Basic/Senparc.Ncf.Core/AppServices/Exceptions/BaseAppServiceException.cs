/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BaseAppServiceException.cs
    File description: BaseAppServiceException related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

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
        /// <summary>
        /// Initializes an application-service exception with a response state code.
        /// </summary>
        /// <param name="stateCode">The application response state code.</param>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The underlying exception.</param>
        /// <param name="logged">Whether the exception has already been logged.</param>
        public BaseAppServiceException(int stateCode, string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
            StateCode = stateCode;
        }
    }
}
