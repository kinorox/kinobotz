using System;

namespace twitchBot.Extensions
{
    public static class StringExtensions
    {
        public static DateTime ConvertTimestampToDateTime(this string s)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            double d = double.Parse(s);
            dtDateTime = dtDateTime.AddMilliseconds(d).ToLocalTime();
            return dtDateTime;
        }
    }
}
