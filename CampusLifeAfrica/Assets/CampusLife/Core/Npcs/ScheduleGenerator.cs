using System;
using System.Collections.Generic;
using CampusLife.Core.Player;
using CampusLife.Core.Time;
using static CampusLife.Core.Npcs.PlaceIds;

namespace CampusLife.Core.Npcs
{
    /// <summary>
    /// Builds a deterministic daily schedule from an NPC's role, programme,
    /// traits, the weekday and the day number. Start times are jittered per
    /// NPC and per day so residents never all move at once or repeat the
    /// exact same routine every day.
    /// </summary>
    public static class ScheduleGenerator
    {
        public static DailySchedule Generate(NpcProfile npc, int day)
        {
            var rng = DeterministicRandom.From(npc.Id, day + 1);
            var weekday = (Weekday)(day % GameClock.DaysPerWeek);
            bool weekend = weekday == Weekday.Saturday || weekday == Weekday.Sunday;
            var b = new Builder();
            switch (npc.Role)
            {
                case NpcRole.Lecturer: Lecturer(b, ref rng, weekend); break;
                case NpcRole.Security: Security(b, ref rng, npc.NightShift); break;
                case NpcRole.Cleaner: Cleaner(b, ref rng, weekday); break;
                default:
                    if (weekend) StudentWeekend(b, ref rng, npc);
                    else StudentWeekday(b, ref rng, npc, weekday);
                    break;
            }
            return b.Build();
        }

        /// <summary>Whether a programme has a lecture block in the morning (true) or afternoon (false) on a weekday.</summary>
        public static bool HasLecture(string programId, Weekday day, bool morning)
        {
            int index = 0;
            for (int i = 0; i < AcademicPrograms.All.Count; i++)
                if (AcademicPrograms.All[i].Id == programId) index = i;
            int d = (int)day;
            if (d >= 5) return false;
            // Even programmes: Mon/Wed/Fri mornings, Tue/Thu afternoons; odd programmes the reverse.
            bool mwf = d % 2 == 0;
            bool morningDay = index % 2 == 0 ? mwf : !mwf;
            // Every programme also has one extra afternoon practical, spread across the week.
            bool practical = !morning && d == index % 5;
            return morning ? morningDay : (!morningDay || practical);
        }

        static void StudentWeekday(Builder b, ref DeterministicRandom rng, NpcProfile npc, Weekday day)
        {
            b.At(0, Hostel, ActivityKind.Sleep);
            b.At(380 + rng.Range(0, 50), Kiosk, ActivityKind.Eat);
            if (HasLecture(npc.ProgramId, day, true))
                b.At(445 + rng.Range(0, 12), LectureComplex, ActivityKind.Lecture);
            else
                FreeTime(b, ref rng, npc, 450 + rng.Range(0, 25));
            FreeTime(b, ref rng, npc, 600 + rng.Range(0, 25));
            b.At(720 + rng.Range(0, 25), Kiosk, ActivityKind.Eat);
            if (HasLecture(npc.ProgramId, day, false))
                b.At(770 + rng.Range(0, 12), LectureComplex, ActivityKind.Lecture);
            else
                FreeTime(b, ref rng, npc, 780 + rng.Range(0, 30));
            if (npc.Studious > 0.65f) b.At(905 + rng.Range(0, 20), Library, ActivityKind.Study);
            else b.At(905 + rng.Range(0, 20), Plaza, ActivityKind.Socialize);
            Evening(b, ref rng, npc);
        }

        static void StudentWeekend(Builder b, ref DeterministicRandom rng, NpcProfile npc)
        {
            b.At(0, Hostel, ActivityKind.Sleep);
            b.At(510 + rng.Range(0, 70), Kiosk, ActivityKind.Eat);
            FreeTime(b, ref rng, npc, 610 + rng.Range(0, 30));
            b.At(750 + rng.Range(0, 30), Kiosk, ActivityKind.Eat);
            if (rng.Chance(0.35))
            {
                // A trip into town on the campus shuttle.
                int leave = 810 + rng.Range(0, 30);
                b.At(leave, ShuttleStop, ActivityKind.WaitForShuttle);
                b.At(leave + 35 + rng.Range(0, 10), OffCampus, ActivityKind.OffCampus);
                b.At(1020 + rng.Range(0, 40), Hostel, ActivityKind.Sleep);
            }
            else
            {
                FreeTime(b, ref rng, npc, 820 + rng.Range(0, 30));
            }
            Evening(b, ref rng, npc);
        }

        static void Evening(Builder b, ref DeterministicRandom rng, NpcProfile npc)
        {
            int t = 1020 + rng.Range(0, 20);
            if (npc.Sporty > 0.55f) b.At(t, SportsField, ActivityKind.Exercise);
            else if (npc.Social > 0.5f) b.At(t, Plaza, ActivityKind.Socialize);
            else b.At(t, Lawn, ActivityKind.Relax);
            int home = 1110 + rng.Range(0, 45);
            b.At(home, Hostel, ActivityKind.Sleep);
            if (npc.Social > 0.72f)
            {
                b.At(home + 50 + rng.Range(0, 20), Plaza, ActivityKind.Socialize);
                b.At(1290 + rng.Range(0, 40), Hostel, ActivityKind.Sleep);
            }
        }

        /// <summary>Unscheduled time, chosen by personality.</summary>
        static void FreeTime(Builder b, ref DeterministicRandom rng, NpcProfile npc, int start)
        {
            double roll = rng.NextDouble();
            if (roll < npc.Studious * 0.7) b.At(start, Library, ActivityKind.Study);
            else if (roll < npc.Studious * 0.7 + npc.Social * 0.5) b.At(start, Plaza, ActivityKind.Socialize);
            else if (npc.Sporty > 0.7f && rng.Chance(0.5)) b.At(start, SportsField, ActivityKind.Exercise);
            else b.At(start, Lawn, ActivityKind.Relax);
        }

        static void Lecturer(Builder b, ref DeterministicRandom rng, bool weekend)
        {
            b.At(0, OffCampus, ActivityKind.OffCampus);
            if (weekend) return;
            b.At(425 + rng.Range(0, 25), LectureComplex, ActivityKind.Lecture);
            b.At(705 + rng.Range(0, 15), Kiosk, ActivityKind.Eat);
            b.At(750 + rng.Range(0, 15), Library, ActivityKind.Study);
            b.At(870 + rng.Range(0, 20), LectureComplex, ActivityKind.Lecture);
            b.At(1005 + rng.Range(0, 30), OffCampus, ActivityKind.OffCampus);
        }

        static void Security(Builder b, ref DeterministicRandom rng, bool night)
        {
            if (night)
            {
                b.At(0, PatrolRoute, ActivityKind.Patrol);
                b.At(120 + rng.Range(0, 15), SecurityPost, ActivityKind.Work);
                b.At(240 + rng.Range(0, 15), PatrolRoute, ActivityKind.Patrol);
                b.At(370 + rng.Range(0, 10), OffCampus, ActivityKind.OffCampus);
                b.At(1070 + rng.Range(0, 10), SecurityPost, ActivityKind.Work);
                b.At(1140 + rng.Range(0, 15), PatrolRoute, ActivityKind.Patrol);
                b.At(1260 + rng.Range(0, 15), SecurityPost, ActivityKind.Work);
                b.At(1350 + rng.Range(0, 15), PatrolRoute, ActivityKind.Patrol);
                return;
            }
            b.At(0, OffCampus, ActivityKind.OffCampus);
            b.At(350 + rng.Range(0, 10), SecurityPost, ActivityKind.Work);
            b.At(480 + rng.Range(0, 15), PatrolRoute, ActivityKind.Patrol);
            b.At(600 + rng.Range(0, 15), SecurityPost, ActivityKind.Work);
            b.At(725 + rng.Range(0, 10), Kiosk, ActivityKind.Eat);
            b.At(760 + rng.Range(0, 10), PatrolRoute, ActivityKind.Patrol);
            b.At(900 + rng.Range(0, 15), SecurityPost, ActivityKind.Work);
            b.At(1085 + rng.Range(0, 10), OffCampus, ActivityKind.OffCampus);
        }

        static void Cleaner(Builder b, ref DeterministicRandom rng, Weekday day)
        {
            b.At(0, OffCampus, ActivityKind.OffCampus);
            if (day == Weekday.Sunday) return;
            b.At(355 + rng.Range(0, 15), SweepRoute, ActivityKind.Clean);
            if (day == Weekday.Saturday)
            {
                b.At(600 + rng.Range(0, 20), OffCampus, ActivityKind.OffCampus);
                return;
            }
            b.At(630 + rng.Range(0, 15), Kiosk, ActivityKind.Eat);
            b.At(665 + rng.Range(0, 10), SweepRoute, ActivityKind.Clean);
            b.At(840 + rng.Range(0, 15), OffCampus, ActivityKind.OffCampus);
        }

        /// <summary>
        /// Shortest block per activity, including the walk there. Meals are long
        /// because the kiosk is a walk from most of campus.
        /// </summary>
        static int MinimumBlock(ActivityKind activity)
        {
            switch (activity)
            {
                case ActivityKind.Eat: return 65;
                case ActivityKind.WaitForShuttle: return 35;
                default: return 20;
            }
        }

        /// <summary>Collects entries, keeping them ordered and leaving each block its minimum length.</summary>
        sealed class Builder
        {
            readonly List<ScheduleEntry> _entries = new List<ScheduleEntry>();

            public void At(int minute, string place, ActivityKind activity)
            {
                if (_entries.Count > 0)
                {
                    var last = _entries[_entries.Count - 1];
                    int earliest = last.StartMinute + MinimumBlock(last.Activity);
                    if (minute < earliest) minute = earliest;
                    if (last.PlaceId == place && last.Activity == activity) return; // no-op change
                }
                if (minute >= 1440) return;
                _entries.Add(new ScheduleEntry(minute, place, activity));
            }

            public DailySchedule Build() => new DailySchedule(_entries);
        }
    }
}
