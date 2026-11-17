/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: IXncfThread.cs

    File description: IXncfThread related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.Threads;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface IXncfThread
    {
        /// <summary>
        /// threadconfiguration
        /// </summary>
        /// <param name="xncfThreadBuilder"></param>
        void ThreadConfig(XncfThreadBuilder xncfThreadBuilder);
    }
}
