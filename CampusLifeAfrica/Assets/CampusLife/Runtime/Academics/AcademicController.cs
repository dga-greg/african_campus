using System;
using System.Collections.Generic;
using CampusLife.Core.Academics;
using CampusLife.Core.Npcs;
using CampusLife.Core.Player;
using CampusLife.Core.Session;
using CampusLife.Core.Time;
using CampusLife.Runtime.Characters;
using CampusLife.Runtime.Player;
using CampusLife.Runtime.Rendering;
using CampusLife.Runtime.World;
using UnityEngine;

namespace CampusLife.Runtime.Academics
{
    public enum ActivityType { Lecture, Exam, Assignment, StudyPicker, AssignmentPicker }

    public enum StepKind { Intro, KeyPoint, Question, Summary }

    public sealed class ActivityStep
    {
        public StepKind Kind;
        public string Title;
        public string Body;
        public KeyPoint KeyPoint;
        public PresentedQuestion Question;
        public bool IsQuiz;
        public int Chosen = -1;
        public bool[] Hidden = new bool[4];
        /// <summary>Whether to show right/wrong immediately (lectures and assignments) or not (exams).</summary>
        public bool RevealAnswer;
    }

    /// <summary>An interactive lecture, exam or assignment, step by step.</summary>
    public sealed class AcademicActivity
    {
        public ActivityType Type;
        public CourseDefinition Course;
        public Session Session;
        public readonly List<ActivityStep> Steps = new List<ActivityStep>();
        public int Index;
        public int NotesTaken;
        public string Summary;

        public ActivityStep Current => Index < Steps.Count ? Steps[Index] : null;
        public bool IsPicker => Type == ActivityType.StudyPicker || Type == ActivityType.AssignmentPicker;

        public int Correct(bool quiz)
        {
            int n = 0;
            foreach (var s in Steps)
                if (s.Kind == StepKind.Question && s.IsQuiz == quiz && s.Chosen == s.Question.Answer) n++;
            return n;
        }

        public int Count(bool quiz)
        {
            int n = 0;
            foreach (var s in Steps) if (s.Kind == StepKind.Question && s.IsQuiz == quiz) n++;
            return n;
        }
    }

    public struct SeatOption
    {
        public string Label;
        public Action Run;
        public bool Primary;
    }

    /// <summary>
    /// Connects the academic rules (<see cref="AcademicRecord"/>) to the world:
    /// what a seated student can do in a lecture hall or the library, the
    /// interactive lecture/exam/assignment activities, lecture-hall boards and
    /// lecturers, time skipping and releasing semester results.
    /// </summary>
    public sealed class AcademicController : MonoBehaviour
    {
        public const string PauseReason = "academic";

        public Func<GameSession> Session;
        public PlayerController Player;
        public MaterialLibrary Materials;
        public Action<string> Toast;
        public Action Autosave;
        /// <summary>Raised when an activity opens or closes (the bootstrap switches overlays).</summary>
        public event Action<bool> ActivityChanged;
        /// <summary>Raised once when a semester's results are released.</summary>
        public event Action<SemesterResult> ResultsReleased;

        public AcademicActivity Activity { get; private set; }
        public string SeatInfo { get; private set; }
        readonly List<SeatOption> _options = new List<SeatOption>();
        readonly Dictionary<string, GameObject> _lecturers = new Dictionary<string, GameObject>();
        float _visualTimer;
        int _openedFrame;

        public IReadOnlyList<SeatOption> SeatOptions => _options;
        public bool HasSeatOptions => Player != null && Player.IsSeated && (_options.Count > 0 || SeatInfo != null) && Activity == null;

        AcademicRecord Record => Session?.Invoke()?.Academics;
        double Now => Session().Clock.TotalMinutes;

        void Update()
        {
            var session = Session?.Invoke();
            if (session == null) return;
            RefreshSeatOptions();
            var input = Player.Input;
            if (Activity == null && HasSeatOptions && Player.ControlEnabled)
            {
                int pick = input.ChoicePressed;
                if (pick >= 0 && pick < _options.Count) _options[pick].Run();
            }
            else if (Activity != null && !Activity.IsPicker && Time.frameCount > _openedFrame)
            {
                // Keyboard: 1–4 answer, Space/Enter continues.
                var step = Activity.Current;
                if (step != null && step.Kind == StepKind.Question && step.Chosen < 0 && input.ChoicePressed >= 0 && input.ChoicePressed < 4) Answer(input.ChoicePressed);
                else if (input.ContinuePressed)
                {
                    if (step != null && step.Kind == StepKind.Summary) Finish();
                    else Next();
                }
            }
            _visualTimer -= Time.deltaTime;
            if (_visualTimer <= 0f)
            {
                _visualTimer = 0.5f;
                UpdateRoomVisuals();
                CheckResults();
            }
        }

        // -------------------------------------------------------- seat options

        void RefreshSeatOptions()
        {
            _options.Clear();
            SeatInfo = null;
            if (Player == null || !Player.IsSeated || Activity != null) return;
            var seat = Player.CurrentSeat;
            if (seat == null || string.IsNullOrEmpty(seat.RoomId)) return;
            var record = Record;
            double now = Now;
            if (seat.RoomId == "library") { LibraryOptions(record, now); return; }

            if (!record.Registered) { SeatInfo = "You haven't registered any courses yet (Tab → Academics)."; return; }
            var s = record.SessionInRoom(seat.RoomId, now);
            if (s == null)
            {
                var next = record.NextTask(now);
                SeatInfo = next != null ? $"No lecture of yours here now. Next — {next.Text}" : "No lecture of yours here now.";
                return;
            }
            var course = record.Catalog.Get(s.CourseCode);
            var check = record.CheckAttendance(s, now);
            string what = s.IsExam ? "Exam" : "Lecture";
            switch (check)
            {
                case AttendanceCheck.NotRegistered:
                    SeatInfo = $"{course.FullName} is in session here, but you're not registered for it.";
                    break;
                case AttendanceCheck.AlreadyRecorded:
                    SeatInfo = s.IsExam ? $"You've already sat the {course.Code} exam." : $"You've attended this week's {course.Code} lecture.";
                    break;
                case AttendanceCheck.TooLate:
                    SeatInfo = $"Too late to join this {course.Code} {what.ToLowerInvariant()} — it started over {AcademicRecord.LateLimit} minutes ago.";
                    break;
                case AttendanceCheck.OnTime:
                case AttendanceCheck.Late:
                    SeatInfo = $"{what}: {course.FullName} · {Format(s.Start)}–{Format(s.End)} · {course.Lecturer}";
                    if (now < s.Start)
                        _options.Add(new SeatOption { Label = $"Wait for the {what.ToLowerInvariant()} to start ({Format(s.Start)})", Run = () => WaitAndStart(s), Primary = true });
                    else
                        _options.Add(new SeatOption { Label = s.IsExam ? $"Sit the {course.Code} exam" : $"Attend the lecture{(check == AttendanceCheck.Late ? " (late)" : "")}", Run = () => Begin(s), Primary = true });
                    break;
            }
        }

        void LibraryOptions(AcademicRecord record, double now)
        {
            if (!record.Registered) { SeatInfo = "Register your courses first (Tab → Academics) to study for them."; return; }
            SeatInfo = "A quiet spot in Nkabom Library.";
            _options.Add(new SeatOption { Label = "Study a topic (1 hour)", Run = () => OpenPicker(ActivityType.StudyPicker), Primary = true });
            bool anyOpen = false;
            foreach (var code in record.RegisteredCourses) anyOpen |= record.AssignmentOpen(code, now);
            if (anyOpen) _options.Add(new SeatOption { Label = "Work on an assignment (2 hours)", Run = () => OpenPicker(ActivityType.AssignmentPicker) });
        }

        static string Format(double minute)
        {
            var c = new GameClock(minute);
            return $"{GameClock.WeekdayShort(c.Weekday)} {c.Hour:00}:{c.Minute:00}";
        }

        // ---------------------------------------------------------- activities

        void WaitAndStart(Session s)
        {
            double wait = s.Start - Now;
            if (wait > 0) Session().Clock.AdvanceMinutes(wait);
            Begin(s);
        }

        void Begin(Session s)
        {
            var record = Record;
            double now = Now;
            var course = record.Catalog.Get(s.CourseCode);
            var a = new AcademicActivity { Type = s.IsExam ? ActivityType.Exam : ActivityType.Lecture, Course = course, Session = s };
            if (s.IsExam)
            {
                if (!record.CanSitExam(s, now)) { Toast?.Invoke("You can't sit this exam now."); return; }
                a.Steps.Add(new ActivityStep
                {
                    Kind = StepKind.Intro, Title = $"Examination · {course.FullName}",
                    Body = $"Eight questions covering all four topics. Answers are revealed in your results. " +
                           $"You have {record.HintsAvailable(course.Code)} 50/50 hint(s) from studying in the library.",
                });
                foreach (var k in AcademicRecord.ExamQuestions(course))
                    a.Steps.Add(new ActivityStep { Kind = StepKind.Question, Question = PresentedQuestion.Shuffle(k, "exam"), Title = "Exam question" });
            }
            else
            {
                if (!record.AttendLecture(s, now)) { Toast?.Invoke("You can't join this lecture now."); return; }
                var topic = course.Topics[s.TopicIndex];
                var mark = record.RecordFor(s).mark;
                a.Steps.Add(new ActivityStep
                {
                    Kind = StepKind.Intro, Title = $"{course.FullName} · Week {s.Week}",
                    Body = $"{course.Lecturer} begins: \"Today's topic is {topic.Title}.\"" + (mark == AttendanceMark.Late ? "\n(You slipped in late — attendance recorded as late.)" : ""),
                });
                foreach (var k in topic.KeyPoints)
                    a.Steps.Add(new ActivityStep { Kind = StepKind.KeyPoint, KeyPoint = k, Title = topic.Title, Body = k.Text });
                a.Steps.Add(new ActivityStep
                {
                    Kind = StepKind.Question, Question = PresentedQuestion.Shuffle(topic.KeyPoints[0], "lecture"),
                    Title = $"{course.Lecturer} asks the class", RevealAnswer = true,
                });
                if (s.HasQuiz)
                {
                    a.Steps.Add(new ActivityStep { Kind = StepKind.Intro, Title = "Mid-semester quiz", Body = "Four questions on topics 1 and 2. It counts for 20% of your course mark." });
                    foreach (var k in AcademicRecord.QuizQuestions(course))
                        a.Steps.Add(new ActivityStep { Kind = StepKind.Question, IsQuiz = true, Question = PresentedQuestion.Shuffle(k, "quiz"), Title = "Quiz question" });
                }
            }
            a.Steps.Add(new ActivityStep { Kind = StepKind.Summary, Title = "Summary" });
            Open(a);
        }

        void OpenPicker(ActivityType type)
        {
            Open(new AcademicActivity { Type = type });
        }

        public void StartStudy(string courseCode, int topicIndex)
        {
            var record = Record;
            if (!record.Study(courseCode, topicIndex)) return;
            var course = record.Catalog.Get(courseCode);
            Session().Clock.AdvanceMinutes(60);
            Close();
            Toast?.Invoke($"Studied {course.Code}: {course.Topics[topicIndex].Title}. Notes added; preparation {record.Progress(courseCode).preparation}/{AcademicRecord.MaxPreparation} ({record.HintsAvailable(courseCode)} exam hint(s)).");
            Autosave?.Invoke();
        }

        public void StartAssignment(string courseCode)
        {
            var record = Record;
            if (!record.AssignmentOpen(courseCode, Now)) return;
            var course = record.Catalog.Get(courseCode);
            var a = new AcademicActivity { Type = ActivityType.Assignment, Course = course };
            double late = Now - record.AssignmentDeadline;
            a.Steps.Add(new ActivityStep
            {
                Kind = StepKind.Intro, Title = $"Assignment · {course.FullName}",
                Body = "Three short questions on topics 1–3. It counts for 20% of your course mark." +
                       (late > 0 ? $"\nThis is late: {AcademicCalendar.LateDayPenalty} marks off per day." : ""),
            });
            foreach (var k in AcademicRecord.AssignmentQuestions(course))
                a.Steps.Add(new ActivityStep { Kind = StepKind.Question, Question = PresentedQuestion.Shuffle(k, "assignment"), Title = "Assignment question", RevealAnswer = true });
            a.Steps.Add(new ActivityStep { Kind = StepKind.Summary, Title = "Summary" });
            Activity = a;
        }

        void Open(AcademicActivity a)
        {
            Activity = a;
            _openedFrame = Time.frameCount;
            Session().Clock.Pause(PauseReason);
            Player.ControlEnabled = false;
            ActivityChanged?.Invoke(true);
        }

        public void TakeNote()
        {
            var step = Activity?.Current;
            if (step?.KeyPoint == null) return;
            if (Record.TakeNote(step.KeyPoint.Id)) Activity.NotesTaken++;
        }

        public bool HasNote(KeyPoint k) => k != null && Record.HasNote(k.Id);

        public void Answer(int option)
        {
            var step = Activity?.Current;
            if (step == null || step.Kind != StepKind.Question || step.Chosen >= 0 || step.Hidden[option]) return;
            step.Chosen = option;
            if (Activity.Type == ActivityType.Lecture && !step.IsQuiz)
                Record.AnswerChallenge(Activity.Session, option == step.Question.Answer);
        }

        public int HintsAvailable => Activity?.Course != null ? Record.HintsAvailable(Activity.Course.Code) : 0;

        public void UseHint()
        {
            var step = Activity?.Current;
            if (Activity == null || Activity.Type != ActivityType.Exam || step?.Question == null || step.Chosen >= 0) return;
            foreach (bool h in step.Hidden) if (h) return; // one hint per question
            if (!Record.UseHint(Activity.Course.Code)) return;
            foreach (int i in step.Question.HintRemovals()) step.Hidden[i] = true;
        }

        public void Next()
        {
            var a = Activity;
            if (a == null) return;
            var step = a.Current;
            if (step != null && step.Kind == StepKind.Question && step.Chosen < 0) return;
            a.Index++;
            if (a.Current != null && a.Current.Kind == StepKind.Summary) Commit(a);
        }

        void Commit(AcademicActivity a)
        {
            var record = Record;
            double now = Now;
            switch (a.Type)
            {
                case ActivityType.Lecture:
                {
                    int quiz = a.Count(true);
                    if (quiz > 0) record.RecordQuiz(a.Session, a.Correct(true), quiz);
                    var p = record.Progress(a.Course.Code);
                    a.Summary = $"Attendance recorded ({record.RecordFor(a.Session).mark}). Notes taken: {a.NotesTaken}." +
                                (quiz > 0 ? $"\nQuiz: {a.Correct(true)}/{quiz} → {p.quizScore}%." : "") +
                                "\nThe lecture ends and you pack up your things.";
                    break;
                }
                case ActivityType.Exam:
                {
                    int total = a.Count(false);
                    record.RecordExam(a.Session, a.Correct(false), total, now);
                    a.Summary = "Pens down! Your script has been collected. Marks are released with the semester results.";
                    break;
                }
                case ActivityType.Assignment:
                {
                    int score = record.SubmitAssignment(a.Course.Code, a.Correct(false), a.Count(false), now);
                    a.Summary = score >= 0 ? $"Submitted. Score: {score}%." : "The submission window has closed.";
                    break;
                }
            }
        }

        /// <summary>Closes the activity and moves time on to when it ends.</summary>
        public void Finish()
        {
            var a = Activity;
            if (a == null) return;
            var clock = Session().Clock;
            clock.Resume(PauseReason);
            if (a.Current == null || a.Current.Kind == StepKind.Summary)
            {
                if (a.Session != null && a.Session.End > clock.TotalMinutes) clock.AdvanceMinutes(a.Session.End - clock.TotalMinutes);
                else if (a.Type == ActivityType.Assignment) clock.AdvanceMinutes(120);
            }
            Close();
            if (!a.IsPicker) Autosave?.Invoke();
        }

        /// <summary>Leaves a picker, or abandons an activity that hasn't been committed (no time passes).</summary>
        public void Cancel()
        {
            if (Activity == null) return;
            if (!Activity.IsPicker && Activity.Current != null && Activity.Current.Kind == StepKind.Summary) { Finish(); return; }
            if (Activity.Type == ActivityType.Lecture || Activity.Type == ActivityType.Exam)
            {
                // Walking out still counts as attendance, but unanswered questions score nothing.
                Activity.Index = Activity.Steps.Count - 1;
                Commit(Activity);
                Finish();
                return;
            }
            Session().Clock.Resume(PauseReason);
            Close();
        }

        void Close()
        {
            Session()?.Clock.Resume(PauseReason);
            Activity = null;
            Player.ControlEnabled = true;
            ActivityChanged?.Invoke(false);
        }

        // -------------------------------------------------------------- world

        void UpdateRoomVisuals()
        {
            var record = Record;
            double now = Now;
            foreach (var room in LectureInteriorBuilder.BuiltRooms.Values)
            {
                if (room.Board == null) continue;
                var s = record.Registered ? record.SessionInRoom(room.Id, now) : null;
                bool live = s != null && now >= s.Start - 15;
                if (s == null) room.Board.text = $"{room.Id}\nNo lecture scheduled now";
                else
                {
                    var c = record.Catalog.Get(s.CourseCode);
                    room.Board.text = s.IsExam
                        ? $"EXAMINATION\n{c.FullName}\nSilence please"
                        : $"{c.FullName}\nWeek {s.Week}: {c.Topics[s.TopicIndex].Title}\n{Format(s.Start)}–{Format(s.End).Substring(4)}";
                }
                ShowLecturer(room, live ? record.Catalog.Get(s.CourseCode).Lecturer : null);
            }
            if (LectureInteriorBuilder.TimetableBoard != null && LectureInteriorBuilder.TimetableBoard.MessageProvider == null)
                LectureInteriorBuilder.TimetableBoard.MessageProvider = TimetableText;
        }

        void ShowLecturer(LectureInteriorBuilder.Room room, string lecturer)
        {
            _lecturers.TryGetValue(room.Id, out var current);
            if (lecturer == null)
            {
                if (current != null) current.SetActive(false);
                return;
            }
            if (current != null && current.name == lecturer)
            {
                current.SetActive(true);
                return;
            }
            if (current != null) Destroy(current);
            var go = new GameObject(lecturer);
            go.transform.SetParent(room.LecturerPost, false);
            var rng = DeterministicRandom.From(lecturer);
            var look = new AppearanceData
            {
                skinTone = rng.Range(3, CharacterOptions.SkinTones.Length), hairStyle = rng.Range(0, CharacterOptions.HairStyles.Length),
                hairColor = rng.Range(0, 2), topStyle = rng.Range(0, CharacterOptions.TopStyles.Length), topColor = rng.Range(0, CharacterOptions.ClothingColors.Length),
                bottomColor = 1, shoeColor = 1, accessory = rng.Chance(0.5) ? 1 : 0, height = 1f,
            };
            var visual = CharacterRigBuilder.Build(go.transform, look, Materials);
            var anim = go.AddComponent<CharacterAnimator>();
            anim.Visual = visual;
            anim.SetPose(CharacterPose.Talk);
            _lecturers[room.Id] = go;
        }

        public string TimetableText()
        {
            var record = Record;
            if (record == null || !record.Registered) return "Your timetable appears here once you register your courses on the student portal (Tab → Academics).";
            var lines = new List<string> { "Your weekly lectures:" };
            foreach (var m in record.WeeklyTimetable())
                lines.Add($"{m.Day} {m.StartMinute / 60:00}:{m.StartMinute % 60:00} · {m.CourseCode} · {m.Room}");
            return string.Join("\n", lines);
        }

        void CheckResults()
        {
            var record = Record;
            if (record == null || record.CurrentSemesterResult() != null || !record.ResultsReady(Now)) return;
            var result = record.FinalizeSemester(Now);
            if (result != null)
            {
                Autosave?.Invoke();
                ResultsReleased?.Invoke(result);
            }
        }
    }
}
