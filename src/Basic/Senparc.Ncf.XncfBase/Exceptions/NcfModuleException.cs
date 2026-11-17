/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfModuleException.cs
    File description: NcfModuleException related implementation

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
    public class XncfPageException : NcfExceptionBase
    {
        public IXncfRegister XncfRegister { get; set; }

        public XncfPageException(IXncfRegister xncfRegister, string message, bool logged = false) : base(message, logged)
        {
            XncfRegister = xncfRegister;
        }

        public XncfPageException(IXncfRegister xncfRegister, string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
            XncfRegister = xncfRegister;
        }
    }
}
