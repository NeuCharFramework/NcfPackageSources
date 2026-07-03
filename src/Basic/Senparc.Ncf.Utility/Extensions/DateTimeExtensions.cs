/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DateTimeExtensions.cs
    文件功能描述：DateTimeExtensions 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

//using System;

//namespace Senparc.Ncf.Core.Extensions
//{
//    public static class DateTimeExtensions
//    {
//        public static string ToShortTime(this DateTime dt)
//        {
//            string result = null;
//            TimeSpan timeSpan = DateTime.Now - dt;
//            if (timeSpan.TotalSeconds < 59)
//            {
//                result = "刚刚";
//            }
//            else if (timeSpan.TotalMinutes < 59)
//            {
//                result = "{0}分钟前".With((int)timeSpan.TotalMinutes);
//            }
//            else if (timeSpan.TotalHours < 24)
//            {
//                result = "{0}小时前".With((int)timeSpan.TotalHours);
//            }
//            else
//            {
//                result = "{0}天前".With((int)timeSpan.TotalDays);
//            }
//            return result;
//        }

//    }
//}
