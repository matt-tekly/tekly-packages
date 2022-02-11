using System;

namespace Tekly.Common.Utils
{
    public static class DateUtils
    {
        public static string ShortDate(DateTime date)
        {
            var year = date.Year - 2000;
            return $"{year}_{date.Month:00}_{date.Day:00}";
        }
    }
}