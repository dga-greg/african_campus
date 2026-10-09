namespace CampusLife.Core.Time
{
    /// <summary>How long resting in your hostel room lasts.</summary>
    public static class RestRules
    {
        public const int WakeMinute = 6 * 60 + 30;
        public const int NapMinutes = 120;

        /// <summary>
        /// From 20:00 to 05:00 you sleep until 06:30; at other times you nap for two hours.
        /// Returns the absolute minute you wake up.
        /// </summary>
        public static double WakeTime(double now)
        {
            int day = (int)(now / GameClock.MinutesPerDay);
            int minute = (int)(now - (double)day * GameClock.MinutesPerDay);
            if (minute >= 20 * 60) return (day + 1) * (double)GameClock.MinutesPerDay + WakeMinute;
            if (minute < 5 * 60) return day * (double)GameClock.MinutesPerDay + WakeMinute;
            return now + NapMinutes;
        }

        public static bool IsNightSleep(double now)
        {
            int minute = (int)(now % GameClock.MinutesPerDay);
            return minute >= 20 * 60 || minute < 5 * 60;
        }
    }
}
