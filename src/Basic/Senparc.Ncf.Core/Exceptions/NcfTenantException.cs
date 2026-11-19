/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfTenantException.cs
    File description: NcfTenantException related implementation

    Created by: Senparc - 20210213

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

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
