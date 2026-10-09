using System.Collections.Generic;

namespace CampusLife.Core.Player
{
    public sealed class AcademicProgram
    {
        public string Id { get; }
        public string Name { get; }
        public string Faculty { get; }

        public AcademicProgram(string id, string name, string faculty) { Id = id; Name = name; Faculty = faculty; }
    }

    /// <summary>Programs offered by Akwaaba Metropolitan University. Course structures arrive in Phase 3.</summary>
    public static class AcademicPrograms
    {
        public static readonly IReadOnlyList<AcademicProgram> All = new[]
        {
            new AcademicProgram("computer_science", "Computer Science", "Faculty of Computing"),
            new AcademicProgram("business_administration", "Business Administration", "School of Business"),
            new AcademicProgram("accounting", "Accounting", "School of Business"),
            new AcademicProgram("economics", "Economics", "Faculty of Social Sciences"),
            new AcademicProgram("engineering", "Engineering", "Faculty of Engineering"),
            new AcademicProgram("nursing", "Nursing", "School of Health Sciences"),
            new AcademicProgram("communication_studies", "Communication Studies", "Faculty of Arts"),
            new AcademicProgram("education", "Education", "Faculty of Education"),
            new AcademicProgram("political_science", "Political Science", "Faculty of Social Sciences"),
            new AcademicProgram("psychology", "Psychology", "Faculty of Social Sciences"),
        };

        public static AcademicProgram Find(string id)
        {
            foreach (var p in All) if (p.Id == id) return p;
            return null;
        }
    }
}
