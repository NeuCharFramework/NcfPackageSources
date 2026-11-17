/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: NcfExceptionBase.cs

    File description: NcfExceptionBase related implementation

    

    

    Created by: Senparc - 20201001

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Exceptions
{
    public class NcfExceptionBase : CO2NET.Exceptions.BaseException
    {
        public NcfExceptionBase(string message, bool logged = false) : base(message, logged)
        {
        }

        public NcfExceptionBase(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
