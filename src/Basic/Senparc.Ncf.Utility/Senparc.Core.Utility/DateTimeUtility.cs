/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: DateTimeUtility.cs

    File description: DateTimeUtility related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Utility
{
    public static class DateTimeUtility
    {
        public static long GetJavascriptTimestamp(System.DateTime input)
        {
            return input.AddTicks((-1) * DateTime.Parse("1970-1-1").Ticks).Ticks / 10000;
            //System.TimeSpan span = new System.TimeSpan(System.DateTime.Parse("1970-1-1").Ticks);
            //System.DateTime time =  input.Subtract(span);
            //return (int)((int)time.Ticks / 10000);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        public static DateTime GetUsableBirthday(int year, int month, int day)
        {
            year = Math.Min(DateTime.Now.Year, Math.Max(1921, year));//Legacy comment translated to English.
            month = Math.Min(12, Math.Max(1, month));//Legacy comment translated to English.
            day = Math.Min(31, Math.Max(1, day));//Legacy comment translated to English.
            DateTime birthday;
            while (true)
            {
                if (DateTime.TryParse($"{year}-{month}-{day}", out birthday))
                {
                    break;
                }
                else
                {
                    day--;//Legacy comment translated to English.
                }
            }
            return birthday;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        public static DateTime GetStaturdayInWeek(DateTime date)
        {
            DateTime staturday = date.Date;
            while (staturday.DayOfWeek != DayOfWeek.Saturday)
            {
                staturday = staturday.AddDays(1);
            }
            return staturday;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        public static DateTime GetSundayInWeek(DateTime date)
        {
            DateTime sunday = date.Date;
            while (sunday.DayOfWeek != DayOfWeek.Sunday)
            {
                sunday = sunday.AddDays(-1);
            }
            return sunday;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        public static int GetWeekOfYear(DateTime date)
        {
            var firstDay = new DateTime(date.Year, 1, 1);
            int firstWeekendDayOfYear = 7 - (int)firstDay.DayOfWeek;//Legacy comment translated to English.
            int lastWeek = (date.DayOfYear - firstWeekendDayOfYear + 6) / 7;
            return lastWeek + 1;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        public static DateTime GetSundayOfWeek(int year, int weeek)
        {
            var firstDay = new DateTime(year, 1, 1);
            int firstWeekend = 7 - (int)firstDay.DayOfWeek;//Legacy comment translated to English.
            int dayOfYear = firstWeekend + (weeek - 2) * 7;
            return firstDay.AddDays(dayOfYear);
        }
    }
}