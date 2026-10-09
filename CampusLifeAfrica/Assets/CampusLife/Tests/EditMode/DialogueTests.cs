using System;
using System.Collections.Generic;
using System.Linq;
using CampusLife.Core.Content;
using CampusLife.Core.Dialogue;
using CampusLife.Core.Player;
using CampusLife.Core.Session;
using NUnit.Framework;

namespace CampusLife.Tests
{
    public class DialogueTests
    {
        GameSession _session;
        Dictionary<string, DialogueGraph> _graphs;

        [SetUp]
        public void SetUp()
        {
            var profile = new PlayerProfileData { displayName = "Kojo", pronouns = "he/him", programId = "computer_science" };
            _session = GameSession.NewGame(profile, Phase1Content.CreateItemCatalog(), "s");
            _graphs = Phase1Content.CreateDialogues().ToDictionary(g => g.Id);
        }

        DialogueRunner Talk(string graphId)
        {
            var g = _graphs[graphId];
            var npc = Phase1Content.Npcs.First(n => n.Id == g.NpcId);
            var runner = new DialogueRunner(g, _session, npc.DisplayName);
            runner.Start();
            return runner;
        }

        static void Pick(DialogueRunner r, string textStart)
        {
            for (int i = 0; i < r.AvailableChoices.Count; i++)
            {
                if (r.AvailableChoices[i].Text.StartsWith(textStart, StringComparison.Ordinal)) { r.Choose(i); return; }
            }
            Assert.Fail($"No available choice starting with '{textStart}' at node '{r.Current?.Id}'. " +
                        $"Options: {string.Join(" | ", r.AvailableChoices.Select(c => c.Text))}");
        }

        [Test]
        public void AllContentGraphsAreValid()
        {
            foreach (var g in _graphs.Values)
                CollectionAssert.IsEmpty(g.Validate(), $"Dialogue {g.Id} has errors");
            foreach (var npc in Phase1Content.Npcs)
                Assert.IsTrue(_graphs.ContainsKey(npc.DialogueId), $"NPC {npc.Id} references missing dialogue {npc.DialogueId}");
        }

        [Test]
        public void EveryRememberedEventHasPlayerFacingText()
        {
            foreach (var g in _graphs.Values)
                foreach (var node in g.Nodes)
                {
                    var effects = new List<DialogueEffect>(node.OnEnter);
                    foreach (var c in node.Choices) effects.AddRange(c.Effects);
                    foreach (var e in effects)
                        if (e.Type == EffectType.Remember)
                            Assert.AreNotEqual(e.Key, Phase1Content.DescribeMemory(e.Key), $"Memory '{e.Key}' in {g.Id} has no description");
                }
        }

        [Test]
        public void ValidatorFindsBrokenLinks()
        {
            var g = new DialogueGraph("broken", "npc", "a")
                .Node(new DialogueNode("a", "Hi", new[] { new DialogueChoice("Go", "missing") }))
                .Node(new DialogueNode("orphan", "Never shown"));
            var errors = g.Validate();
            Assert.IsTrue(errors.Any(e => e.Contains("missing")));
            Assert.IsTrue(errors.Any(e => e.Contains("unreachable")));
        }

        [Test]
        public void TextPlaceholdersAreFilled()
        {
            var r = Talk("esi_orientation");
            StringAssert.Contains("Kojo", r.CurrentText);
            StringAssert.Contains("Computer Science", r.CurrentText);
        }

        [Test]
        public void FirstSemesterJourneyProgressesObjectives()
        {
            var objectives = Phase1Content.CreateObjectives();
            Assert.AreEqual("meet_esi", objectives.Current(_session.Flags).Id);

            var esi = Talk("esi_orientation");
            Pick(esi, "That's me!");
            Assert.IsTrue(_session.Flags.Has(Ids.FlagMetEsi));
            Assert.IsTrue(_session.Relationships.Get(Ids.NpcEsi).Remembers("met_player"));
            Pick(esi, "Any tips");
            Pick(esi, "Noted.");
            Assert.IsTrue(esi.IsFinished);
            Assert.AreEqual(8, _session.Relationships.Get(Ids.NpcEsi).Affinity);
            Assert.AreEqual("library_card", objectives.Current(_session.Flags).Id);

            var again = Talk("esi_orientation");
            Assert.AreEqual("reminder", again.Current.Id);
            again.Choose(0);

            var darko = Talk("darko_library");
            Assert.AreEqual("greet", darko.Current.Id);
            Pick(darko, "I'm a new student");
            Pick(darko, "Is there a fee");
            Pick(darko, "Understood.");
            Assert.IsTrue(_session.Inventory.Has(Ids.ItemLibraryCard));
            Assert.IsTrue(_session.Flags.Has(Ids.FlagGotLibraryCard));
            Assert.IsTrue(_session.Relationships.Get(Ids.NpcDarko).Remembers("registered_player"));
            Pick(darko, "Thank you, Mr. Darko");
            Assert.AreEqual("show_esi", objectives.Current(_session.Flags).Id);

            var darkoAgain = Talk("darko_library");
            Assert.AreEqual("returning", darkoAgain.Current.Id, "Darko remembers the player has a card");
            darkoAgain.Abort();

            var esiCard = Talk("esi_orientation");
            Assert.AreEqual("congratulate", esiCard.Current.Id);
            Assert.AreEqual(1, esiCard.AvailableChoices.Count, "Only the choice matching how the player first met Esi");
            Pick(esiCard, "Thanks for pointing");
            Pick(esiCard, "See you around");
            Assert.IsNull(objectives.Current(_session.Flags));
            Assert.AreEqual("chat", Talk("esi_orientation").Current.Id);
            Assert.AreEqual(4, _session.Relationships.Get(Ids.NpcEsi).Conversations);
        }

        [Test]
        public void DarkoFirstThenEsiDoesNotDeadEnd()
        {
            var darko = Talk("darko_library");
            Pick(darko, "I'm a new student");
            Pick(darko, "Thank you, sir.");
            Pick(darko, "Thank you, Mr. Darko");

            var esi = Talk("esi_orientation");
            Assert.AreEqual("congratulate", esi.Current.Id);
            Assert.AreEqual(1, esi.AvailableChoices.Count);
            StringAssert.Contains("Kojo", esi.FormatText(esi.AvailableChoices[0].Text));
            esi.Choose(0);
            Assert.IsTrue(_session.Flags.Has(Ids.FlagMetEsi));
            Assert.IsNull(Phase1Content.CreateObjectives().Current(_session.Flags));
        }

        [Test]
        public void BriefFirstMeetingChangesLaterReaction()
        {
            var esi = Talk("esi_orientation");
            Pick(esi, "I'll figure it out");
            Pick(esi, "Okay. Thanks.");
            Assert.AreEqual(-3, _session.Relationships.Get(Ids.NpcEsi).Affinity);
            _session.Inventory.Add(Ids.ItemLibraryCard);
            var later = Talk("esi_orientation");
            Pick(later, "You were right");
            Assert.AreEqual("farewell_brief", later.Current.Id);
            Assert.AreEqual(3, _session.Relationships.Get(Ids.NpcEsi).Trust);
        }

        [Test]
        public void KioskPurchaseUsesWalletAndInventory()
        {
            long before = _session.Wallet.Balance;
            var kiosk = Talk("akosua_kiosk");
            Pick(kiosk, "A bottle of malt");
            Assert.AreEqual(before - 800, _session.Wallet.Balance);
            Assert.AreEqual(1, _session.Inventory.Count(Ids.ItemMalt));
            Assert.IsTrue(kiosk.Feedback.Any(f => f.Contains("GH₵8.00")));
            Pick(kiosk, "Something else");
            Pick(kiosk, "A bottle of malt");
            Assert.AreEqual(2, _session.Inventory.Count(Ids.ItemMalt));
            Assert.AreEqual(before - 1600, _session.Wallet.Balance);
            Assert.AreEqual(3, _session.Wallet.Ledger.Count, "Allowance plus two distinct purchases");
        }

        [Test]
        public void UnaffordableItemsAreNotOffered()
        {
            var spend = _session.NextTransactionId("test");
            _session.Wallet.Debit(spend, _session.Wallet.Balance - 100, "test", "", 0); // leave GH₵1.00
            var kiosk = Talk("akosua_kiosk");
            Assert.IsTrue(kiosk.AvailableChoices.Any(c => c.Text.StartsWith("Sachet water")));
            Assert.IsFalse(kiosk.AvailableChoices.Any(c => c.Text.StartsWith("A bottle of malt")));
            Assert.IsFalse(kiosk.AvailableChoices.Any(c => c.Text.StartsWith("One meat pie")));
        }

        [Test]
        public void GreetingIsRememberedOnce()
        {
            var kiosk = Talk("akosua_kiosk");
            Pick(kiosk, "Just saying hello");
            Pick(kiosk, "I will");
            var again = Talk("akosua_kiosk");
            Assert.AreEqual("greet_regular", again.Current.Id);
            Assert.IsFalse(again.AvailableChoices.Any(c => c.Text.StartsWith("Just saying hello")));
            Assert.AreEqual(3, _session.Relationships.Get(Ids.NpcAkosua).Affinity);
        }

        [Test]
        public void InvalidChoiceIndexThrows()
        {
            var r = Talk("darko_library");
            Assert.Throws<ArgumentOutOfRangeException>(() => r.Choose(99));
            r.Abort();
            Assert.IsTrue(r.IsFinished);
            Assert.Throws<InvalidOperationException>(() => r.Choose(0));
        }

        [Test]
        public void NodeWithNoAvailableChoicesCanBeContinuedToEnd()
        {
            var g = new DialogueGraph("t", "npc", "a")
                .Node(new DialogueNode("a", "Locked", new[] { new DialogueChoice("Secret", null, new[] { DialogueCondition.Flag("never") }) }));
            var r = new DialogueRunner(g, _session, "NPC");
            r.Start();
            Assert.IsTrue(r.CanContinue);
            r.Continue();
            Assert.IsTrue(r.IsFinished);
        }
    }
}
