using System;
using System.Collections.Generic;
using CampusLife.Core.Time;

namespace CampusLife.Core.Academics
{
    public enum TermPhase { Orientation, Teaching, Exams, AwaitingResults, Vacation }

    /// <summary>
    /// Semester structure (days counted from the semester's first Monday):
    /// week 0 orientation and registration, four teaching weeks, an exam
    /// week, then results. Compact enough to play through, with each course
    /// meeting once a week.
    /// </summary>
    public static class AcademicCalendar
    {
        public const int TeachingWeeks = 4;
        public const int TeachingStartDay = 7;
        public const int RegistrationCloseDay = 13;          // end of teaching week 1
        public const int ExamStartDay = TeachingStartDay + TeachingWeeks * 7; // 35
        public const int ExamDays = 5;
        public const int ResultsDay = ExamStartDay + 7;       // 42
        public const int AssignmentDueWeek = 3;               // due end of teaching week 3
        public const int QuizWeek = 3;
        public const int LateDayPenalty = 10;
        public const int MaxLateDays = 5;

        public static TermPhase Phase(int day)
        {
            if (day < TeachingStartDay) return TermPhase.Orientation;
            if (day < ExamStartDay) return TermPhase.Teaching;
            if (day < ExamStartDay + ExamDays) return TermPhase.Exams;
            if (day < ResultsDay) return TermPhase.AwaitingResults;
            return TermPhase.Vacation;
        }

        /// <summary>Teaching week 1..4 for a day, or 0 outside teaching.</summary>
        public static int TeachingWeek(int day)
            => Phase(day) == TermPhase.Teaching ? (day - TeachingStartDay) / 7 + 1 : 0;

        public static bool RegistrationOpen(int day) => day >= 0 && day <= RegistrationCloseDay;

        /// <summary>Last minute (absolute game minutes) to hand in an assignment on time.</summary>
        public static double AssignmentDeadline => (TeachingStartDay + AssignmentDueWeek * 7) * (double)GameClock.MinutesPerDay - 1;
    }

    public struct Meeting
    {
        public string CourseCode;
        public Weekday Day;
        public int StartMinute;
        public int DurationMinutes;
        public string Room;
    }

    public sealed class Session
    {
        public string CourseCode;
        public int Week;
        public int Day;
        public int StartMinute;
        public int DurationMinutes;
        public string Room;
        public bool IsExam;

        public double Start => Day * (double)GameClock.MinutesPerDay + StartMinute;
        public double End => Start + DurationMinutes;
        public int TopicIndex => Week - 1;
        public bool HasQuiz => !IsExam && Week == AcademicCalendar.QuizWeek;
        public string Key => IsExam ? $"{CourseCode}:exam" : $"{CourseCode}:w{Week}";
    }

    /// <summary>Builds clash-free weekly timetables and the exam timetable.</summary>
    public static class TimetableBuilder
    {
        public static readonly int[] SlotStarts = { 8 * 60, 10 * 60 + 30, 13 * 60, 15 * 60 + 30 };
        public const int LectureMinutes = 120;
        public const int ExamMinutes = 120;
        public static readonly string[] Rooms = { "LT1", "LT2" };

        /// <summary>
        /// Each course gets one weekly two-hour slot (Mon–Fri × 4). A course
        /// prefers a slot derived from its code and takes the next free one on a clash.
        /// </summary>
        public static List<Meeting> Weekly(IEnumerable<string> courseCodes)
        {
            var codes = new List<string>(courseCodes);
            codes.Sort(StringComparer.Ordinal);
            int slots = 5 * SlotStarts.Length;
            var taken = new bool[slots];
            var result = new List<Meeting>();
            foreach (var code in codes)
            {
                int preferred = (int)(Npcs.DeterministicRandom.Hash(code) % (ulong)slots);
                int slot = -1;
                for (int i = 0; i < slots; i++)
                {
                    int s = (preferred + i * 7) % slots;
                    if (!taken[s]) { slot = s; break; }
                }
                if (slot < 0) throw new InvalidOperationException("Too many courses for the weekly timetable.");
                taken[slot] = true;
                result.Add(new Meeting
                {
                    CourseCode = code,
                    Day = (Weekday)(slot / SlotStarts.Length),
                    StartMinute = SlotStarts[slot % SlotStarts.Length],
                    DurationMinutes = LectureMinutes,
                    Room = Rooms[slot % Rooms.Length],
                });
            }
            result.Sort((a, b) => a.Day != b.Day ? a.Day.CompareTo(b.Day) : a.StartMinute.CompareTo(b.StartMinute));
            return result;
        }

        /// <summary>All teaching sessions plus one exam per course in exam week (09:00 or 14:00).</summary>
        public static List<Session> Sessions(IEnumerable<string> courseCodes)
        {
            var codes = new List<string>(courseCodes);
            codes.Sort(StringComparer.Ordinal);
            var sessions = new List<Session>();
            foreach (var m in Weekly(codes))
                for (int week = 1; week <= AcademicCalendar.TeachingWeeks; week++)
                    sessions.Add(new Session
                    {
                        CourseCode = m.CourseCode, Week = week,
                        Day = AcademicCalendar.TeachingStartDay + (week - 1) * 7 + (int)m.Day,
                        StartMinute = m.StartMinute, DurationMinutes = m.DurationMinutes, Room = m.Room,
                    });
            for (int i = 0; i < codes.Count; i++)
                sessions.Add(new Session
                {
                    CourseCode = codes[i], Week = 0, IsExam = true,
                    Day = AcademicCalendar.ExamStartDay + i % AcademicCalendar.ExamDays,
                    StartMinute = i < AcademicCalendar.ExamDays ? 9 * 60 : 14 * 60,
                    DurationMinutes = ExamMinutes, Room = Rooms[0],
                });
            sessions.Sort((a, b) => a.Start.CompareTo(b.Start));
            return sessions;
        }
    }
}
