using System.Collections;
using System.Linq;
using CampusLife.Core.Content;
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
    /// <summary>
    /// End-to-end checks on the real procedurally built campus: movement,
    /// stairs, camera collision, building entry, NPC dialogue, sitting and
    /// save/load. Run from Window → General → Test Runner → PlayMode.
    /// </summary>
    public class CampusSmokeTests
    {
        const string TestSlot = "playmode_test";
        GameBootstrap _game;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameBootstrap.AutoBootstrap = false;
            // Never touch the player's real save files from tests.
            SaveSystem.StorageOverride = new MemorySaveStorage();
            _game = new GameObject("TestGame").AddComponent<GameBootstrap>();
            yield return null;
            _game.BeginCharacterCreation();
            _game.DraftProfile.displayName = "Test Student";
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

        IEnumerator Walk(Vector2 input, float seconds)
        {
            _game.Input.SimulatedMove = input;
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
            _game.Input.SimulatedMove = null;
            yield return null;
        }

        IEnumerator WaitForTransition()
        {
            float timeout = Time.time + 5f;
            yield return null;
            while (_game.Areas.IsTransitioning && Time.time < timeout) yield return null;
            Assert.IsFalse(_game.Areas.IsTransitioning, "Area transition did not finish");
        }

        [UnityTest]
        public IEnumerator NewGameStartsAtTheMainGate()
        {
            Assert.AreEqual(CampusExteriorBuilder.AreaId, _game.Areas.Current.Id);
            Assert.AreEqual(GameFlowState.Playing, _game.State);
            Assert.Less(Vector3.Distance(_game.Player.transform.position, new Vector3(0f, 0f, -83f)), 1.5f);
            Assert.AreEqual(25000, _game.Session.Wallet.Balance, "Starting allowance is GH₵250.00");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerWalksForwardAndStaysGrounded()
        {
            _game.CameraRig.SnapBehindTarget();
            var start = _game.Player.transform.position;
            yield return Walk(new Vector2(0f, 1f), 1.5f);
            var end = _game.Player.transform.position;
            Assert.Greater(end.z - start.z, 1.5f, "Player should walk north");
            Assert.Less(Mathf.Abs(end.y - start.y), 0.3f, "Player should stay on the ground");
        }

        [UnityTest]
        public IEnumerator EnterLibraryClimbStairsAndExit()
        {
            _game.Areas.RequestTransition(LibraryInteriorBuilder.AreaId, "entrance");
            yield return WaitForTransition();
            Assert.AreEqual(LibraryInteriorBuilder.AreaId, _game.Areas.Current.Id);
            Assert.IsTrue(_game.Areas.Current.IsInterior);

            // Foot of the staircase, facing up the stairs (+Z).
            _game.Player.Teleport(LibraryInteriorBuilder.Origin + new Vector3(-10.4f, 0.05f, -4f), 0f);
            _game.CameraRig.SnapBehindTarget();
            yield return Walk(new Vector2(0f, 1f), 4.5f);
            Assert.Greater(_game.Player.transform.position.y, 3f, "Player should reach the mezzanine");

            _game.Areas.RequestTransition(CampusExteriorBuilder.AreaId, "library_front");
            yield return WaitForTransition();
            Assert.AreEqual(CampusExteriorBuilder.AreaId, _game.Areas.Current.Id);
        }

        [UnityTest]
        public IEnumerator TalkToDarkoGivesLibraryCardAndSurvivesSaveLoad()
        {
            _game.Areas.RequestTransition(LibraryInteriorBuilder.AreaId, "entrance");
            yield return WaitForTransition();
            var darko = Object.FindObjectsByType<NpcCharacter>(FindObjectsSortMode.None).First(n => n.Definition.Id == Ids.NpcDarko);
            _game.Dialogue.Begin(darko, _game.Player);
            Assert.IsTrue(_game.Dialogue.IsActive);
            Assert.IsTrue(_game.Session.Clock.IsPaused, "Clock pauses during conversations");
            _game.Dialogue.Choose(0); // register
            _game.Dialogue.Choose(0); // thank you
            _game.Dialogue.Choose(0); // end
            Assert.IsFalse(_game.Dialogue.IsActive);
            Assert.IsTrue(_game.Session.Inventory.Has(Ids.ItemLibraryCard));
            yield return null;

            Assert.IsTrue(_game.SaveTo(TestSlot, out var msg), msg);
            Assert.IsTrue(_game.TryLoad(TestSlot, out msg), msg);
            yield return null;
            Assert.IsTrue(_game.Session.Inventory.Has(Ids.ItemLibraryCard));
            Assert.IsTrue(_game.Session.Relationships.Get(Ids.NpcDarko).Remembers("registered_player"));
            Assert.AreEqual(LibraryInteriorBuilder.AreaId, _game.Areas.Current.Id, "Loaded back inside the library");
        }

        [UnityTest]
        public IEnumerator SitOnBenchAndStandUp()
        {
            var seat = Object.FindObjectsByType<SeatInteractable>(FindObjectsSortMode.None).First(s => s.isActiveAndEnabled);
            _game.Player.SitOn(seat);
            yield return null;
            Assert.IsTrue(_game.Player.IsSeated);
            Assert.IsTrue(seat.Occupied);
            _game.Player.StandUp();
            yield return null;
            Assert.IsFalse(_game.Player.IsSeated);
            Assert.IsFalse(seat.Occupied);
        }

        [UnityTest]
        public IEnumerator CameraStaysInFrontOfWalls()
        {
            // Stand just inside the library's south wall, facing north, so the wall is behind the camera.
            _game.Areas.RequestTransition(LibraryInteriorBuilder.AreaId, "entrance");
            yield return WaitForTransition();
            _game.Player.Teleport(LibraryInteriorBuilder.Origin + new Vector3(5f, 0.05f, -8.2f), 0f);
            _game.CameraRig.SnapBehindTarget();
            yield return null;
            yield return null;
            float wallZ = LibraryInteriorBuilder.Origin.z - 9f + 0.15f;
            Assert.Greater(_game.CameraRig.transform.position.z, wallZ, "Camera must not pass through the wall");
        }

        [UnityTest]
        public IEnumerator BuyingAtTheKioskChargesTheWallet()
        {
            var akosua = Object.FindObjectsByType<NpcCharacter>(FindObjectsSortMode.None).First(n => n.Definition.Id == Ids.NpcAkosua);
            long before = _game.Session.Wallet.Balance;
            _game.Dialogue.Begin(akosua, _game.Player);
            _game.Dialogue.Choose(0); // sachet water
            Assert.AreEqual(before - 50, _game.Session.Wallet.Balance);
            Assert.AreEqual(1, _game.Session.Inventory.Count(Ids.ItemSachetWater));
            _game.Dialogue.Cancel();
            yield return null;
        }
    }
}
