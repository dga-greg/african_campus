using System;
using System.Collections.Generic;
using CampusLife.Core.Npcs;

namespace CampusLife.Core.Academics
{
    /// <summary>A fact taught in a lecture, paired with the question that tests it.</summary>
    public sealed class KeyPoint
    {
        public string Id { get; }
        public string Text { get; }
        public string Question { get; }
        public IReadOnlyList<string> Options { get; }
        public int Answer { get; }

        public KeyPoint(string id, string text, string question, string[] options, int answer)
        {
            if (options == null || options.Length != 4) throw new ArgumentException($"{id}: questions need exactly four options.");
            if (answer < 0 || answer >= options.Length) throw new ArgumentOutOfRangeException(nameof(answer));
            Id = id; Text = text; Question = question; Options = options; Answer = answer;
        }
    }

    public sealed class Topic
    {
        public int Index { get; }
        public string Title { get; }
        public IReadOnlyList<KeyPoint> KeyPoints { get; }

        public Topic(int index, string title, KeyPoint[] keyPoints)
        {
            Index = index; Title = title; KeyPoints = keyPoints;
        }
    }

    public enum CourseKind { Core, Elective }

    public sealed class CourseDefinition
    {
        public string Code { get; }
        public string Title { get; }
        public int Credits { get; }
        /// <summary>Programme the course belongs to; null for university-wide courses.</summary>
        public string ProgramId { get; }
        public CourseKind Kind { get; }
        public string Lecturer { get; }
        public int Level { get; }
        public int Semester { get; }
        public IReadOnlyList<Topic> Topics { get; }
        public IReadOnlyList<string> Prerequisites { get; }

        public CourseDefinition(string code, string title, int credits, string programId, CourseKind kind, string lecturer,
            int level, int semester, Topic[] topics, string[] prerequisites = null)
        {
            Code = code; Title = title; Credits = credits; ProgramId = programId; Kind = kind; Lecturer = lecturer;
            Level = level; Semester = semester; Topics = topics; Prerequisites = prerequisites ?? new string[0];
        }

        public string FullName => $"{Code} {Title}";

        public IEnumerable<KeyPoint> AllKeyPoints()
        {
            foreach (var t in Topics) foreach (var k in t.KeyPoints) yield return k;
        }
    }

    public sealed class CourseCatalog
    {
        readonly Dictionary<string, CourseDefinition> _courses = new Dictionary<string, CourseDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, KeyPoint> _keyPoints = new Dictionary<string, KeyPoint>(StringComparer.Ordinal);

        public CourseCatalog Add(CourseDefinition c)
        {
            if (_courses.ContainsKey(c.Code)) throw new ArgumentException($"Duplicate course {c.Code}.");
            _courses.Add(c.Code, c);
            foreach (var k in c.AllKeyPoints())
            {
                if (_keyPoints.ContainsKey(k.Id)) throw new ArgumentException($"Duplicate key point {k.Id}.");
                _keyPoints.Add(k.Id, k);
            }
            return this;
        }

        public bool TryGet(string code, out CourseDefinition course) => _courses.TryGetValue(code ?? "", out course);
        public CourseDefinition Get(string code) => TryGet(code, out var c) ? c : throw new KeyNotFoundException($"Unknown course {code}.");
        public bool TryGetKeyPoint(string id, out KeyPoint k) => _keyPoints.TryGetValue(id ?? "", out k);
        public IEnumerable<CourseDefinition> All => _courses.Values;

        /// <summary>Courses a student of a programme may take in a level and semester (their core plus university-wide courses).</summary>
        public List<CourseDefinition> Offered(string programId, int level, int semester)
        {
            var list = new List<CourseDefinition>();
            foreach (var c in _courses.Values)
                if (c.Level == level && c.Semester == semester && (c.ProgramId == null || c.ProgramId == programId)) list.Add(c);
            list.Sort((a, b) => string.CompareOrdinal(a.Code, b.Code));
            return list;
        }

        public List<string> Validate()
        {
            var errors = new List<string>();
            foreach (var c in _courses.Values)
            {
                if (c.Topics.Count != AcademicCalendar.TeachingWeeks) errors.Add($"{c.Code}: needs {AcademicCalendar.TeachingWeeks} topics.");
                foreach (var t in c.Topics)
                    if (t.KeyPoints.Count != 2) errors.Add($"{c.Code} topic {t.Index}: needs two key points.");
                foreach (var p in c.Prerequisites)
                    if (!_courses.ContainsKey(p)) errors.Add($"{c.Code}: unknown prerequisite {p}.");
                if (c.Credits <= 0) errors.Add($"{c.Code}: credits must be positive.");
            }
            return errors;
        }
    }

    /// <summary>A question as shown to the player, with options shuffled deterministically.</summary>
    public sealed class PresentedQuestion
    {
        public KeyPoint Source { get; }
        public string[] Options { get; }
        public int Answer { get; }

        PresentedQuestion(KeyPoint source, string[] options, int answer)
        {
            Source = source; Options = options; Answer = answer;
        }

        public static PresentedQuestion Shuffle(KeyPoint k, string seedKey)
        {
            var order = new[] { 0, 1, 2, 3 };
            var rng = DeterministicRandom.From(k.Id + "|" + seedKey);
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            var options = new string[4];
            int answer = 0;
            for (int i = 0; i < 4; i++)
            {
                options[i] = k.Options[order[i]];
                if (order[i] == k.Answer) answer = i;
            }
            return new PresentedQuestion(k, options, answer);
        }

        /// <summary>The two wrong options a 50/50 hint removes (deterministic).</summary>
        public int[] HintRemovals()
        {
            var wrong = new List<int>();
            for (int i = 0; i < 4; i++) if (i != Answer) wrong.Add(i);
            var rng = DeterministicRandom.From(Source.Id + "|hint");
            wrong.RemoveAt(rng.Range(0, wrong.Count));
            return wrong.ToArray();
        }
    }
}
