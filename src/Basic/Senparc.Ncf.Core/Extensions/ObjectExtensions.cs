/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ObjectExtensions.cs
    文件功能描述：ObjectExtensions 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

namespace Senparc.Ncf.Core.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// 判断对象是否为null
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsNull(object obj)
        {
            return obj == null;
        }
    }
}