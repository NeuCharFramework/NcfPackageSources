/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: NcfTenantException.cs

    File description: NcfTenantException related implementation

    

    

    Created by: Senparc - 20210213

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Exceptions
{
    public class NcfTenantException : NcfExceptionBase
    {
        public NcfTenantException(string message, bool logged = false) : base(message, logged)
        {
        }

        public NcfTenantException(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
