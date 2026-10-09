using System.Collections;
using System.Linq;
using CampusLife.Core.Academics;
using CampusLife.Core.Save;
using CampusLife.Runtime;
using CampusLife.Runtime.Academics;
using CampusLife.Runtime.Infrastructure;
using CampusLife.Runtime.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CampusLife.Tests.PlayMode
{
    /// <summary>Phase 3: registration, attending a lecture in the hall, studying and resting.</summary>
    public class AcademicFlowTests
    {
        static readonly string[] Selection = { "AMU 101", "AMU 103", "CSC 101", "CSC 103", "AMU 105" };
        GameBootstrap _game;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameBootstrap.AutoBootstrap = false;
            SaveSystem.StorageOverride = new MemorySaveStorage();
            _game = new GameObject("TestGame").AddComponent<GameBootstrap>();
            yield return null;
            _game.BeginCharacterCreation();
            _game.DraftProfile.displayName = "Test Student";
            _game.DraftProfile.programId = "computer_science";
            Assert.IsTrue(_game.TryStartNewGame(out var error), error);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_game != null) Object.Destroy(_game.gameObject);
            yield return null;
            SaveSystem.StorageOverride = null;
            GameBootstrap.AutoBootstrap = true;
        }

        IEnumerator Enter(string area, string spawn)
        {
            _game.Areas.RequestTransition(area, spawn);
            float timeout = Time.time + 6f;
            yield return null;
            while (_game.Areas.IsTransitioning && Time.time < timeout) yield return null;
            Assert.AreEqual(area, _game.Areas.Current.Id);
        }

        [UnityTest]
        public IEnumerator AttendALectureInTheHall()
        {
            var record = _game.Session.Academics;
            Assert.IsTrue(record.TryRegister(Selection, _game.Session.Clock.TotalMinutes, out var errors), string.Join(" ", errors));
            var lecture = record.Sessions.First(s => !s.IsExam);
            _game.Session.Clock.AdvanceMinutes(lecture.Start - 20 - _game.Session.Clock.TotalMinutes);

            yield return Enter(LectureInteriorBuilder.AreaId, "entrance");
            var seat = Object.FindObjectsByType<SeatInteractable>(FindObjectsSortMode.None)
                .First(x => x.RoomId == lecture.Room && !x.Occupied && x.isActiveAndEnabled);
            _game.Player.SitOn(seat);
            yield return null;
            Assert.IsTrue(_game.Academic.HasSeatOptions, "Seated in the right hall shows options");
            StringAssert.Contains("Wait", _game.Academic.SeatOptions[0].Label);

            _game.Academic.SeatOptions[0].Run();
            yield return null;
            var activity = _game.Academic.Activity;
            Assert.IsNotNull(activity);
            Assert.AreEqual(ActivityType.Lecture, activity.Type);
            Assert.AreEqual(Overlay.Activity, _game.Overlay);
            Assert.AreEqual(AttendanceMark.Present, record.RecordFor(lecture).mark);

            _game.Academic.Next();                     // intro
            _game.Academic.TakeNote(); _game.Academic.Next();
            _game.Academic.TakeNote(); _game.Academic.Next();
            var q = activity.Current;
            Assert.AreEqual(StepKind.Question, q.Kind);
            _game.Academic.Answer(q.Question.Answer);
            _game.Academic.Next();
            Assert.AreEqual(StepKind.Summary, activity.Current.Kind);
            _game.Academic.Finish();
            yield return null;

            Assert.IsNull(_game.Academic.Activity);
            Assert.AreEqual(Overlay.None, _game.Overlay);
            Assert.GreaterOrEqual(_game.Session.Clock.TotalMinutes, lecture.End, "Time moves on to the end of the lecture");
            Assert.AreEqual(1, record.RecordFor(lecture).challenge);
            Assert.AreEqual(2, activity.NotesTaken);
            Assert.IsTrue(_game.Player.ControlEnabled);
        }

        [UnityTest]
        public IEnumerator StudyInTheLibrary()
        {
            var record = _game.Session.Academics;
            record.TryRegister(Selection, _game.Session.Clock.TotalMinutes, out _);
            yield return Enter(LibraryInteriorBuilder.AreaId, "entrance");
            var seat = Object.FindObjectsByType<SeatInteractable>(FindObjectsSortMode.None).First(x => x.RoomId == "library" && !x.Occupied && x.isActiveAndEnabled);
            _game.Player.SitOn(seat);
            yield return null;
            Assert.IsTrue(_game.Academic.HasSeatOptions);
            double before = _game.Session.Clock.TotalMinutes;
            _game.Academic.SeatOptions[0].Run();
            Assert.AreEqual(ActivityType.StudyPicker, _game.Academic.Activity.Type);
            _game.Academic.StartStudy("CSC 101", 0);
            yield return null;
            Assert.IsNull(_game.Academic.Activity);
            Assert.AreEqual(1, record.Progress("CSC 101").preparation);
            Assert.GreaterOrEqual(_game.Session.Clock.TotalMinutes, before + 60);
            Assert.IsFalse(_game.Session.Clock.IsPaused, "Clock resumes after studying");
        }

        [UnityTest]
        public IEnumerator RestingAtUnityHallSkipsToMorning()
        {
            var clock = _game.Session.Clock;
            clock.AdvanceMinutes(21 * 60 - clock.MinuteOfDay); // 21:00 on day 1
            int day = clock.Day;
            Object.FindObjectsByType<RestInteractable>(FindObjectsSortMode.None).First().Interact(_game.Player);
            float timeout = Time.time + 6f;
            while (clock.Day == day && Time.time < timeout) yield return null;
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(day + 1, clock.Day);
            Assert.AreEqual(6, clock.Hour);
            Assert.IsTrue(_game.Player.ControlEnabled);
        }
    }
}
