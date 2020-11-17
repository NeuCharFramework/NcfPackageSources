using Senparc.Ncf.XncfBase.Threads;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// XNCF 线程模块接口
    /// </summary>
    public interface IXncfThread
    {
        /// <summary>
        /// 线程配置
        /// </summary>
        /// <param name="xncfThreadBuilder"></param>
        void ThreadConfig(XncfThreadBuilder xncfThreadBuilder);
    }
}
