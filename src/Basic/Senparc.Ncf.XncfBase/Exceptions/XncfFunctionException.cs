/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: XncfFunctionException.cs

    File description: XncfFunctionException related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class XncfFunctionException : NcfExceptionBase
    {
        public XncfFunctionException(string message, bool logged = false) : this(message, null, logged)
        {
        }

        public XncfFunctionException(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
