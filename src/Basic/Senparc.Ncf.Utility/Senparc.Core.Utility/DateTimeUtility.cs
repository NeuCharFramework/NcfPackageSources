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
        /// Get the correct available birthday
        /// </summary>
        /// <returns></returns>
        public static DateTime GetUsableBirthday(int year, int month, int day)
        {
            year = Math.Min(DateTime.Now.Year, Math.Max(1921, year));//Make sure the year is between 1921 and this year
            month = Math.Min(12, Math.Max(1, month));//Make sure the month is between January and December
            day = Math.Min(31, Math.Max(1, day));//Make sure the day is between 1-31
            DateTime birthday;
            while (true)
            {
                if (DateTime.TryParse($"{year}-{month}-{day}", out birthday))
                {
                    break;
                }
                else
                {
                    day--;//If the date is incorrect, subtract one
                }
            }
            return birthday;
        }

        /// <summary>
        /// Get the date of this Saturday (Saturday is the last day of the week)
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
        /// Get the date of this Sunday (Sunday is the first day of the week)
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
        /// Get the current week of the year
        /// </summary>
        /// <returns></returns>
        public static int GetWeekOfYear(DateTime date)
        {
            var firstDay = new DateTime(date.Year, 1, 1);
            int firstWeekendDayOfYear = 7 - (int)firstDay.DayOfWeek;//When is the first weekend (Saturday)?
            int lastWeek = (date.DayOfYear - firstWeekendDayOfYear + 6) / 7;
            return lastWeek + 1;
        }

        /// <summary>
        /// Get the first day of the specified week (Sunday)
        /// </summary>
        /// <returns></returns>
        public static DateTime GetSundayOfWeek(int year, int weeek)
        {
            var firstDay = new DateTime(year, 1, 1);
            int firstWeekend = 7 - (int)firstDay.DayOfWeek;//When is the first weekend (Saturday)?
            int dayOfYear = firstWeekend + (weeek - 2) * 7;
            return firstDay.AddDays(dayOfYear);
        }
    }
}