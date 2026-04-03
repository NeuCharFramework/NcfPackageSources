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
//                result = "just";
//            }
//            else if (timeSpan.TotalMinutes < 59)
//            {
//                result = "{0} minutes ago".With((int)timeSpan.TotalMinutes);
//            }
//            else if (timeSpan.TotalHours < 24)
//            {
//                result = "{0} hours ago".With((int)timeSpan.TotalHours);
//            }
//            else
//            {
//                result = "{0} days ago".With((int)timeSpan.TotalDays);
//            }
//            return result;
//        }

//    }
//}
