using System;
using System.Collections.Generic;

namespace CampusLife.Core.Time
{
    public enum DayPeriod { Night, Morning, Afternoon, Evening }

    public enum Weekday { Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday }

    /// <summary>
    /// Accelerated in-game clock. Time is stored as total game minutes since
    /// Day 0 00:00 (a Monday). Several systems can pause the clock at once
    /// (menus, conversations), so pausing is reference-counted by reason.
    /// </summary>
    public sealed class GameClock
    {
        public const int MinutesPerHour = 60;
        public const int MinutesPerDay = 24 * MinutesPerHour;
        public const int DaysPerWeek = 7;

        /// <summary>
        /// Default pace: 0.3 game minutes per real second (an 80-minute day). Slow
        /// enough that a resident can walk across campus between schedule blocks.
        /// </summary>
        public const double DefaultMinutesPerRealSecond = 0.3;

        readonly HashSet<string> _pauseReasons = new HashSet<string>();
        double _totalMinutes;

        /// <summary>Game minutes that pass per real-time second while running.</summary>
        public double MinutesPerRealSecond { get; set; }

        public event Action<GameClock> HourChanged;
        public event Action<GameClock> DayChanged;

        public GameClock(double startMinutes = 7 * MinutesPerHour, double minutesPerRealSecond = DefaultMinutesPerRealSecond)
        {
            if (startMinutes < 0) throw new ArgumentOutOfRangeException(nameof(startMinutes));
            _totalMinutes = startMinutes;
            MinutesPerRealSecond = minutesPerRealSecond;
        }

        public double TotalMinutes => _totalMinutes;
        public int Day => (int)(_totalMinutes / MinutesPerDay);
        public int MinuteOfDay => (int)(_totalMinutes - (double)Day * MinutesPerDay);
        public int Hour => MinuteOfDay / MinutesPerHour;
        public int Minute => MinuteOfDay % MinutesPerHour;
        public Weekday Weekday => (Weekday)(Day % DaysPerWeek);
        public bool IsWeekend => Weekday == Weekday.Saturday || Weekday == Weekday.Sunday;
        public bool IsPaused => _pauseReasons.Count > 0;

        /// <summary>0..1 fraction of the current day, useful for sun position.</summary>
        public float DayFraction => MinuteOfDay / (float)MinutesPerDay;

        public DayPeriod Period => PeriodForHour(Hour);

        public static DayPeriod PeriodForHour(int hour)
        {
            if (hour >= 5 && hour < 12) return DayPeriod.Morning;
            if (hour >= 12 && hour < 17) return DayPeriod.Afternoon;
            if (hour >= 17 && hour < 21) return DayPeriod.Evening;
            return DayPeriod.Night;
        }

        public void Pause(string reason)
        {
            if (string.IsNullOrEmpty(reason)) throw new ArgumentException("A pause reason is required.", nameof(reason));
            _pauseReasons.Add(reason);
        }

        public void Resume(string reason) => _pauseReasons.Remove(reason);

        public bool IsPausedBy(string reason) => _pauseReasons.Contains(reason);

        /// <summary>Advances by real seconds, honouring pause state and time scale.</summary>
        public void Tick(double realSeconds)
        {
            if (IsPaused || realSeconds <= 0) return;
            AdvanceMinutes(realSeconds * MinutesPerRealSecond);
        }

        /// <summary>Advances by game minutes regardless of pause state (sleeping, skipping time).</summary>
        public void AdvanceMinutes(double minutes)
        {
            if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes), "Time cannot run backwards.");
            int oldHourIndex = (int)(_totalMinutes / MinutesPerHour);
            int oldDay = Day;
            _totalMinutes += minutes;
            int newHourIndex = (int)(_totalMinutes / MinutesPerHour);
            if (newHourIndex != oldHourIndex) HourChanged?.Invoke(this);
            if (Day != oldDay) DayChanged?.Invoke(this);
        }

        /// <summary>Restores a saved time without raising change events.</summary>
        public void Restore(double totalMinutes)
        {
            if (totalMinutes < 0) throw new ArgumentOutOfRangeException(nameof(totalMinutes));
            _totalMinutes = totalMinutes;
        }

        public static string WeekdayShort(Weekday day) => day.ToString().Substring(0, 3);

        public string Format() => $"Day {Day + 1} · {WeekdayShort(Weekday)} {Hour:00}:{Minute:00}";
    }
}
