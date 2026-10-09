using System.Collections.Generic;
using CampusLife.Core.Academics;
using CampusLife.Core.Session;
using CampusLife.Core.Time;
using CampusLife.Runtime.Academics;
using UnityEngine;

namespace CampusLife.Runtime.UI
{
    public sealed partial class GameUI
    {
        Vector2 _academicScroll;
        readonly HashSet<string> _selectedCourses = new HashSet<string>();
        List<string> _registrationErrors;
        bool _selectionInitialised;

        static string PhaseLabel(TermPhase phase, GameSession s)
        {
            switch (phase)
            {
                case TermPhase.Orientation: return "Orientation week";
                case TermPhase.Teaching: return $"Teaching week {AcademicCalendar.TeachingWeek(s.Academics.TermDay(s.Clock.TotalMinutes))}";
                case TermPhase.Exams: return "Examination week";
                case TermPhase.AwaitingResults: return "Awaiting results";
                default: return "Vacation";
            }
        }

        static string When(double minute)
        {
            var c = new GameClock(minute);
            return $"{GameClock.WeekdayShort(c.Weekday)} (day {c.Day + 1}) {c.Hour:00}:{c.Minute:00}";
        }

        // ----------------------------------------------------- seated options

        void DrawSeatPanel()
        {
            var a = Game.Academic;
            float w = 440f;
            var options = a.SeatOptions;
            float h = 70f + options.Count * 52f;
            var info = new GUIContent(UiTheme.Fix(a.SeatInfo ?? ""));
            float infoH = UiTheme.Small.CalcHeight(info, w - 32f);
            h += infoH;
            var r = new Rect(_w - w - 16f, _h - h - 70f, w, h);
            RegisterTouchRect(r);
            GUI.Box(r, GUIContent.none, UiTheme.PanelSoft);
            GUI.Label(new Rect(r.x + 16f, r.y + 10f, w - 32f, infoH), info, UiTheme.Small);
            float y = r.y + 18f + infoH;
            for (int i = 0; i < options.Count; i++)
            {
                if (Button(new Rect(r.x + 16f, y, w - 32f, 44f), $"{i + 1}.  {options[i].Label}", options[i].Primary ? UiTheme.ButtonPrimary : UiTheme.Button))
                    options[i].Run();
                y += 52f;
            }
            GUI.Label(new Rect(r.x + 16f, r.yMax - 32f, w - 32f, 24f), Game.Input.TouchControlsEnabled ? "Use the button below to stand up." : "Press E to stand up.", UiTheme.Dim);
        }

        // --------------------------------------------------------- activities

        void DrawActivity()
        {
            var ctl = Game.Academic;
            var a = ctl.Activity;
            if (a == null) return;
            var panel = CenterPanel(820f, 560f);
            float x = panel.x + 28f, w = panel.width - 56f, y = panel.y + 22f;

            if (a.IsPicker)
            {
                DrawPicker(ctl, a, panel);
                return;
            }

            var step = a.Current;
            int questionNo = 0, questions = 0;
            for (int i = 0; i < a.Steps.Count; i++)
            {
                if (a.Steps[i].Kind != StepKind.Question) continue;
                questions++;
                if (i <= a.Index) questionNo = questions;
            }
            string header = a.Course != null ? a.Course.FullName : "";
            GUI.Label(new Rect(x, y, w - 150f, 30f), $"<color=#F9C21A><b>{header}</b></color>", UiTheme.Body);
            if (questions > 0 && step != null && step.Kind == StepKind.Question)
                GUI.Label(new Rect(panel.xMax - 190f, y, 160f, 30f), $"Question {questionNo}/{questions}", UiTheme.Dim);
            y += 38f;
            if (step == null) return;
            GUI.Label(new Rect(x, y, w, 36f), UiTheme.Fix(step.Title ?? ""), UiTheme.Heading);
            y += 46f;

            switch (step.Kind)
            {
                case StepKind.Intro:
                    GUI.Label(new Rect(x, y, w, 200f), UiTheme.Fix(step.Body), UiTheme.Body);
                    if (Button(new Rect(x, panel.yMax - 76f, 220f, 52f), "Continue  (Space)", UiTheme.ButtonPrimary)) ctl.Next();
                    break;

                case StepKind.KeyPoint:
                {
                    GUI.Box(new Rect(x, y, w, 150f), GUIContent.none, UiTheme.PanelSoft);
                    GUI.Label(new Rect(x + 18f, y + 14f, w - 36f, 130f), $"<size=22>{UiTheme.Fix(step.Body)}</size>", UiTheme.Body);
                    bool noted = ctl.HasNote(step.KeyPoint);
                    if (Button(new Rect(x, panel.yMax - 76f, 220f, 52f), noted ? "In your notes" : "Take notes", noted ? UiTheme.ButtonSelected : UiTheme.Button) && !noted) ctl.TakeNote();
                    if (Button(new Rect(x + 236f, panel.yMax - 76f, 220f, 52f), "Continue  (Space)", UiTheme.ButtonPrimary)) ctl.Next();
                    GUI.Label(new Rect(x + 476f, panel.yMax - 70f, w - 476f, 48f), "Notes can be reviewed in Tab → Academics before exams.", UiTheme.Dim);
                    break;
                }

                case StepKind.Question:
                {
                    var q = step.Question;
                    var qContent = new GUIContent(UiTheme.Fix(q.Source.Question));
                    float qh = Mathf.Max(32f, UiTheme.Body.CalcHeight(qContent, w));
                    GUI.Label(new Rect(x, y, w, qh), qContent, UiTheme.Body);
                    y += qh + 14f;
                    for (int i = 0; i < 4; i++)
                    {
                        var r = new Rect(x, y, w, 46f);
                        y += 54f;
                        if (step.Hidden[i])
                        {
                            GUI.Box(r, "", UiTheme.Pill);
                            continue;
                        }
                        GUIStyle style = UiTheme.Choice;
                        string label = $"{i + 1}.  {q.Options[i]}";
                        if (step.Chosen >= 0)
                        {
                            if (step.RevealAnswer && i == q.Answer) { style = UiTheme.ButtonSelected; label += "   (correct)"; }
                            else if (i == step.Chosen) style = step.RevealAnswer ? UiTheme.Button : UiTheme.ButtonSelected;
                        }
                        if (Button(r, label, style) && step.Chosen < 0) ctl.Answer(i);
                    }
                    if (step.Chosen >= 0)
                    {
                        string feedback = !step.RevealAnswer ? "Answer recorded." : step.Chosen == q.Answer ? "<color=#7BE0A0><b>Correct!</b></color>" : $"<color=#FF8A80><b>Not quite.</b></color> {UiTheme.Fix(q.Source.Text)}";
                        GUI.Label(new Rect(x, panel.yMax - 128f, w, 48f), feedback, UiTheme.Small);
                        if (Button(new Rect(x, panel.yMax - 76f, 220f, 52f), "Continue  (Space)", UiTheme.ButtonPrimary)) ctl.Next();
                    }
                    else if (a.Type == ActivityType.Exam)
                    {
                        int hints = ctl.HintsAvailable;
                        bool used = System.Array.IndexOf(step.Hidden, true) >= 0;
                        if (hints > 0 && !used && Button(new Rect(x, panel.yMax - 76f, 260f, 52f), $"Use 50/50 hint ({hints} left)")) ctl.UseHint();
                    }
                    break;
                }

                case StepKind.Summary:
                    GUI.Label(new Rect(x, y, w, 220f), UiTheme.Fix(a.Summary ?? ""), UiTheme.Body);
                    if (Button(new Rect(x, panel.yMax - 76f, 220f, 52f), "Finish  (Space)", UiTheme.ButtonPrimary)) ctl.Finish();
                    break;
            }

            if (step.Kind != StepKind.Summary && Button(new Rect(panel.xMax - 196f, panel.yMax - 76f, 168f, 52f), a.Type == ActivityType.Assignment ? "Stop for now" : "Leave early"))
                ctl.Cancel();
        }

        void DrawPicker(AcademicController ctl, AcademicActivity a, Rect panel)
        {
            var record = Game.Session.Academics;
            float x = panel.x + 28f, w = panel.width - 56f, y = panel.y + 22f;
            bool study = a.Type == ActivityType.StudyPicker;
            GUI.Label(new Rect(x, y, w, 36f), study ? "Study a topic (1 hour)" : "Work on an assignment (2 hours)", UiTheme.Heading);
            y += 44f;
            GUI.Label(new Rect(x, y, w, 44f), study
                ? "Studying copies the topic's key points into your notes and builds preparation: every 2 points give you one 50/50 hint in that exam."
                : $"Assignments are due {When(record.AssignmentDeadline)}. Late work loses {AcademicCalendar.LateDayPenalty} marks per day.", UiTheme.Dim);
            y += 52f;
            var view = new Rect(x, y, w, panel.yMax - y - 90f);
            float rowH = study ? 98f : 56f;
            _academicScroll = GUI.BeginScrollView(view, _academicScroll, new Rect(0f, 0f, w - 20f, record.RegisteredCourses.Count * rowH));
            float ry = 0f;
            foreach (var code in record.RegisteredCourses)
            {
                var c = record.Catalog.Get(code);
                var p = record.Progress(code);
                if (study)
                {
                    GUI.Label(new Rect(0f, ry, w - 20f, 26f), $"<b>{c.FullName}</b>   <size=14>preparation {p.preparation}/{AcademicRecord.MaxPreparation} · hints {record.HintsAvailable(code)}</size>", UiTheme.Small);
                    float bw = (w - 20f - 3 * 8f) / 4f;
                    for (int t = 0; t < c.Topics.Count; t++)
                        if (Button(new Rect(t * (bw + 8f), ry + 30f, bw, 56f), $"<size=14>{t + 1}. {c.Topics[t].Title}</size>")) ctl.StartStudy(code, t);
                }
                else
                {
                    bool open = record.AssignmentOpen(code, Game.Session.Clock.TotalMinutes);
                    string status = p.assignmentScore >= 0 ? $"submitted · {p.assignmentScore}%" : open ? "open" : "closed";
                    GUI.Label(new Rect(0f, ry + 12f, w - 220f, 30f), $"<b>{c.FullName}</b>  <size=14>({status})</size>", UiTheme.Small);
                    if (open && Button(new Rect(w - 210f, ry + 4f, 190f, 46f), "Start", UiTheme.ButtonPrimary)) ctl.StartAssignment(code);
                }
                ry += rowH;
            }
            GUI.EndScrollView();
            if (Button(new Rect(x, panel.yMax - 70f, 160f, 50f), "Back")) ctl.Cancel();
        }

        // ------------------------------------------------------------ results

        void DrawResults(SemesterResult result, bool standalone)
        {
            if (result == null) { Game.CloseOverlay(); return; }
            var panel = CenterPanel(900f, 600f);
            float x = panel.x + 28f, w = panel.width - 56f, y = panel.y + 22f;
            GUI.Label(new Rect(x, y, w, 40f), $"Semester results · Year {result.year}, Semester {result.semester}", UiTheme.Heading);
            y += 52f;
            DrawResultTable(result, new Rect(x, y, w, panel.height - 190f));
            float by = panel.yMax - 120f;
            var record = Game.Session.Academics;
            string standing = result.courses.Count == 0
                ? "You did not register any courses this semester."
                : $"GPA <b>{result.gpa:0.00}</b> · CGPA <b>{result.cgpa:0.00}</b> · {GradingScale.ClassOfDegree(result.cgpa)} standing" +
                  (result.probation ? "\n<color=#FF8A80><b>Academic probation:</b> your CGPA is below 1.50. Attend lectures, study and see your lecturers.</color>" : "");
            var resits = record.Resits();
            if (resits.Count > 0) standing += $"\nResits required: {string.Join(", ", resits)}";
            GUI.Label(new Rect(x, by, w - 180f, 100f), standing, UiTheme.Small);
            if (standalone && Button(new Rect(panel.xMax - 196f, panel.yMax - 76f, 168f, 52f), "Close", UiTheme.ButtonPrimary)) Game.CloseOverlay();
        }

        void DrawResultTable(SemesterResult result, Rect area)
        {
            float[] cols = { 0f, 0.46f, 0.55f, 0.64f, 0.72f, 0.8f, 0.88f, 0.95f };
            string[] heads = { "Course", "Cr", "Att.", "Assign.", "Quiz", "Exam", "Total", "Grade" };
            for (int i = 0; i < heads.Length; i++)
                GUI.Label(new Rect(area.x + cols[i] * area.width, area.y, 120f, 26f), $"<b>{heads[i]}</b>", UiTheme.Small);
            float y = area.y + 30f;
            var catalog = Game.Session.Academics.Catalog;
            foreach (var c in result.courses)
            {
                string colour = c.passed ? "#FFFFFF" : "#FF8A80";
                string[] cells =
                {
                    catalog.TryGet(c.courseCode, out var def) ? def.FullName : c.courseCode, c.credits.ToString(),
                    c.attendanceComponent.ToString(), c.assignmentComponent.ToString(), c.quizComponent.ToString(), c.examComponent.ToString(),
                    $"<b>{c.total}</b>", $"<b>{c.grade}</b>",
                };
                for (int i = 0; i < cells.Length; i++)
                    GUI.Label(new Rect(area.x + cols[i] * area.width, y, i == 0 ? area.width * 0.45f : 90f, 26f), $"<color={colour}>{cells[i]}</color>", UiTheme.Small);
                y += 30f;
            }
        }

        // ------------------------------------------------------ academics tab

        void DrawAcademicsTab(Rect view)
        {
            var s = Game.Session;
            var record = s.Academics;
            double now = s.Clock.TotalMinutes;
            GUILayout.BeginArea(view);
            _academicScroll = GUILayout.BeginScrollView(_academicScroll);
            var program = CampusLife.Core.Player.AcademicPrograms.Find(record.ProgramId);
            GUILayout.Label($"<b>{(program != null ? program.Name : record.ProgramId)}</b> · Year {record.Data.year}, Semester {record.Data.semester} · {PhaseLabel(record.Phase(now), s)}", UiTheme.Body);
            if (record.Results.Count > 0) GUILayout.Label($"CGPA <b>{record.Cgpa():0.00}</b> · {GradingScale.ClassOfDegree(record.Cgpa())} standing", UiTheme.Body);
            GUILayout.Space(8f);

            if (!record.Registered)
            {
                if (record.RegistrationOpen(now)) DrawRegistration(record, now);
                else GUILayout.Label("Course registration closed at the end of teaching week 1. You can register again next semester.", UiTheme.Dim);
            }
            else
            {
                Section("Weekly timetable");
                foreach (var m in record.WeeklyTimetable())
                {
                    var c = record.Catalog.Get(m.CourseCode);
                    GUILayout.Label($"{m.Day,-9}  {m.StartMinute / 60:00}:{m.StartMinute % 60:00}–{(m.StartMinute + m.DurationMinutes) / 60:00}:{(m.StartMinute + m.DurationMinutes) % 60:00}   <b>{c.Code}</b> {c.Title} · {m.Room} · {c.Lecturer}", UiTheme.Small);
                }
                GUILayout.Label($"Exams (week 5, LT1): " + string.Join(" · ", ExamLines(record)), UiTheme.Dim);

                Section("Course progress");
                foreach (var code in record.RegisteredCourses)
                {
                    var c = record.Catalog.Get(code);
                    var p = record.Progress(code);
                    int attended = 0;
                    foreach (var r in p.sessions) if (r.mark != AttendanceMark.Absent) attended++;
                    string assignment = p.assignmentScore >= 0 ? $"{p.assignmentScore}%" : record.AssignmentOpen(code, now) ? $"due {When(record.AssignmentDeadline)}" : "—";
                    string quiz = p.quizScore >= 0 ? $"{p.quizScore}%" : "—";
                    string exam = p.examScore >= 0 ? "sat" : "—";
                    GUILayout.Label($"<b>{c.FullName}</b> ({c.Credits} cr)\n   Lectures {attended}/{AcademicCalendar.TeachingWeeks} · participation {record.AttendanceScore(code)}% · assignment {assignment} · quiz {quiz} · exam {exam} · preparation {p.preparation} ({record.HintsAvailable(code)} hints)", UiTheme.Small);
                }

                Section("Notes");
                int noteCount = 0;
                foreach (var code in record.RegisteredCourses)
                {
                    var c = record.Catalog.Get(code);
                    var lines = new List<string>();
                    foreach (var k in c.AllKeyPoints())
                        if (record.HasNote(k.Id)) lines.Add("• " + UiTheme.Fix(k.Text));
                    if (lines.Count == 0) continue;
                    noteCount += lines.Count;
                    GUILayout.Label($"<b>{c.Code}</b>\n" + string.Join("\n", lines), UiTheme.Small);
                }
                if (noteCount == 0) GUILayout.Label("No notes yet. Take notes in lectures, or study topics at a library seat.", UiTheme.Dim);
            }

            foreach (var result in record.Results)
            {
                Section($"Results · Year {result.year}, Semester {result.semester}");
                var catalog = record.Catalog;
                foreach (var c in result.courses)
                    GUILayout.Label($"{(catalog.TryGet(c.courseCode, out var def) ? def.FullName : c.courseCode)}: <b>{c.total}</b> ({c.grade}){(c.passed ? "" : " — resit")}", UiTheme.Small);
                GUILayout.Label($"GPA {result.gpa:0.00} · CGPA {result.cgpa:0.00}{(result.probation ? " · academic probation" : "")}", UiTheme.Small);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static IEnumerable<string> ExamLines(AcademicRecord record)
        {
            foreach (var s in record.Sessions)
                if (s.IsExam)
                {
                    var c = new GameClock(s.Start);
                    yield return $"{s.CourseCode} {GameClock.WeekdayShort(c.Weekday)} {c.Hour:00}:{c.Minute:00}";
                }
        }

        static void Section(string title)
        {
            GUILayout.Space(10f);
            GUILayout.Label($"<color=#F9C21A><b>{title}</b></color>", UiTheme.Body);
        }

        void DrawRegistration(AcademicRecord record, double now)
        {
            var offered = record.Offered();
            if (!_selectionInitialised)
            {
                _selectedCourses.Clear();
                foreach (var c in offered) if (c.Kind == CourseKind.Core) _selectedCourses.Add(c.Code);
                _selectionInitialised = true;
            }
            Section("Course registration");
            GUILayout.Label($"Select your courses: all required courses plus at least one elective ({AcademicRecord.MinCredits}–{AcademicRecord.MaxCredits} credits). Registration closes {When((record.SemesterStartDay + AcademicCalendar.RegistrationCloseDay + 1) * (double)GameClock.MinutesPerDay)}.", UiTheme.Dim);
            int credits = 0;
            foreach (var c in offered)
            {
                bool required = c.Kind == CourseKind.Core;
                bool on = _selectedCourses.Contains(c.Code);
                string label = $"  {c.FullName} · {c.Credits} credits · {(required ? "required" : "elective")} · {c.Lecturer}";
                bool next = GUILayout.Toggle(on, label, UiTheme.ToggleStyle);
                if (!required && next != on)
                {
                    if (next) _selectedCourses.Add(c.Code); else _selectedCourses.Remove(c.Code);
                    _registrationErrors = null;
                }
                if (_selectedCourses.Contains(c.Code)) credits += c.Credits;
            }
            GUILayout.Label($"Selected: <b>{credits}</b> credits", UiTheme.Body);
            if (GUILayout.Button("Register courses", UiTheme.ButtonPrimary, GUILayout.Width(260f), GUILayout.Height(50f)))
            {
                if (record.TryRegister(_selectedCourses, now, out var errors))
                {
                    _registrationErrors = null;
                    _selectionInitialised = false;
                    Toast($"Registered {record.RegisteredCourses.Count} courses ({record.RegisteredCredits()} credits). Your timetable is ready.");
                    Game.Autosave();
                }
                else _registrationErrors = errors;
            }
            if (_registrationErrors != null)
                foreach (var e in _registrationErrors) GUILayout.Label($"<color=#FF8A80>{e}</color>", UiTheme.Small);
        }
    }
}
