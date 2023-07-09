namespace Infrastructure.Extensions
{
    public static class StringExtensions
    {
        public static string Mask(this string source, int start, int maskLength)
        {
            return source.Mask(start, maskLength, 'X');
        }

        public static string Mask(this string source, int start, int maskLength, char maskCharacter)
        {
            if (start > source.Length - 1)
            {
                throw new ArgumentException("Start position is greater than string length");
            }

            if (maskLength > source.Length)
            {
                throw new ArgumentException("Mask length is greater than string length");
            }

            if (start + maskLength > source.Length)
            {
                throw new ArgumentException("Start position and mask length imply more characters than are present");
            }

            string mask = new string(maskCharacter, maskLength);
            string unMaskStart = source.Substring(0, start);
            string unMaskEnd = source.Substring(start + maskLength, source.Length - maskLength);

            return unMaskStart + mask + unMaskEnd;
        }
        public static DateTime ConvertTimestampToDateTime(this string s)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            double d = double.Parse(s);
            dtDateTime = dtDateTime.AddMilliseconds(d).ToLocalTime();
            return dtDateTime;
        }
        public static string GetUntilOrEmpty(this string text, string stopAt = "-")
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

            return charLocation > 0 ? text[..charLocation] : string.Empty;
        }
    }
}
