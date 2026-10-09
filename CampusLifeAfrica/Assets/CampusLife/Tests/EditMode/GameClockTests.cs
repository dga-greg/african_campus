using System;
using CampusLife.Core.Time;
using NUnit.Framework;

namespace CampusLife.Tests
{
    public class GameClockTests
    {
        [Test]
        public void StartsAtSevenOnMondayOfDayOne()
        {
            var clock = new GameClock();
            Assert.AreEqual(0, clock.Day);
            Assert.AreEqual(7, clock.Hour);
            Assert.AreEqual(Weekday.Monday, clock.Weekday);
            Assert.AreEqual(DayPeriod.Morning, clock.Period);
            Assert.AreEqual("Day 1 · Mon 07:00", clock.Format());
        }

        [Test]
        public void TickUsesTimeScale()
        {
            var clock = new GameClock(0, minutesPerRealSecond: 2);
            clock.Tick(30);
            Assert.AreEqual(60, clock.TotalMinutes, 1e-9);
            Assert.AreEqual(1, clock.Hour);
        }

        [Test]
        public void PauseIsReferenceCountedByReason()
        {
            var clock = new GameClock(0);
            clock.Pause("menu");
            clock.Pause("dialogue");
            clock.Tick(10);
            Assert.AreEqual(0, clock.TotalMinutes);
            clock.Resume("menu");
            Assert.IsTrue(clock.IsPaused);
            clock.Tick(10);
            Assert.AreEqual(0, clock.TotalMinutes);
            clock.Resume("dialogue");
            clock.Tick(10);
            Assert.AreEqual(10, clock.TotalMinutes, 1e-9);
        }

        [Test]
        public void RaisesHourAndDayEvents()
        {
            var clock = new GameClock(23 * 60 + 30);
            int hours = 0, days = 0;
            clock.HourChanged += _ => hours++;
            clock.DayChanged += _ => days++;
            clock.AdvanceMinutes(20);
            Assert.AreEqual(0, hours);
            clock.AdvanceMinutes(20);
            Assert.AreEqual(1, hours);
            Assert.AreEqual(1, days);
            Assert.AreEqual(Weekday.Tuesday, clock.Weekday);
        }

        [Test]
        public void WeekendsAndPeriods()
        {
            var clock = new GameClock(5 * GameClock.MinutesPerDay + 18 * 60);
            Assert.AreEqual(Weekday.Saturday, clock.Weekday);
            Assert.IsTrue(clock.IsWeekend);
            Assert.AreEqual(DayPeriod.Evening, clock.Period);
            Assert.AreEqual(DayPeriod.Night, GameClock.PeriodForHour(2));
            Assert.AreEqual(DayPeriod.Afternoon, GameClock.PeriodForHour(13));
        }

        [Test]
        public void RejectsNegativeTime()
        {
            var clock = new GameClock();
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.AdvanceMinutes(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Restore(-5));
            Assert.Throws<ArgumentException>(() => clock.Pause(""));
        }
    }
}
