using System;
using System.Collections.Generic;

namespace CampusLife.Core.Academics
{
    public sealed class GradeBand
    {
        public string Letter { get; }
        public int MinScore { get; }
        public double Points { get; }

        public GradeBand(string letter, int minScore, double points)
        {
            Letter = letter; MinScore = minScore; Points = points;
        }
    }

    /// <summary>Configurable grading scale for Akwaaba Metropolitan University.</summary>
    public sealed class GradingScale
    {
        readonly List<GradeBand> _bands;

        public int PassMark { get; }
        public double ProbationCgpa { get; }
        public IReadOnlyList<GradeBand> Bands => _bands;

        public GradingScale(IEnumerable<GradeBand> bands, int passMark, double probationCgpa)
        {
            _bands = new List<GradeBand>(bands);
            _bands.Sort((a, b) => b.MinScore.CompareTo(a.MinScore));
            if (_bands.Count == 0 || _bands[_bands.Count - 1].MinScore != 0) throw new ArgumentException("The lowest band must start at 0.");
            PassMark = passMark;
            ProbationCgpa = probationCgpa;
        }

        /// <summary>AMU scale: A 80+, B+ 75, B 70, C+ 65, C 60, D+ 55, D 50, F below 50 (4.0 scale).</summary>
        public static GradingScale Default { get; } = new GradingScale(new[]
        {
            new GradeBand("A", 80, 4.0), new GradeBand("B+", 75, 3.5), new GradeBand("B", 70, 3.0), new GradeBand("C+", 65, 2.5),
            new GradeBand("C", 60, 2.0), new GradeBand("D+", 55, 1.5), new GradeBand("D", 50, 1.0), new GradeBand("F", 0, 0.0),
        }, passMark: 50, probationCgpa: 1.5);

        public GradeBand Grade(int score)
        {
            score = Math.Max(0, Math.Min(100, score));
            foreach (var b in _bands) if (score >= b.MinScore) return b;
            return _bands[_bands.Count - 1];
        }

        public bool Passed(int score) => score >= PassMark;

        /// <summary>Credit-weighted grade point average, rounded to two decimals.</summary>
        public static double Gpa(IEnumerable<(int credits, double points)> results)
        {
            double weighted = 0;
            int credits = 0;
            foreach (var (c, p) in results)
            {
                weighted += c * p;
                credits += c;
            }
            return credits == 0 ? 0 : Math.Round(weighted / credits, 2, MidpointRounding.AwayFromZero);
        }

        public static string ClassOfDegree(double cgpa)
        {
            if (cgpa >= 3.6) return "First Class";
            if (cgpa >= 3.0) return "Second Class (Upper Division)";
            if (cgpa >= 2.5) return "Second Class (Lower Division)";
            if (cgpa >= 2.0) return "Third Class";
            if (cgpa >= 1.5) return "Pass";
            return "Below pass standing";
        }
    }

    /// <summary>How a course total is made up (percent of the final mark).</summary>
    public sealed class AssessmentWeights
    {
        public int AttendanceAndParticipation = 10;
        public int Assignment = 20;
        public int Quiz = 20;
        public int Exam = 50;

        public static AssessmentWeights Default { get; } = new AssessmentWeights();

        public int Total => AttendanceAndParticipation + Assignment + Quiz + Exam;
    }
}
