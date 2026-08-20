/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfUninstallException.cs
    文件功能描述：NcfUninstallException 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core
{
    /// <summary>
    /// NCF 未安装
    /// </summary>
    public class NcfUninstallException : NcfExceptionBase
    {
        public NcfUninstallException(string message, bool logged = false) : this(message, null, logged)
        {
        }

        public NcfUninstallException(string message, Exception inner = null, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
