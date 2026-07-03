/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IXncfThread.cs
    文件功能描述：IXncfThread 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
