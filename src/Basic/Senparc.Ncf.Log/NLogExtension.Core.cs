/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NLogExtension.Core.cs
    文件功能描述：NLogExtension.Core 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using NLog;
using Senparc.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Log
{
  public static  class NLogExtension
    {
        /// <summary>
        /// 记录错误信息的扩展方法
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="stringFormat"></param>
        /// <param name="args"></param>
        public static void ErrorFormat(this Logger logger, string stringFormat, params object[] args)
        {
            logger.Error(stringFormat.With(args));
        }
    }
}
