/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IntegerExtensions.cs
    文件功能描述：IntegerExtensions 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

namespace Senparc.Ncf.Core.Extensions
{
    public static class IntegerExtensions
    {
        public static string ThousandChange(this int num)
        {
            if (num > 999)
            {
                num = num / 1000;
                return num.ToString() + "K";
            }
            else
            {
                return num.ToString();
            }
        }
    }
}
