/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FileUtility.cs
    文件功能描述：FileUtility 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Utility
{
    public class FileUtility
    {
        /// <summary>
        /// 获取随机文件名
        /// </summary>
        /// <returns></returns>
        public static string GetRandomFileName()
        {
            return $"{DateTime.Now.Ticks}{Guid.NewGuid().ToString("n").Substring(0, 8)}";
        }
    }
}