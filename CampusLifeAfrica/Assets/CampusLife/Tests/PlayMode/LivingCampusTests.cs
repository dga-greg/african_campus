using System.Collections;
using System.Linq;
using CampusLife.Core.Npcs;
using CampusLife.Core.Save;
using CampusLife.Runtime;
using CampusLife.Runtime.Infrastructure;
using CampusLife.Runtime.Npc;
using CampusLife.Runtime.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CampusLife.Tests.PlayMode
{
    /// <summary>Phase 2 checks: navigation bake, autonomous residents, LOD, slots, queues and conversations.</summary>
    public class LivingCampusTests
    {
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
            Assert.IsTrue(_game.TryStartNewGame(out var error), error);
            float timeout = Time.realtimeSinceStartup + 60f;
            while (!_game.Residents.IsNavigationReady(CampusExteriorBuilder.AreaId) && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.IsTrue(_game.Residents.IsNavigationReady(CampusExteriorBuilder.AreaId), "Campus NavMesh bakes within a minute");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_game != null) Object.Destroy(_game.gameObject);
            yield return null;
            SaveSystem.StorageOverride = null;
            GameBootstrap.AutoBootstrap = true;
        }

        IEnumerator WaitSeconds(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        [UnityTest]
        public IEnumerator ResidentsAppearNearThePlayerWithinLimits()
        {
            yield return WaitSeconds(3f);
            var r = _game.Residents;
            Assert.Greater(r.ActiveCount, 0, "Residents get bodies near the gate on Monday morning");
            Assert.LessOrEqual(r.ActiveCount, r.MaxActive);
            Assert.AreEqual(r.ActiveCount, r.HighCount + r.MediumCount);
            Assert.GreaterOrEqual(r.Sim.States.Count, 20);
        }

        [UnityTest]
        public IEnumerator TravellingResidentsMakeProgress()
        {
            yield return WaitSeconds(3f);
            var walker = _game.Residents.ActiveBodies.FirstOrDefault(b => b.IsTravelling);
            if (walker == null) Assert.Inconclusive("Nobody was walking at this moment");
            var state = _game.Residents.Sim.Get(walker.Profile.Id);
            Assert.IsTrue(_game.Slots.TryGetAccess(state.PlaceId, out var dest));
            float before = Vector3.Distance(walker.transform.position, dest);
            yield return WaitSeconds(3f);
            if (!walker.gameObject.activeSelf || !walker.IsTravelling) yield break; // arrived / went inside
            Assert.Less(Vector3.Distance(walker.transform.position, dest), before - 1f, "Walking toward the destination");
        }

        [UnityTest]
        public IEnumerator EnteringTheLibraryUnloadsOutdoorResidents()
        {
            yield return WaitSeconds(2f);
            _game.Areas.RequestTransition(LibraryInteriorBuilder.AreaId, "entrance");
            float timeout = Time.time + 5f;
            while ((_game.Areas.IsTransitioning || _game.Areas.Current.Id != LibraryInteriorBuilder.AreaId) && Time.time < timeout) yield return null;
            yield return WaitSeconds(1f);
            foreach (var body in _game.Residents.ActiveBodies)
                Assert.AreEqual(LibraryInteriorBuilder.AreaId, body.AreaId, $"{body.Profile.Id} should not have a body outdoors");
        }

        [UnityTest]
        public IEnumerator ResidentRemembersIntroduction()
        {
            yield return WaitSeconds(3f);
            var body = _game.Residents.ActiveBodies.FirstOrDefault(b => b.Profile.Role == NpcRole.Student);
            if (body == null) Assert.Inconclusive("No student nearby");
            _game.Dialogue.Begin(body.Talk, _game.Player);
            Assert.IsTrue(_game.Dialogue.IsActive);
            StringAssert.DoesNotContain("{status}", _game.Dialogue.Runner.CurrentText, "Dialogue variables are filled");
            _game.Dialogue.Choose(0); // "Hi! I'm ..."
            Assert.IsTrue(_game.Session.Relationships.Get(body.Profile.Id).Remembers("introduced"));
            _game.Dialogue.Cancel();
        }

        [Test]
        public void SlotsAreNeverShared()
        {
            var slots = new PlaceSlots();
            slots.AddStand("p", "campus", Vector3.zero, 0f);
            slots.AddStand("p", "campus", Vector3.one, 0f);
            var a = slots.Reserve("p", "a", -1, 0, SlotKind.Stand);
            var b = slots.Reserve("p", "b", -1, 0, SlotKind.Stand);
            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            Assert.AreNotSame(a, b);
            Assert.IsNull(slots.Reserve("p", "c", -1, 0, SlotKind.Stand), "Place is full");
            slots.Release("a");
            Assert.AreSame(a, slots.Reserve("p", "c", -1, 0, SlotKind.Stand));
        }

        [Test]
        public void KioskQueueServesInOrder()
        {
            var slots = new PlaceSlots();
            slots.SetQueue("kiosk", new System.Collections.Generic.List<Vector3> { Vector3.zero, Vector3.back, Vector3.back * 2f }, 0f);
            Assert.IsTrue(slots.JoinQueue("kiosk", "first"));
            Assert.IsTrue(slots.JoinQueue("kiosk", "second"));
            Assert.AreEqual(1, slots.QueuePosition("kiosk", "second", out var point, out _));
            Assert.AreEqual(Vector3.back, point);
            slots.LeaveQueue("kiosk", "first");
            Assert.AreEqual(0, slots.QueuePosition("kiosk", "second", out point, out _));
            Assert.AreEqual(Vector3.zero, point);
        }
    }
}
