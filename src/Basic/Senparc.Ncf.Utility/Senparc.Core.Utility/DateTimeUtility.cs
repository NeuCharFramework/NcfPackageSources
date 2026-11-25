/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DateTimeUtility.cs
    File description: DateTimeUtility related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Utility
{
    /// <summary>
    /// Provides legacy date and week calculations used by NCF application code.
    /// </summary>
    public static class DateTimeUtility
    {
        /// <summary>
        /// Converts a <see cref="DateTime"/> to a JavaScript timestamp measured in milliseconds
        /// from the Unix epoch.
        /// </summary>
        /// <param name="input">The date and time to convert.</param>
        /// <returns>The Unix timestamp in milliseconds.</returns>
        public static long GetJavascriptTimestamp(System.DateTime input)
        {
            return input.AddTicks((-1) * DateTime.Parse("1970-1-1").Ticks).Ticks / 10000;
            //System.TimeSpan span = new System.TimeSpan(System.DateTime.Parse("1970-1-1").Ticks);
            //System.DateTime time =  input.Subtract(span);
            //return (int)((int)time.Ticks / 10000);
        }

        /// <summary>
        /// Clamps the supplied year, month, and day to the supported legacy birthday range
        /// and returns the nearest constructible date when the combination is invalid.
        /// </summary>
        /// <param name="year">The requested year, clamped to the current year and 1921.</param>
        /// <param name="month">The requested month, clamped to 1 through 12.</param>
        /// <param name="day">The requested day, clamped to 1 through 31 and reduced until valid.</param>
        /// <returns>A constructible birthday date.</returns>
        public static DateTime GetUsableBirthday(int year, int month, int day)
        {
            year = Math.Min(DateTime.Now.Year, Math.Max(1921, year));
            month = Math.Min(12, Math.Max(1, month));
            day = Math.Min(31, Math.Max(1, day));
            DateTime birthday;
            while (true)
            {
                if (DateTime.TryParse($"{year}-{month}-{day}", out birthday))
                {
                    break;
                }
                else
                {
                    day--;
                }
            }
            return birthday;
        }

        /// <summary>
        /// Gets the Saturday on or after the date's calendar day.
        /// </summary>
        /// <param name="date">The date from which the search begins.</param>
        /// <returns>The Saturday in the same or following week.</returns>
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
        /// Gets the Sunday on or before the date's calendar day.
        /// </summary>
        /// <param name="date">The date from which the search begins.</param>
        /// <returns>The Sunday in the same or preceding week.</returns>
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
        /// Calculates the legacy week number used by this utility.
        /// </summary>
        /// <param name="date">The date whose week number is required.</param>
        /// <returns>The calculated one-based week number.</returns>
        public static int GetWeekOfYear(DateTime date)
        {
            var firstDay = new DateTime(date.Year, 1, 1);
            int firstWeekendDayOfYear = 7 - (int)firstDay.DayOfWeek;
            int lastWeek = (date.DayOfYear - firstWeekendDayOfYear + 6) / 7;
            return lastWeek + 1;
        }

        /// <summary>
        /// Returns the Sunday associated with a legacy week number in a given year.
        /// </summary>
        /// <param name="year">The calendar year.</param>
        /// <param name="weeek">The one-based week number used by the legacy calculation.</param>
        /// <returns>The calculated Sunday date.</returns>
        public static DateTime GetSundayOfWeek(int year, int weeek)
        {
            var firstDay = new DateTime(year, 1, 1);
            int firstWeekend = 7 - (int)firstDay.DayOfWeek;
            int dayOfYear = firstWeekend + (weeek - 2) * 7;
            return firstDay.AddDays(dayOfYear);
        }
    }
}
