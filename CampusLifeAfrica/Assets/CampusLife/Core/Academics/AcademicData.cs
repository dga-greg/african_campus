using System;
using System.Collections.Generic;

namespace CampusLife.Core.Academics
{
    public enum AttendanceMark { Absent = 0, Late = 1, Present = 2 }

    [Serializable]
    public class SessionRecord
    {
        public string key;
        public AttendanceMark mark;
        /// <summary>Lecture challenge: -1 not asked, 0 wrong, 1 right.</summary>
        public int challenge = -1;
    }

    [Serializable]
    public class CourseProgress
    {
        public string courseCode;
        public List<SessionRecord> sessions = new List<SessionRecord>();
        public int assignmentScore = -1;
        public double assignmentSubmittedMinute = -1;
        public int quizScore = -1;
        public int examScore = -1;
        public int preparation;
        public int hintsUsed;
    }

    [Serializable]
    public class CourseResult
    {
        public string courseCode;
        public int credits;
        public int attendanceComponent;
        public int assignmentComponent;
        public int quizComponent;
        public int examComponent;
        public int total;
        public string grade;
        public double gradePoint;
        public bool passed;
    }

    [Serializable]
    public class SemesterResult
    {
        public int year;
        public int semester;
        public List<CourseResult> courses = new List<CourseResult>();
        public double gpa;
        public double cgpa;
        public bool probation;
        public double releasedMinute;
    }

    [Serializable]
    public class AcademicData
    {
        public int year = 1;
        public int semester = 1;
        /// <summary>Game day the current semester began (its orientation Monday).</summary>
        public int semesterStartDay;
        public bool registered;
        public List<string> registeredCourses = new List<string>();
        public List<CourseProgress> progress = new List<CourseProgress>();
        public List<string> notes = new List<string>();
        public List<SemesterResult> results = new List<SemesterResult>();
    }
}
