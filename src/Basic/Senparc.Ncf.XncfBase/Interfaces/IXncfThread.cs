using Senparc.Ncf.XncfBase.Threads;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    ///XNCF thread module interface
    /// </summary>
    public interface IXncfThread
    {
        /// <summary>
        ///thread configuration
        /// </summary>
        /// <param name="xncfThreadBuilder"></param>
        void ThreadConfig(XncfThreadBuilder xncfThreadBuilder);
    }
}
