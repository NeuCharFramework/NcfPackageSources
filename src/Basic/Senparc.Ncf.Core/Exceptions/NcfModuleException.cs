/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfModuleException.cs
    File description: NcfModuleException related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Exceptions
{
    public class NcfModuleException : NcfExceptionBase
    {
        //public IXncfRegister XncfRegister;
        public NcfModuleException(string message, bool logged = false) : base(message, logged)
        {
        }

        public NcfModuleException(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
