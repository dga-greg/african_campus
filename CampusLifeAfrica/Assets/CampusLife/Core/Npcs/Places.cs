namespace CampusLife.Core.Npcs
{
    /// <summary>Well-known place ids used by schedules, the world builder and tests.</summary>
    public static class PlaceIds
    {
        /// <summary>Area id of the outdoor campus.</summary>
        public const string CampusArea = "campus";

        public const string Hostel = "hostel";
        public const string LectureComplex = "lecture_complex";
        public const string OffCampus = "off_campus";
        public const string Library = "library";
        public const string Plaza = "plaza";
        public const string Kiosk = "kiosk";
        public const string SportsField = "sports_field";
        public const string ShuttleStop = "shuttle_stop";
        public const string Lawn = "lawn";
        public const string SecurityPost = "security_post";
        public const string PatrolRoute = "patrol_route";
        public const string SweepRoute = "sweep_route";

        /// <param name="lectureArea">Interior area of the Lecture Theatre Complex, or null to keep it hidden.</param>
        public static PlaceCatalog CreateCatalog(string libraryArea, string lectureArea = null)
        {
            const string campusArea = CampusArea;
            return new PlaceCatalog()
                .Add(new PlaceDefinition(Hostel, "Unity Hall", null))
                .Add(new PlaceDefinition(LectureComplex, "Lecture Theatre Complex", lectureArea))
                .Add(new PlaceDefinition(OffCampus, "Off campus", null))
                .Add(new PlaceDefinition(Library, "Nkabom Library", libraryArea))
                .Add(new PlaceDefinition(Plaza, "Akwaaba Plaza", campusArea))
                .Add(new PlaceDefinition(Kiosk, "Auntie Akosua's kiosk", campusArea))
                .Add(new PlaceDefinition(SportsField, "Sports field", campusArea))
                .Add(new PlaceDefinition(ShuttleStop, "Shuttle stop", campusArea))
                .Add(new PlaceDefinition(Lawn, "West lawn", campusArea))
                .Add(new PlaceDefinition(SecurityPost, "Security booth", campusArea))
                .Add(new PlaceDefinition(PatrolRoute, "Patrol route", campusArea))
                .Add(new PlaceDefinition(SweepRoute, "Plaza and walkway", campusArea));
        }
    }
}
