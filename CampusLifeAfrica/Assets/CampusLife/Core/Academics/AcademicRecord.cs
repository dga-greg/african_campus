using System;
using System.Collections.Generic;
using CampusLife.Core.Time;

namespace CampusLife.Core.Academics
{
    public enum AttendanceCheck { NotRegistered, NoSession, TooEarly, OnTime, Late, TooLate, AlreadyRecorded }

    public sealed class AcademicTask
    {
        public string Text;
        /// <summary>Room ("LT1", "LT2", "library") the task happens in, or null.</summary>
        public string Room;
        public double Minute;
    }

    /// <summary>
    /// A student's academic life for the current semester and their history:
    /// registration, attendance, notes, study, assessments, results and CGPA.
    /// All rules are deterministic and independent of the engine.
    /// </summary>
    public sealed class AcademicRecord
    {
        public const int MinCredits = 15;
        public const int MaxCredits = 18;
        public const int OnTimeGrace = 10;    // minutes after start that still count as on time
        public const int LateLimit = 30;      // minutes after start you can still join
        public const int EarliestJoin = 60;   // minutes before start you can sit down and wait
        public const int MaxPreparation = 8;

        readonly AcademicData _data;
        List<Session> _sessions;

        public CourseCatalog Catalog { get; }
        public GradingScale Scale { get; }
        public AssessmentWeights Weights { get; }
        public string ProgramId { get; }

        public AcademicData Data => _data;
        public bool Registered => _data.registered;
        public IReadOnlyList<string> RegisteredCourses => _data.registeredCourses;
        public IReadOnlyList<SemesterResult> Results => _data.results;
        public int SemesterStartDay => _data.semesterStartDay;

        public AcademicRecord(AcademicData data, CourseCatalog catalog, string programId, GradingScale scale = null, AssessmentWeights weights = null)
        {
            _data = data ?? new AcademicData();
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            ProgramId = programId;
            Scale = scale ?? GradingScale.Default;
            Weights = weights ?? AssessmentWeights.Default;
            if (_data.registeredCourses == null) _data.registeredCourses = new List<string>();
            if (_data.progress == null) _data.progress = new List<CourseProgress>();
            if (_data.notes == null) _data.notes = new List<string>();
            if (_data.results == null) _data.results = new List<SemesterResult>();
        }

        /// <summary>Day within the current semester (0 = orientation Monday).</summary>
        public int TermDay(double now) => (int)(now / GameClock.MinutesPerDay) - _data.semesterStartDay;

        public TermPhase Phase(double now) => AcademicCalendar.Phase(TermDay(now));

        // ------------------------------------------------------- registration

        public List<CourseDefinition> Offered() => Catalog.Offered(ProgramId, _data.year * 100, _data.semester);

        public bool RegistrationOpen(double now) => !_data.registered && AcademicCalendar.RegistrationOpen(TermDay(now));

        public List<string> ValidateRegistration(IEnumerable<string> codes)
        {
            var errors = new List<string>();
            var selected = new HashSet<string>(StringComparer.Ordinal);
            var offered = new Dictionary<string, CourseDefinition>(StringComparer.Ordinal);
            foreach (var c in Offered()) offered[c.Code] = c;
            int credits = 0;
            foreach (var code in codes)
            {
                if (!selected.Add(code)) { errors.Add($"{code} is selected twice."); continue; }
                if (!offered.TryGetValue(code, out var course)) { errors.Add($"{code} is not offered to your programme this semester."); continue; }
                credits += course.Credits;
                foreach (var pre in course.Prerequisites)
                    if (!HasPassed(pre)) errors.Add($"{code} requires {pre}, which you have not passed.");
            }
            foreach (var c in offered.Values)
                if (c.Kind == CourseKind.Core && !selected.Contains(c.Code)) errors.Add($"{c.Code} is a required course.");
            if (credits < MinCredits) errors.Add($"Register at least {MinCredits} credits (selected {credits}).");
            if (credits > MaxCredits) errors.Add($"Register at most {MaxCredits} credits (selected {credits}).");
            return errors;
        }

        public bool TryRegister(IEnumerable<string> codes, double now, out List<string> errors)
        {
            var list = new List<string>(codes);
            errors = new List<string>();
            if (_data.registered) errors.Add("You have already registered this semester.");
            else if (!AcademicCalendar.RegistrationOpen(TermDay(now))) errors.Add("Course registration is closed.");
            else errors.AddRange(ValidateRegistration(list));
            if (errors.Count > 0) return false;
            list.Sort(StringComparer.Ordinal);
            _data.registeredCourses = list;
            _data.progress = new List<CourseProgress>();
            foreach (var code in list) _data.progress.Add(new CourseProgress { courseCode = code });
            _data.registered = true;
            _sessions = null;
            return true;
        }

        public int RegisteredCredits()
        {
            int sum = 0;
            foreach (var code in _data.registeredCourses) sum += Catalog.Get(code).Credits;
            return sum;
        }

        public bool HasPassed(string code)
        {
            foreach (var r in _data.results)
                foreach (var c in r.courses)
                    if (c.courseCode == code && c.passed) return true;
            return false;
        }

        // ------------------------------------------------------------ sessions

        /// <summary>All sessions of the semester in absolute game time.</summary>
        public IReadOnlyList<Session> Sessions
        {
            get
            {
                if (_sessions == null)
                {
                    _sessions = TimetableBuilder.Sessions(_data.registeredCourses);
                    foreach (var s in _sessions) s.Day += _data.semesterStartDay;
                }
                return _sessions;
            }
        }

        public List<Meeting> WeeklyTimetable() => TimetableBuilder.Weekly(_data.registeredCourses);

        /// <summary>The session held in a room around a moment (from an hour before until it ends).</summary>
        public Session SessionInRoom(string room, double now)
        {
            foreach (var s in Sessions)
                if (s.Room == room && now >= s.Start - EarliestJoin && now < s.End) return s;
            return null;
        }

        public CourseProgress Progress(string code)
        {
            foreach (var p in _data.progress) if (p.courseCode == code) return p;
            return null;
        }

        public SessionRecord RecordFor(Session s)
        {
            var p = Progress(s.CourseCode);
            if (p == null) return null;
            foreach (var r in p.sessions) if (r.key == s.Key) return r;
            return null;
        }

        public AttendanceCheck CheckAttendance(Session s, double now)
        {
            if (s == null) return AttendanceCheck.NoSession;
            if (Progress(s.CourseCode) == null) return AttendanceCheck.NotRegistered;
            if (s.IsExam ? Progress(s.CourseCode).examScore >= 0 : RecordFor(s) != null) return AttendanceCheck.AlreadyRecorded;
            if (now < s.Start - EarliestJoin) return AttendanceCheck.TooEarly;
            if (now <= s.Start + OnTimeGrace) return AttendanceCheck.OnTime;
            if (now <= s.Start + LateLimit) return AttendanceCheck.Late;
            return AttendanceCheck.TooLate;
        }

        /// <summary>Records attendance for a lecture. Returns false if it cannot be attended now or was already recorded.</summary>
        public bool AttendLecture(Session s, double now)
        {
            if (s == null || s.IsExam) return false;
            var check = CheckAttendance(s, now);
            if (check != AttendanceCheck.OnTime && check != AttendanceCheck.Late) return false;
            Progress(s.CourseCode).sessions.Add(new SessionRecord { key = s.Key, mark = check == AttendanceCheck.OnTime ? AttendanceMark.Present : AttendanceMark.Late });
            return true;
        }

        /// <summary>Lecture challenge answer; only counts once and only for an attended session.</summary>
        public bool AnswerChallenge(Session s, bool correct)
        {
            var r = RecordFor(s);
            if (r == null || r.challenge >= 0) return false;
            r.challenge = correct ? 1 : 0;
            return true;
        }

        // --------------------------------------------------------------- notes

        public bool TakeNote(string keyPointId)
        {
            if (!Catalog.TryGetKeyPoint(keyPointId, out _) || _data.notes.Contains(keyPointId)) return false;
            _data.notes.Add(keyPointId);
            return true;
        }

        public bool HasNote(string keyPointId) => _data.notes.Contains(keyPointId);

        /// <summary>
        /// One hour of library study on a topic: adds preparation (capped) and
        /// copies the topic's key points into the student's notes, so a missed
        /// lecture can be caught up on.
        /// </summary>
        public bool Study(string courseCode, int topicIndex)
        {
            var p = Progress(courseCode);
            if (p == null) return false;
            var course = Catalog.Get(courseCode);
            if (topicIndex < 0 || topicIndex >= course.Topics.Count) return false;
            foreach (var k in course.Topics[topicIndex].KeyPoints) TakeNote(k.Id);
            if (p.preparation < MaxPreparation) p.preparation++;
            return true;
        }

        public int HintsAvailable(string courseCode)
        {
            var p = Progress(courseCode);
            return p == null ? 0 : Math.Max(0, p.preparation / 2 - p.hintsUsed);
        }

        public bool UseHint(string courseCode)
        {
            if (HintsAvailable(courseCode) <= 0) return false;
            Progress(courseCode).hintsUsed++;
            return true;
        }

        // --------------------------------------------------------- assessments

        /// <summary>The assignment is set in the first teaching week and can be submitted once.</summary>
        public bool AssignmentOpen(string courseCode, double now)
        {
            var p = Progress(courseCode);
            if (p == null || p.assignmentScore >= 0) return false;
            int day = TermDay(now);
            return day >= AcademicCalendar.TeachingStartDay && day < AcademicCalendar.ExamStartDay;
        }

        public double AssignmentDeadline => AcademicCalendar.AssignmentDeadline + _data.semesterStartDay * (double)GameClock.MinutesPerDay;

        public static List<KeyPoint> AssignmentQuestions(CourseDefinition c)
            => new List<KeyPoint> { c.Topics[0].KeyPoints[1], c.Topics[1].KeyPoints[1], c.Topics[2].KeyPoints[1] };

        public static List<KeyPoint> QuizQuestions(CourseDefinition c)
            => new List<KeyPoint> { c.Topics[0].KeyPoints[0], c.Topics[0].KeyPoints[1], c.Topics[1].KeyPoints[0], c.Topics[1].KeyPoints[1] };

        public static List<KeyPoint> ExamQuestions(CourseDefinition c) => new List<KeyPoint>(c.AllKeyPoints());

        /// <summary>Score out of 100 with 10 points off per late day (up to five days), then closed.</summary>
        public int SubmitAssignment(string courseCode, int correct, int total, double now)
        {
            if (!AssignmentOpen(courseCode, now)) return -1;
            int raw = total <= 0 ? 0 : (int)Math.Round(100.0 * correct / total);
            double late = now - AssignmentDeadline;
            int lateDays = late <= 0 ? 0 : (int)Math.Ceiling(late / GameClock.MinutesPerDay);
            if (lateDays > AcademicCalendar.MaxLateDays) return -1;
            int score = Math.Max(0, raw - lateDays * AcademicCalendar.LateDayPenalty);
            var p = Progress(courseCode);
            p.assignmentScore = score;
            p.assignmentSubmittedMinute = now;
            return score;
        }

        public bool RecordQuiz(Session s, int correct, int total)
        {
            if (s == null || !s.HasQuiz || RecordFor(s) == null) return false;
            var p = Progress(s.CourseCode);
            if (p.quizScore >= 0) return false;
            p.quizScore = total <= 0 ? 0 : (int)Math.Round(100.0 * correct / total);
            return true;
        }

        public bool CanSitExam(Session exam, double now)
        {
            var check = CheckAttendance(exam, now);
            return exam != null && exam.IsExam && (check == AttendanceCheck.OnTime || check == AttendanceCheck.Late);
        }

        public bool RecordExam(Session exam, int correct, int total, double now)
        {
            if (!CanSitExam(exam, now)) return false;
            Progress(exam.CourseCode).examScore = total <= 0 ? 0 : (int)Math.Round(100.0 * correct / total);
            return true;
        }

        // ------------------------------------------------------------- results

        /// <summary>Attendance and participation, 0..100: 70% attendance across the four lectures, 30% lecture challenges answered correctly.</summary>
        public int AttendanceScore(string courseCode)
        {
            var p = Progress(courseCode);
            if (p == null) return 0;
            double attend = 0;
            int asked = 0, right = 0;
            foreach (var r in p.sessions)
            {
                if (r.key.EndsWith(":exam", StringComparison.Ordinal)) continue;
                attend += r.mark == AttendanceMark.Present ? 1 : r.mark == AttendanceMark.Late ? 0.5 : 0;
                if (r.challenge >= 0) { asked++; right += r.challenge; }
            }
            double attendance = attend / AcademicCalendar.TeachingWeeks;
            double participation = right / (double)AcademicCalendar.TeachingWeeks;
            return (int)Math.Round(100 * (0.7 * attendance + 0.3 * participation));
        }

        public CourseResult ComputeResult(string courseCode)
        {
            var p = Progress(courseCode) ?? throw new KeyNotFoundException(courseCode);
            var course = Catalog.Get(courseCode);
            int attendance = AttendanceScore(courseCode);
            int assignment = Math.Max(0, p.assignmentScore);
            int quiz = Math.Max(0, p.quizScore);
            int exam = Math.Max(0, p.examScore);
            double total = (attendance * Weights.AttendanceAndParticipation + assignment * Weights.Assignment
                            + quiz * Weights.Quiz + exam * Weights.Exam) / (double)Weights.Total;
            int rounded = (int)Math.Round(total, MidpointRounding.AwayFromZero);
            var grade = Scale.Grade(rounded);
            return new CourseResult
            {
                courseCode = courseCode, credits = course.Credits,
                attendanceComponent = attendance, assignmentComponent = assignment, quizComponent = quiz, examComponent = exam,
                total = rounded, grade = grade.Letter, gradePoint = grade.Points, passed = Scale.Passed(rounded),
            };
        }

        public bool ResultsReady(double now) => TermDay(now) >= AcademicCalendar.ResultsDay;

        public SemesterResult CurrentSemesterResult()
        {
            foreach (var r in _data.results)
                if (r.year == _data.year && r.semester == _data.semester) return r;
            return null;
        }

        /// <summary>Releases this semester's results once (idempotent).</summary>
        public SemesterResult FinalizeSemester(double now)
        {
            var existing = CurrentSemesterResult();
            if (existing != null) return existing;
            if (!ResultsReady(now)) return null;
            var result = new SemesterResult { year = _data.year, semester = _data.semester, releasedMinute = now };
            var points = new List<(int, double)>();
            foreach (var code in _data.registeredCourses)
            {
                var r = ComputeResult(code);
                result.courses.Add(r);
                points.Add((r.credits, r.gradePoint));
            }
            result.gpa = GradingScale.Gpa(points);
            _data.results.Add(result);
            result.cgpa = Cgpa();
            result.probation = result.courses.Count > 0 && result.cgpa < Scale.ProbationCgpa;
            return result;
        }

        /// <summary>Cumulative GPA over every released course result.</summary>
        public double Cgpa()
        {
            var all = new List<(int, double)>();
            foreach (var r in _data.results)
                foreach (var c in r.courses) all.Add((c.credits, c.gradePoint));
            return GradingScale.Gpa(all);
        }

        /// <summary>Courses failed and not yet passed, to be retaken.</summary>
        public List<string> Resits()
        {
            var list = new List<string>();
            foreach (var r in _data.results)
                foreach (var c in r.courses)
                    if (!c.passed && !HasPassed(c.courseCode) && !list.Contains(c.courseCode)) list.Add(c.courseCode);
            return list;
        }

        // ---------------------------------------------------------- next task

        /// <summary>What the student should do next, for the HUD objective.</summary>
        public AcademicTask NextTask(double now)
        {
            int day = TermDay(now);
            if (!_data.registered)
            {
                if (AcademicCalendar.RegistrationOpen(day))
                    return new AcademicTask { Text = "Register your courses on the student portal (Tab → Academics)" };
                return null;
            }
            if (CurrentSemesterResult() != null) return null;
            if (ResultsReady(now)) return new AcademicTask { Text = "Your semester results are out — open the student portal (Tab → Academics)" };
            foreach (var s in Sessions)
            {
                if (s.End <= now) continue;
                var check = CheckAttendance(s, now);
                if (check == AttendanceCheck.AlreadyRecorded || check == AttendanceCheck.TooLate) continue;
                var c = Catalog.Get(s.CourseCode);
                var clock = new GameClock(s.Start);
                string what = s.IsExam ? "Exam" : s.HasQuiz ? "Lecture + quiz" : "Lecture";
                return new AcademicTask
                {
                    Text = $"{what}: {c.Code} {c.Title} — {GameClock.WeekdayShort(clock.Weekday)} {clock.Hour:00}:{clock.Minute:00}, {s.Room}",
                    Room = s.Room,
                    Minute = s.Start,
                };
            }
            return null;
        }
    }
}
