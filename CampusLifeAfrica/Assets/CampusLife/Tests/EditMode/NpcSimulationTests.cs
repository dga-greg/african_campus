using System;
using System.Collections.Generic;
using System.Linq;
using CampusLife.Core.Content;
using CampusLife.Core.Dialogue;
using CampusLife.Core.Npcs;
using CampusLife.Core.Player;
using CampusLife.Core.Session;
using CampusLife.Core.Time;
using NUnit.Framework;

namespace CampusLife.Tests
{
    public class NpcRosterAndScheduleTests
    {
        static PlaceCatalog Catalog()
        {
            var c = PlaceIds.CreateCatalog("library_interior");
            // Rough layout of the real campus (metres).
            c.SetAccessPoint(PlaceIds.Hostel, -30f, 20f);
            c.SetAccessPoint(PlaceIds.LectureComplex, 28f, 5f);
            c.SetAccessPoint(PlaceIds.OffCampus, 0f, -86f);
            c.SetAccessPoint(PlaceIds.Library, 0f, 42f);
            c.SetAccessPoint(PlaceIds.Plaza, 0f, -15f);
            c.SetAccessPoint(PlaceIds.Kiosk, 12f, -63f);
            c.SetAccessPoint(PlaceIds.SportsField, -38f, -26f);
            c.SetAccessPoint(PlaceIds.ShuttleStop, 24f, -54f);
            c.SetAccessPoint(PlaceIds.Lawn, -20f, 15f);
            c.SetAccessPoint(PlaceIds.SecurityPost, 9f, -83f);
            c.SetAccessPoint(PlaceIds.PatrolRoute, 0f, -40f);
            c.SetAccessPoint(PlaceIds.SweepRoute, 0f, -20f);
            return c;
        }

        [Test]
        public void RosterIsStableAndUnique()
        {
            var roster = Phase2Content.Roster;
            Assert.GreaterOrEqual(roster.Count, 20, "Phase 2 requires at least 20 autonomous NPCs");
            Assert.AreEqual(roster.Count, roster.Select(r => r.Id).Distinct().Count());
            Assert.AreEqual(roster.Count, roster.Select(r => r.Definition.DisplayName).Distinct().Count());
            Assert.AreSame(roster, Phase2Content.Roster);
            Assert.AreEqual("student_00", roster[0].Id);
            foreach (var role in new[] { NpcRole.Student, NpcRole.Lecturer, NpcRole.Security, NpcRole.Cleaner })
                Assert.IsTrue(roster.Any(r => r.Role == role), $"Roster has a {role}");
            foreach (var p in roster)
                Assert.IsNull(Phase1Content.FindNpc(p.Id), "Resident ids never clash with story NPCs");
        }

        [Test]
        public void SchedulesAreValidForEveryResidentAndEveryDayOfTheWeek()
        {
            var places = Catalog();
            foreach (var npc in Phase2Content.Roster)
                for (int day = 0; day < 14; day++)
                {
                    var s = ScheduleGenerator.Generate(npc, day);
                    CollectionAssert.IsEmpty(s.Validate(places), $"{npc.Id} day {day}");
                    Assert.AreEqual(0, s.Entries[0].StartMinute);
                }
        }

        [Test]
        public void SchedulesAreDeterministic()
        {
            var npc = Phase2Content.Roster[3];
            var a = ScheduleGenerator.Generate(npc, 2).Entries.Select(e => e.ToString()).ToList();
            var b = ScheduleGenerator.Generate(npc, 2).Entries.Select(e => e.ToString()).ToList();
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void ResidentsDoNotAllMoveAtOnceOrRepeatIdenticalDays()
        {
            var students = Phase2Content.Roster.Where(r => r.Role == NpcRole.Student).ToList();
            var breakfastTimes = students.Select(s => ScheduleGenerator.Generate(s, 0).Entries[1].StartMinute).Distinct().Count();
            Assert.Greater(breakfastTimes, students.Count / 2, "Departure times are spread out");

            var monday = ScheduleGenerator.Generate(students[0], 0).Entries.Select(e => e.StartMinute);
            var nextMonday = ScheduleGenerator.Generate(students[0], 7).Entries.Select(e => e.StartMinute);
            CollectionAssert.AreNotEqual(monday.ToList(), nextMonday.ToList(), "The same weekday varies week to week");
        }

        [Test]
        public void SchedulesFollowRolesAndWeekdays()
        {
            var lecturer = Phase2Content.Roster.First(r => r.Role == NpcRole.Lecturer);
            var saturday = ScheduleGenerator.Generate(lecturer, 5);
            Assert.AreEqual(1, saturday.Entries.Count, "Lecturers are off campus at weekends");
            Assert.AreEqual(PlaceIds.OffCampus, saturday.EntryAt(720).PlaceId);
            Assert.AreEqual(ActivityKind.Lecture, ScheduleGenerator.Generate(lecturer, 0).EntryAt(9 * 60).Activity);

            var day = Phase2Content.Roster.First(r => r.Role == NpcRole.Security && !r.NightShift);
            var night = Phase2Content.Roster.First(r => r.Role == NpcRole.Security && r.NightShift);
            for (int minute = 0; minute < 1440; minute += 30)
            {
                bool dayOn = ScheduleGenerator.Generate(day, 1).EntryAt(minute).PlaceId != PlaceIds.OffCampus;
                bool nightOn = ScheduleGenerator.Generate(night, 1).EntryAt(minute).PlaceId != PlaceIds.OffCampus;
                Assert.IsTrue(dayOn || nightOn, $"A guard is on duty at {minute / 60:00}:{minute % 60:00}");
            }

            foreach (var student in Phase2Content.Roster.Where(r => r.Role == NpcRole.Student))
            {
                var s = ScheduleGenerator.Generate(student, 1);
                Assert.AreEqual(PlaceIds.Hostel, s.EntryAt(3 * 60).PlaceId, "Students sleep in the hostel at 03:00");
            }
        }

        [Test]
        public void ProgrammesHaveDifferentTimetables()
        {
            bool csMonday = ScheduleGenerator.HasLecture("computer_science", Weekday.Monday, true);
            bool baMonday = ScheduleGenerator.HasLecture("business_administration", Weekday.Monday, true);
            Assert.AreNotEqual(csMonday, baMonday);
            Assert.IsFalse(ScheduleGenerator.HasLecture("computer_science", Weekday.Saturday, true));
        }

        [Test]
        public void SimulationMovesResidentsBetweenPlacesAtWalkingPace()
        {
            var places = Catalog();
            var sim = new CampusSimulation(Phase2Content.Roster, places);
            var npc = Phase2Content.Roster.First(r => r.Role == NpcRole.Student);
            var schedule = sim.ScheduleFor(npc, 0);
            var departure = schedule.Entries[1];

            sim.Reset(departure.StartMinute - 5);
            var state = sim.Get(npc.Id);
            Assert.AreEqual(PlaceIds.Hostel, state.PlaceId);
            Assert.AreEqual(NpcMode.AtPlace, state.Mode);

            int departed = 0, arrived = 0;
            sim.Departed += s => { if (s.Id == npc.Id) departed++; };
            sim.Arrived += s => { if (s.Id == npc.Id) arrived++; };

            double t = departure.StartMinute - 5;
            for (; t < departure.StartMinute + 0.5; t += 0.5) sim.Advance(t);
            Assert.AreEqual(1, departed);
            Assert.AreEqual(NpcMode.Travelling, state.Mode);
            Assert.AreEqual(departure.PlaceId, state.PlaceId);

            var (hx, hz) = places.AccessPoint(PlaceIds.Hostel);
            var (dx, dz) = places.AccessPoint(departure.PlaceId);
            double seconds = Math.Sqrt((dx - hx) * (dx - hx) + (dz - hz) * (dz - hz)) * CampusSimulation.RouteFactor / npc.WalkSpeed;
            Assert.AreEqual(seconds * sim.MinutesPerRealSecond, state.ArriveMinute - state.DepartMinute, 0.01, "Walking time converted to game time");

            var mid = sim.PositionAt(state, (state.DepartMinute + state.ArriveMinute) / 2);
            Assert.AreEqual((hx + dx) / 2, mid.x, 0.5);

            for (; t < state.ArriveMinute + 1; t += 0.5) sim.Advance(t);
            Assert.AreEqual(1, arrived);
            Assert.AreEqual(NpcMode.AtPlace, state.Mode);
            Assert.AreEqual(departure.Activity, state.Activity);
        }

        [Test]
        public void WalkingTimesFitBetweenScheduleBlocks()
        {
            var sim = new CampusSimulation(Phase2Content.Roster, Catalog());
            var places = sim.Places;
            int legs = 0, late = 0;
            foreach (var npc in Phase2Content.Roster)
                for (int day = 0; day < 7; day++)
                {
                    var e = sim.ScheduleFor(npc, day).Entries;
                    for (int i = 1; i < e.Count; i++)
                    {
                        var (fx, fz) = places.AccessPoint(e[i - 1].PlaceId);
                        var (tx, tz) = places.AccessPoint(e[i].PlaceId);
                        double travel = sim.TravelMinutes(fx, fz, tx, tz, npc.WalkSpeed);
                        int next = i + 1 < e.Count ? e[i + 1].StartMinute : 1440;
                        legs++;
                        if (travel > (next - e[i].StartMinute) * 0.6) late++;
                    }
                }
            Assert.Greater(legs, 500);
            Assert.Less(late, legs / 50, $"{late} of {legs} legs leave too little time at the destination");
        }

        [Test]
        public void PhysicalArrivalCanCompleteTravelEarly()
        {
            var sim = new CampusSimulation(Phase2Content.Roster, Catalog());
            var npc = Phase2Content.Roster[0];
            var departure = sim.ScheduleFor(npc, 0).Entries[1];
            sim.Reset(departure.StartMinute - 1);
            sim.Advance(departure.StartMinute);
            Assert.AreEqual(NpcMode.Travelling, sim.Get(npc.Id).Mode);
            sim.NotifyArrived(npc.Id, departure.StartMinute + 1);
            Assert.AreEqual(NpcMode.AtPlace, sim.Get(npc.Id).Mode);
        }

        [Test]
        public void LongTimeJumpsSnapToSchedule()
        {
            var sim = new CampusSimulation(Phase2Content.Roster, Catalog());
            sim.Reset(60);
            sim.Advance(60 + 6 * 60); // slept six hours
            foreach (var s in sim.States)
            {
                Assert.AreEqual(NpcMode.AtPlace, s.Mode);
                Assert.AreEqual(sim.CurrentEntry(s.Profile, 420).PlaceId, s.PlaceId);
            }
        }

        [Test]
        public void CampusIsBusierByDayThanAtNight()
        {
            var sim = new CampusSimulation(Phase2Content.Roster, Catalog());
            int busiest = 0;
            for (int minute = 9 * 60; minute <= 17 * 60; minute += 30)
            {
                sim.Reset(minute);
                busiest = Math.Max(busiest, sim.CountOutdoors());
            }
            sim.Reset(3 * 60);
            int night = sim.CountOutdoors();
            Assert.Greater(busiest, 8, "Plenty of residents are out during the day");
            Assert.Less(night, 4, "Only night security is out at 03:00");
            Assert.Greater(night, 0, "The night guard keeps patrolling");
        }

        [Test]
        public void QueueIsFirstComeFirstServed()
        {
            var q = new SlotQueue(3);
            Assert.IsTrue(q.Join("a"));
            Assert.IsTrue(q.Join("b"));
            Assert.IsTrue(q.Join("a"));
            Assert.IsTrue(q.Join("c"));
            Assert.IsFalse(q.Join("d"), "Full queue");
            Assert.AreEqual(1, q.PositionOf("b"));
            q.Leave("a");
            Assert.AreEqual("b", q.Front);
            Assert.AreEqual(0, q.PositionOf("b"));
            Assert.AreEqual(-1, q.PositionOf("a"));
        }

        [Test]
        public void RandomIsReproducible()
        {
            var a = DeterministicRandom.From("x", 3);
            var b = DeterministicRandom.From("x", 3);
            for (int i = 0; i < 10; i++) Assert.AreEqual(a.NextULong(), b.NextULong());
            var r = new DeterministicRandom(1);
            for (int i = 0; i < 1000; i++)
            {
                int v = r.Range(2, 5);
                Assert.IsTrue(v >= 2 && v < 5);
                double d = r.NextDouble();
                Assert.IsTrue(d >= 0 && d < 1);
            }
        }
    }

    public class AmbientDialogueTests
    {
        GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame(new PlayerProfileData { displayName = "Akos", programId = "economics" }, Phase1Content.CreateItemCatalog(), "s");
        }

        DialogueRunner Talk(NpcProfile npc)
        {
            var graph = Phase2Content.CreateDialogues().First(g => g.Id == npc.Definition.DialogueId);
            var r = new DialogueRunner(graph, _session, npc.Definition.DisplayName, npc.Id);
            r.SetVariable("first", npc.Definition.DisplayName.Split(' ')[0]);
            r.SetVariable("their_program", "Nursing");
            r.SetVariable("status", Phase2Content.DescribeIntention(ActivityKind.Study, true));
            r.SetVariable("news", Phase2Content.NewsFor(npc.Id, 0));
            r.Start();
            return r;
        }

        [Test]
        public void AmbientGraphsAreValidAndEveryResidentHasOne()
        {
            var graphs = Phase2Content.CreateDialogues();
            foreach (var g in graphs) CollectionAssert.IsEmpty(g.Validate(), g.Id);
            foreach (var npc in Phase2Content.Roster)
                Assert.IsTrue(graphs.Any(g => g.Id == npc.Definition.DialogueId), npc.Id);
            foreach (var g in graphs)
                foreach (var node in g.Nodes)
                    foreach (var e in node.Choices.SelectMany(c => c.Effects).Concat(node.OnEnter))
                        if (e.Type == EffectType.Remember)
                            Assert.AreNotEqual(e.Key, Phase2Content.DescribeMemory(e.Key), e.Key);
        }

        [Test]
        public void SharedGraphKeepsSeparateMemoriesPerStudent()
        {
            var a = Phase2Content.Roster[0];
            var b = Phase2Content.Roster[1];
            var talkA = Talk(a);
            StringAssert.Contains("Heading to Nkabom Library", talkA.CurrentText, "Status reflects what they are doing");
            talkA.Choose(0); // introduce
            StringAssert.Contains(a.Definition.DisplayName.Split(' ')[0], talkA.CurrentText);
            Assert.IsTrue(_session.Relationships.Get(a.Id).Remembers("introduced"));
            Assert.IsFalse(_session.Relationships.Knows(b.Id));

            Assert.AreEqual("again", Talk(a).Current.Id, "Student A remembers the player");
            Assert.AreEqual("intro", Talk(b).Current.Id, "Student B has never met the player");
        }

        [Test]
        public void BumpingIntoSomeoneIsRemembered()
        {
            var npc = Phase2Content.Roster[2];
            _session.Relationships.Get(npc.Id).Remember("bumped_into", CampusLife.Core.Social.MemoryKind.Conflict, 1);
            var r = Talk(npc);
            Assert.AreEqual("bumped", r.Current.Id);
        }

        [Test]
        public void AnyNpcLookupCoversStoryAndResidents()
        {
            Assert.AreEqual("Esi Mensah", Phase2Content.FindAnyNpc(Ids.NpcEsi).DisplayName);
            Assert.AreEqual(Phase2Content.Roster[5].Definition.DisplayName, Phase2Content.FindAnyNpc(Phase2Content.Roster[5].Id).DisplayName);
            Assert.IsNull(Phase2Content.FindAnyNpc("nobody"));
        }

        [Test]
        public void SharedGraphWithoutNpcIdIsRejected()
        {
            var graph = Phase2Content.CreateDialogues()[0];
            Assert.Throws<ArgumentException>(() => new DialogueRunner(graph, _session, "x"));
        }
    }
}
