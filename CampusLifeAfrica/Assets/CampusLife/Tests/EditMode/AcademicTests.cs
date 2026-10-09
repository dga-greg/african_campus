using System;
using System.Collections.Generic;
using System.Linq;
using CampusLife.Core.Academics;
using CampusLife.Core.Content;
using CampusLife.Core.Player;
using CampusLife.Core.Save;
using CampusLife.Core.Serialization;
using CampusLife.Core.Session;
using CampusLife.Core.Time;
using NUnit.Framework;

namespace CampusLife.Tests
{
    public class GradingTests
    {
        [Test]
        public void ScaleMapsScoresToGrades()
        {
            var s = GradingScale.Default;
            Assert.AreEqual("A", s.Grade(80).Letter);
            Assert.AreEqual("B+", s.Grade(79).Letter);
            Assert.AreEqual("C", s.Grade(60).Letter);
            Assert.AreEqual("D", s.Grade(50).Letter);
            Assert.AreEqual("F", s.Grade(49).Letter);
            Assert.AreEqual("A", s.Grade(150).Letter, "Clamped");
            Assert.IsTrue(s.Passed(50));
            Assert.IsFalse(s.Passed(49));
        }

        [Test]
        public void GpaIsCreditWeighted()
        {
            Assert.AreEqual(3.25, GradingScale.Gpa(new[] { (3, 4.0), (3, 2.5) }), 1e-9);
            Assert.AreEqual(3.33, GradingScale.Gpa(new[] { (3, 4.0), (3, 4.0), (3, 2.0) }), 1e-9);
            Assert.AreEqual(0, GradingScale.Gpa(new (int, double)[0]));
            Assert.AreEqual("First Class", GradingScale.ClassOfDegree(3.7));
            Assert.AreEqual("Second Class (Upper Division)", GradingScale.ClassOfDegree(3.2));
        }

        [Test]
        public void ScaleMustStartAtZero()
        {
            Assert.Throws<ArgumentException>(() => new GradingScale(new[] { new GradeBand("A", 50, 4) }, 50, 1.5));
        }
    }

    public class CurriculumTests
    {
        [Test]
        public void CatalogIsCompleteAndValid()
        {
            var catalog = Phase3Content.Catalog;
            CollectionAssert.IsEmpty(catalog.Validate());
            foreach (var program in AcademicPrograms.All)
            {
                var offered = catalog.Offered(program.Id, 100, 1);
                Assert.AreEqual(2, offered.Count(c => c.ProgramId == program.Id), $"{program.Id} has two programme courses");
                Assert.AreEqual(4, offered.Count(c => c.Kind == CourseKind.Core), $"{program.Id} has four required courses");
                Assert.AreEqual(2, offered.Count(c => c.Kind == CourseKind.Elective));
            }
            Assert.AreEqual(24, catalog.All.Count());
        }

        [Test]
        public void EveryQuestionIsWellFormed()
        {
            foreach (var c in Phase3Content.Catalog.All)
                foreach (var k in c.AllKeyPoints())
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(k.Text), k.Id);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(k.Question), k.Id);
                    Assert.AreEqual(4, k.Options.Distinct().Count(), $"{k.Id} has four distinct options");
                    Assert.IsTrue(k.Options.All(o => !string.IsNullOrWhiteSpace(o)), k.Id);
                }
        }

        [Test]
        public void ShuffledQuestionsKeepTheRightAnswer()
        {
            foreach (var k in Phase3Content.Catalog.Get("CSC 101").AllKeyPoints())
            {
                var q = PresentedQuestion.Shuffle(k, "exam");
                Assert.AreEqual(k.Options[k.Answer], q.Options[q.Answer]);
                CollectionAssert.AreEquivalent(k.Options, q.Options);
                var removed = q.HintRemovals();
                Assert.AreEqual(2, removed.Length);
                CollectionAssert.DoesNotContain(removed, q.Answer, "A hint never removes the right answer");
                Assert.AreEqual(q.Answer, PresentedQuestion.Shuffle(k, "exam").Answer, "Deterministic");
            }
        }
    }

    public class TimetableTests
    {
        [Test]
        public void WeeklyTimetableHasNoClashes()
        {
            foreach (var program in AcademicPrograms.All)
            {
                // The largest possible load: every course offered to the programme (18 credits).
                var codes = Phase3Content.Catalog.Offered(program.Id, 100, 1).Select(c => c.Code).ToList();
                var meetings = TimetableBuilder.Weekly(codes);
                Assert.AreEqual(codes.Count, meetings.Count);
                Assert.AreEqual(meetings.Count, meetings.Select(m => (m.Day, m.StartMinute)).Distinct().Count(), program.Id);
                Assert.IsTrue(meetings.All(m => (int)m.Day < 5), "Lectures are on weekdays");
            }
            Assert.Throws<InvalidOperationException>(() => TimetableBuilder.Weekly(Enumerable.Range(0, 21).Select(i => "C" + i)));
        }

        [Test]
        public void SessionsCoverTeachingWeeksAndExams()
        {
            var codes = new[] { "AMU 101", "AMU 103", "CSC 101", "CSC 103", "AMU 105" };
            var sessions = TimetableBuilder.Sessions(codes);
            Assert.AreEqual(codes.Length * (AcademicCalendar.TeachingWeeks + 1), sessions.Count);
            foreach (var s in sessions.Where(x => !x.IsExam))
            {
                Assert.AreEqual(TermPhase.Teaching, AcademicCalendar.Phase(s.Day));
                Assert.AreEqual(s.Week, AcademicCalendar.TeachingWeek(s.Day));
            }
            foreach (var s in sessions.Where(x => x.IsExam))
                Assert.AreEqual(TermPhase.Exams, AcademicCalendar.Phase(s.Day));
            Assert.AreEqual(codes.Length, sessions.Where(x => x.HasQuiz).Count(), "One quiz per course, in week 3");
        }

        [Test]
        public void CalendarPhases()
        {
            Assert.AreEqual(TermPhase.Orientation, AcademicCalendar.Phase(0));
            Assert.AreEqual(TermPhase.Teaching, AcademicCalendar.Phase(7));
            Assert.AreEqual(TermPhase.Exams, AcademicCalendar.Phase(35));
            Assert.AreEqual(TermPhase.AwaitingResults, AcademicCalendar.Phase(40));
            Assert.AreEqual(TermPhase.Vacation, AcademicCalendar.Phase(42));
            Assert.IsTrue(AcademicCalendar.RegistrationOpen(13));
            Assert.IsFalse(AcademicCalendar.RegistrationOpen(14));
        }
    }

    public class AcademicRecordTests
    {
        static readonly string[] CsSelection = { "AMU 101", "AMU 103", "CSC 101", "CSC 103", "AMU 105" };
        const double Day = GameClock.MinutesPerDay;

        static AcademicRecord NewRecord(string program = "computer_science")
            => new AcademicRecord(new AcademicData(), Phase3Content.Catalog, program);

        static AcademicRecord Registered()
        {
            var r = NewRecord();
            Assert.IsTrue(r.TryRegister(CsSelection, 2 * Day, out var errors), string.Join(" ", errors));
            return r;
        }

        [Test]
        public void RegistrationEnforcesRules()
        {
            var r = NewRecord();
            Assert.IsEmpty(r.ValidateRegistration(CsSelection));
            Assert.IsTrue(r.ValidateRegistration(new[] { "AMU 101", "AMU 103", "CSC 101", "AMU 105" }).Any(e => e.Contains("CSC 103")), "Missing core course");
            Assert.IsTrue(r.ValidateRegistration(CsSelection.Concat(new[] { "BUS 101" })).Any(e => e.Contains("not offered")), "Other programme's course");
            Assert.IsTrue(r.ValidateRegistration(new[] { "AMU 101", "AMU 103", "CSC 101", "CSC 103" }).Any(e => e.Contains("at least")), "Too few credits");
            Assert.IsEmpty(r.ValidateRegistration(CsSelection.Concat(new[] { "AMU 107" })), "Both electives = 18 credits is allowed");
            Assert.IsTrue(r.ValidateRegistration(new[] { "AMU 101", "AMU 101", "AMU 103", "CSC 101", "CSC 103", "AMU 105" }).Any(e => e.Contains("twice")));

            Assert.IsFalse(r.TryRegister(CsSelection, 20 * Day, out var late), "Registration closed after week 1");
            Assert.IsTrue(late.Any(e => e.Contains("closed")));
            Assert.IsTrue(r.TryRegister(CsSelection, 1 * Day, out _));
            Assert.AreEqual(15, r.RegisteredCredits());
            Assert.IsFalse(r.TryRegister(CsSelection, 1 * Day, out _), "Only once per semester");
        }

        [Test]
        public void PrerequisitesAreChecked()
        {
            var catalog = new CourseCatalog();
            Topic[] Topics(string code) => Enumerable.Range(0, 4).Select(i => new Topic(i, "t", new[]
            {
                new KeyPoint($"{code}{i}a", "x", "q", new[] { "a", "b", "c", "d" }, 0),
                new KeyPoint($"{code}{i}b", "x", "q", new[] { "a", "b", "c", "d" }, 0),
            })).ToArray();
            catalog.Add(new CourseDefinition("X 101", "Basics", 15, "p", CourseKind.Core, "L", 100, 1, Topics("x")));
            catalog.Add(new CourseDefinition("X 102", "Next", 15, "p", CourseKind.Core, "L", 100, 2, Topics("y"), new[] { "X 101" }));
            var data = new AcademicData { semester = 2 };
            var r = new AcademicRecord(data, catalog, "p");
            Assert.IsTrue(r.ValidateRegistration(new[] { "X 102" }).Any(e => e.Contains("requires X 101")));
            data.results.Add(new SemesterResult { year = 1, semester = 1, courses = { new CourseResult { courseCode = "X 101", passed = true, credits = 15, gradePoint = 3 } } });
            Assert.IsEmpty(r.ValidateRegistration(new[] { "X 102" }));
        }

        [Test]
        public void AttendanceDependsOnArrivalTime()
        {
            var r = Registered();
            var s = r.Sessions.First(x => !x.IsExam);
            Assert.AreEqual(AttendanceCheck.TooEarly, r.CheckAttendance(s, s.Start - 90));
            Assert.AreEqual(AttendanceCheck.OnTime, r.CheckAttendance(s, s.Start - 20));
            Assert.AreEqual(AttendanceCheck.OnTime, r.CheckAttendance(s, s.Start + 10));
            Assert.AreEqual(AttendanceCheck.Late, r.CheckAttendance(s, s.Start + 25));
            Assert.AreEqual(AttendanceCheck.TooLate, r.CheckAttendance(s, s.Start + 31));
            Assert.IsFalse(r.AttendLecture(s, s.Start + 45));
            Assert.IsTrue(r.AttendLecture(s, s.Start + 25));
            Assert.AreEqual(AttendanceMark.Late, r.RecordFor(s).mark);
            Assert.IsFalse(r.AttendLecture(s, s.Start), "Recorded once");
            Assert.AreEqual(AttendanceCheck.AlreadyRecorded, r.CheckAttendance(s, s.Start));
            Assert.AreSame(s, r.SessionInRoom(s.Room, s.Start + 5));
            Assert.IsNull(r.SessionInRoom(s.Room, s.End + 1));
        }

        [Test]
        public void ChallengeCountsOnlyOncePerAttendedSession()
        {
            var r = Registered();
            var s = r.Sessions.First(x => !x.IsExam);
            Assert.IsFalse(r.AnswerChallenge(s, true), "Must attend first");
            r.AttendLecture(s, s.Start);
            Assert.IsTrue(r.AnswerChallenge(s, true));
            Assert.IsFalse(r.AnswerChallenge(s, false));
        }

        [Test]
        public void StudyingAddsNotesPreparationAndHints()
        {
            var r = Registered();
            Assert.AreEqual(0, r.HintsAvailable("CSC 101"));
            Assert.IsTrue(r.Study("CSC 101", 0));
            Assert.IsTrue(r.HasNote("CSC101-t1-k1"));
            Assert.IsTrue(r.HasNote("CSC101-t1-k2"));
            r.Study("CSC 101", 1);
            Assert.AreEqual(1, r.HintsAvailable("CSC 101"));
            Assert.IsTrue(r.UseHint("CSC 101"));
            Assert.IsFalse(r.UseHint("CSC 101"));
            for (int i = 0; i < 20; i++) r.Study("CSC 101", 2);
            Assert.AreEqual(AcademicRecord.MaxPreparation, r.Progress("CSC 101").preparation, "Preparation is capped");
            Assert.IsFalse(r.Study("BUS 101", 0), "Not registered");
        }

        [Test]
        public void AssignmentsPenaliseLateness()
        {
            var r = Registered();
            double deadline = r.AssignmentDeadline;
            Assert.IsFalse(r.AssignmentOpen("CSC 101", 3 * Day), "Not set during orientation");
            Assert.AreEqual(67, r.SubmitAssignment("CSC 101", 2, 3, deadline - 60));
            Assert.AreEqual(-1, r.SubmitAssignment("CSC 101", 3, 3, deadline - 30), "Once only");
            Assert.AreEqual(80, r.SubmitAssignment("CSC 103", 3, 3, deadline + Day * 1.5), "Two days late: -20");
            Assert.AreEqual(-1, r.SubmitAssignment("AMU 101", 3, 3, deadline + Day * 6), "Closed after five late days");
        }

        [Test]
        public void QuizAndExamRules()
        {
            var r = Registered();
            var week1 = r.Sessions.First(x => x.CourseCode == "CSC 101" && x.Week == 1);
            var week3 = r.Sessions.First(x => x.CourseCode == "CSC 101" && x.Week == 3);
            Assert.IsFalse(r.RecordQuiz(week1, 4, 4), "No quiz in week 1");
            Assert.IsFalse(r.RecordQuiz(week3, 4, 4), "Must attend");
            r.AttendLecture(week3, week3.Start);
            Assert.IsTrue(r.RecordQuiz(week3, 3, 4));
            Assert.AreEqual(75, r.Progress("CSC 101").quizScore);

            var exam = r.Sessions.First(x => x.CourseCode == "CSC 101" && x.IsExam);
            Assert.IsFalse(r.RecordExam(exam, 8, 8, exam.Start + 45), "Too late to sit");
            Assert.IsTrue(r.RecordExam(exam, 6, 8, exam.Start));
            Assert.AreEqual(75, r.Progress("CSC 101").examScore);
            Assert.IsFalse(r.RecordExam(exam, 8, 8, exam.Start), "Exam taken once");
        }

        [Test]
        public void ResultsCombineComponentsIntoGpaAndCgpa()
        {
            var r = Registered();
            // Excellent work in CSC 101: every lecture on time, all challenges right, full marks.
            foreach (var s in r.Sessions.Where(x => x.CourseCode == "CSC 101" && !x.IsExam))
            {
                r.AttendLecture(s, s.Start);
                r.AnswerChallenge(s, true);
                if (s.HasQuiz) r.RecordQuiz(s, 4, 4);
            }
            r.SubmitAssignment("CSC 101", 3, 3, r.AssignmentDeadline - 100);
            var csExam = r.Sessions.First(x => x.CourseCode == "CSC 101" && x.IsExam);
            r.RecordExam(csExam, 8, 8, csExam.Start);
            // Middling CSC 103: attended half, assignment 67, no quiz, exam 50.
            foreach (var s in r.Sessions.Where(x => x.CourseCode == "CSC 103" && !x.IsExam && x.Week <= 2)) r.AttendLecture(s, s.Start);
            r.SubmitAssignment("CSC 103", 2, 3, r.AssignmentDeadline - 100);
            var exam103 = r.Sessions.First(x => x.CourseCode == "CSC 103" && x.IsExam);
            r.RecordExam(exam103, 4, 8, exam103.Start);

            Assert.AreEqual(100, r.ComputeResult("CSC 101").total);
            var mid = r.ComputeResult("CSC 103");
            // attendance 35 (70% × 0.5), assignment 67, quiz 0, exam 50 → 3.5 + 13.4 + 0 + 25 = 41.9 → 42
            Assert.AreEqual(35, mid.attendanceComponent);
            Assert.AreEqual(42, mid.total);
            Assert.AreEqual("F", mid.grade);
            Assert.IsFalse(mid.passed);

            Assert.IsNull(r.FinalizeSemester(40 * Day), "Results not out until day 42");
            var result = r.FinalizeSemester(42 * Day + 60);
            Assert.IsNotNull(result);
            Assert.AreSame(result, r.FinalizeSemester(43 * Day), "Released once");
            Assert.AreEqual(5, result.courses.Count);
            Assert.AreEqual(Math.Round(4.0 * 3 / 15, 2), result.gpa, 1e-9, "Only CSC 101 earned grade points");
            Assert.AreEqual(result.gpa, result.cgpa, 1e-9);
            Assert.IsTrue(result.probation, "CGPA below 1.5 means probation");
            CollectionAssert.Contains(r.Resits(), "CSC 103");
            CollectionAssert.DoesNotContain(r.Resits(), "CSC 101");
            Assert.IsTrue(r.HasPassed("CSC 101"));
        }

        [Test]
        public void AttendingWithoutLearningDoesNotGuaranteeAGoodGrade()
        {
            var r = Registered();
            foreach (var s in r.Sessions.Where(x => x.CourseCode == "AMU 101" && !x.IsExam)) r.AttendLecture(s, s.Start);
            Assert.Less(r.ComputeResult("AMU 101").total, 50, "Perfect attendance alone is not a pass");
        }

        [Test]
        public void NextTaskGuidesTheStudent()
        {
            var r = NewRecord();
            StringAssert.Contains("Register", r.NextTask(Day).Text);
            r.TryRegister(CsSelection, Day, out _);
            var first = r.Sessions.First();
            var task = r.NextTask(Day * 2);
            Assert.AreEqual(first.Room, task.Room);
            StringAssert.Contains(first.CourseCode, task.Text);
            r.AttendLecture(first, first.Start);
            var after = r.NextTask(first.Start + 1);
            Assert.AreNotEqual(task.Minute, after.Minute, "An attended lecture is no longer the next task");
            StringAssert.Contains("results", r.NextTask(42 * Day).Text);
            r.FinalizeSemester(42 * Day);
            Assert.IsNull(r.NextTask(42 * Day));
        }

        [Test]
        public void SemesterOffsetShiftsTheCalendar()
        {
            var r = new AcademicRecord(new AcademicData { semesterStartDay = 100 }, Phase3Content.Catalog, "computer_science");
            Assert.AreEqual(TermPhase.Orientation, r.Phase(100 * Day));
            Assert.IsTrue(r.TryRegister(CsSelection, 101 * Day, out _));
            Assert.IsTrue(r.Sessions.All(s => s.Day >= 107));
        }
    }

    public class AcademicSaveTests
    {
        [Test]
        public void AcademicProgressSurvivesSaveAndLoad()
        {
            var session = GameSession.NewGame(new PlayerProfileData { displayName = "Ama", programId = "nursing" }, Phase1Content.CreateItemCatalog(), "s");
            var a = session.Academics;
            Assert.IsTrue(a.TryRegister(new[] { "AMU 101", "AMU 103", "NUR 101", "NUR 103", "AMU 107" }, session.Clock.TotalMinutes, out var e), string.Join(" ", e));
            var lecture = a.Sessions.First(s => !s.IsExam);
            a.AttendLecture(lecture, lecture.Start);
            a.Study("NUR 101", 0);

            var storage = new MemorySaveStorage();
            var service = new SaveService(storage);
            service.Save("slot", session.ToSave("0.3.0"), DateTime.UtcNow);
            var loaded = GameSession.FromSave(service.Load("slot").Game, Phase1Content.CreateItemCatalog());
            Assert.IsTrue(loaded.Academics.Registered);
            Assert.AreEqual(5, loaded.Academics.RegisteredCourses.Count);
            Assert.AreEqual(AttendanceMark.Present, loaded.Academics.RecordFor(lecture).mark);
            Assert.IsTrue(loaded.Academics.HasNote("NUR101-t1-k1"));
        }

        [Test]
        public void Version1SavesMigrateToVersion2()
        {
            var session = GameSession.NewGame(new PlayerProfileData { displayName = "Kojo", programId = "economics" }, Phase1Content.CreateItemCatalog(), "s");
            var save = session.ToSave("0.2.0");
            var tree = (Dictionary<string, object>)FieldJson.ParseTree(FieldJson.Serialize(save));
            tree.Remove("academic");
            tree["version"] = JsonNumber.Of(1);
            var game = new SaveMigrator().Migrate(FieldJson.Serialize(tree), 1);
            Assert.AreEqual(2, game.version);
            Assert.IsNotNull(game.academic);
            Assert.IsFalse(game.academic.registered);
            var restored = GameSession.FromSave(game, Phase1Content.CreateItemCatalog());
            StringAssert.Contains("Register", restored.Academics.NextTask(restored.Clock.TotalMinutes).Text);
        }

        [Test]
        public void UnknownCourseInSaveIsRejected()
        {
            var session = GameSession.NewGame(new PlayerProfileData { displayName = "Kojo", programId = "economics" }, Phase1Content.CreateItemCatalog(), "s");
            var save = session.ToSave("x");
            save.academic.registered = true;
            save.academic.registeredCourses.Add("FAKE 999");
            Assert.Throws<InvalidOperationException>(() => GameSession.FromSave(save, Phase1Content.CreateItemCatalog()));
        }
    }

    public class RestTests
    {
        const double Day = GameClock.MinutesPerDay;

        [Test]
        public void NightSleepWakesAtSixThirty()
        {
            Assert.AreEqual(Day + 390, RestRules.WakeTime(22 * 60));
            Assert.AreEqual(390, RestRules.WakeTime(2 * 60));
            Assert.AreEqual(14 * 60 + 120, RestRules.WakeTime(14 * 60), "Daytime nap of two hours");
            Assert.IsTrue(RestRules.IsNightSleep(23 * 60));
            Assert.IsFalse(RestRules.IsNightSleep(12 * 60));
        }
    }
}
