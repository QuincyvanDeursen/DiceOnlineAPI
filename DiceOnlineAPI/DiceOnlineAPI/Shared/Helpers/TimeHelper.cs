namespace DiceOnlineAPI.Shared.Helpers
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo AmsterdamTimeZone = GetAmsterdamTimeZone();

        public static DateTime AmsterdamNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AmsterdamTimeZone);

        public static DateTime ToAmsterdamTime(DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, AmsterdamTimeZone);
        }

        private static TimeZoneInfo GetAmsterdamTimeZone()
        {
            try
            {
                // Probeer eerst IANA (Linux/Mac)
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    // Fallback naar Windows ID
                    return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
                }
                catch (TimeZoneNotFoundException)
                {
                    // Laatste fallback: UTC+1/+2 handmatig
                    return TimeZoneInfo.CreateCustomTimeZone(
                        "Amsterdam",
                        TimeSpan.FromHours(1),
                        "Amsterdam Time",
                        "Amsterdam Time");
                }
            }
        }
    }
}