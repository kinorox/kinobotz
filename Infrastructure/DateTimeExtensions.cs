namespace Infrastructure
{
    public static class DateTimeExtensions
    {
        public static string GetPrettyDate(this DateTime d)
        {
            var s = DateTime.Now.Subtract(d);
            var dayDiff = (int)s.TotalDays;
            var secDiff = (int)s.TotalSeconds;

            return dayDiff switch
            {
                < 0 => null!,
                >= 31 => null!,
                0 when secDiff < 60 => "just now",
                0 when secDiff < 120 => "1 minute ago",
                0 when secDiff < 3600 => $"{Math.Floor((double) secDiff / 60)} minutes ago",
                0 when secDiff < 7200 => "1 hour ago",
                0 when secDiff < 86400 => $"{Math.Floor((double) secDiff / 3600)} hours ago",
                1 => "yesterday",
                _ => dayDiff < 7 ? $"{dayDiff} days ago" : $"{Math.Ceiling((double) dayDiff / 7)} weeks ago"
            };
        }
    }
}
