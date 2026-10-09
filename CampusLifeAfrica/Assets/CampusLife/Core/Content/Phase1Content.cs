using System.Collections.Generic;
using CampusLife.Core.Dialogue;
using CampusLife.Core.Economy;
using CampusLife.Core.Narrative;
using CampusLife.Core.Player;
using CampusLife.Core.Social;
using static CampusLife.Core.Dialogue.DialogueCondition;

namespace CampusLife.Core.Content
{
    public sealed class NpcDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Role { get; }
        public string DialogueId { get; }
        public AppearanceData Appearance { get; }

        public NpcDefinition(string id, string displayName, string role, string dialogueId, AppearanceData appearance)
        {
            Id = id; DisplayName = displayName; Role = role; DialogueId = dialogueId; Appearance = appearance;
        }
    }

    /// <summary>Well-known ids shared by content, gameplay and tests.</summary>
    public static class Ids
    {
        public const string NpcEsi = "esi_mensah";
        public const string NpcDarko = "kwabena_darko";
        public const string NpcAkosua = "auntie_akosua";

        public const string FlagMetEsi = "met_esi";
        public const string FlagGotLibraryCard = "got_library_card";
        public const string FlagEsiCongratulated = "esi_congratulated";

        public const string ItemLibraryCard = "library_card";
        public const string ItemSachetWater = "sachet_water";
        public const string ItemMalt = "malt_drink";
        public const string ItemMeatPie = "meat_pie";

        public const string LocationGate = "gate";
        public const string LocationLibraryEntrance = "library_entrance";
        public const string LocationLibraryDesk = "library_desk";
        public const string LocationKiosk = "kiosk";
    }

    /// <summary>
    /// Content for the first playable milestone: arrival at AMU, meeting the
    /// orientation volunteer, registering at Nkabom Library and the campus kiosk.
    /// </summary>
    public static class Phase1Content
    {
        public static ItemCatalog CreateItemCatalog()
        {
            return new ItemCatalog()
                .Add(new ItemDefinition(Ids.ItemLibraryCard, "AMU library card", "Lets you borrow books and use the reading rooms.", 0))
                .Add(new ItemDefinition(Ids.ItemSachetWater, "Sachet water", "Ice-cold pure water.", 50))
                .Add(new ItemDefinition(Ids.ItemMalt, "Malt drink", "A chilled bottle of malt.", Money.FromCedis(8)))
                .Add(new ItemDefinition(Ids.ItemMeatPie, "Meat pie", "Flaky pastry, freshly baked this morning.", Money.FromCedis(10)));
        }

        public static IReadOnlyList<NpcDefinition> Npcs { get; } = new[]
        {
            new NpcDefinition(Ids.NpcEsi, "Esi Mensah", "Orientation volunteer", "esi_orientation",
                new AppearanceData { skinTone = 5, hairStyle = 3, hairColor = 0, topStyle = 0, topColor = 4, bottomColor = 1, shoeColor = 0, accessory = 3, height = 0.97f }),
            new NpcDefinition(Ids.NpcDarko, "Mr. Kwabena Darko", "Library assistant", "darko_library",
                new AppearanceData { skinTone = 6, hairStyle = 0, hairColor = 5, topStyle = 1, topColor = 0, bottomColor = 8, shoeColor = 1, accessory = 1, height = 1.03f }),
            new NpcDefinition(Ids.NpcAkosua, "Auntie Akosua", "Kiosk owner", "akosua_kiosk",
                new AppearanceData { skinTone = 4, hairStyle = 6, hairColor = 1, topStyle = 3, topColor = 7, bottomColor = 7, shoeColor = 8, accessory = 2, height = 0.95f }),
        };

        static readonly Dictionary<string, string> MemoryText = new Dictionary<string, string>
        {
            ["met_player"] = "Met you at the main gate during orientation.",
            ["met_player_brief"] = "Met you briefly at the gate — you wanted to find your own way.",
            ["asked_for_advice"] = "You asked for advice about first year.",
            ["saw_library_card"] = "Celebrated your new library card with you.",
            ["registered_player"] = "Registered your library card.",
            ["greeted"] = "You stopped by just to say hello.",
        };

        /// <summary>Player-facing description of a remembered event.</summary>
        public static string DescribeMemory(string key)
            => key != null && MemoryText.TryGetValue(key, out var text) ? text : key;

        public static NpcDefinition FindNpc(string npcId)
        {
            foreach (var n in Npcs) if (n.Id == npcId) return n;
            return null;
        }

        public static ObjectiveTracker CreateObjectives()
        {
            return new ObjectiveTracker(new[]
            {
                new ObjectiveDefinition("meet_esi", "Talk to Esi, the orientation volunteer by the main gate", Ids.FlagMetEsi, Ids.LocationGate),
                new ObjectiveDefinition("library_card", "Register for your library card at Nkabom Library", Ids.FlagGotLibraryCard, Ids.LocationLibraryDesk, Ids.FlagMetEsi),
                new ObjectiveDefinition("show_esi", "Show Esi your new library card", Ids.FlagEsiCongratulated, Ids.LocationGate, Ids.FlagGotLibraryCard),
            });
        }

        public static IReadOnlyList<DialogueGraph> CreateDialogues() => new[] { Esi(), Darko(), Akosua() };

        static DialogueChoice C(string text, string next, DialogueCondition[] conditions = null, params DialogueEffect[] effects)
            => new DialogueChoice(text, next, conditions, effects);

        static DialogueGraph Esi()
        {
            return new DialogueGraph("esi_orientation", Ids.NpcEsi, "intro")
                .Entry(new DialogueEntry("congratulate", Item(Ids.ItemLibraryCard), NoFlag(Ids.FlagEsiCongratulated)))
                .Entry(new DialogueEntry("chat", Flag(Ids.FlagEsiCongratulated)))
                .Entry(new DialogueEntry("reminder", Flag(Ids.FlagMetEsi)))
                .Node(new DialogueNode("intro",
                    "Akwaaba! Welcome to Akwaaba Metropolitan University. I'm Esi, one of the orientation volunteers. You must be {player} — {program}, right?",
                    new[]
                    {
                        C("That's me! The campus is bigger than I expected.", "tip", null, DialogueEffect.Affinity(5)),
                        C("Yes. Where should I start?", "tip"),
                        C("I'll figure it out on my own, thanks.", "solo", null, DialogueEffect.Affinity(-3)),
                    }))
                .Node(new DialogueNode("tip",
                    "First stop: Nkabom Library — follow the main walkway north, past the gardens. Mr. Darko at the front desk will register your library card. You'll need it for everything this semester.",
                    new[]
                    {
                        C("I'll head there now. Thanks, Esi!", null, null, DialogueEffect.Affinity(2)),
                        C("Any tips for surviving first year?", "advice"),
                    },
                    new[] { DialogueEffect.SetFlag(Ids.FlagMetEsi), DialogueEffect.Remember("met_player") }))
                .Node(new DialogueNode("advice",
                    "Get to know the library staff, keep track of every cedi you spend, and never skip the first week of lectures. Oh — and Auntie Akosua's kiosk by the shuttle stop has the coldest sachet water on campus.",
                    new[] { C("Noted. See you around!", null, null, DialogueEffect.Affinity(3), DialogueEffect.Remember("asked_for_advice")) }))
                .Node(new DialogueNode("solo",
                    "Fair enough! If you get lost, the library is straight up the main walkway. Get your card at the front desk.",
                    new[] { C("Okay. Thanks.", null) },
                    new[] { DialogueEffect.SetFlag(Ids.FlagMetEsi), DialogueEffect.Remember("met_player_brief") }))
                .Node(new DialogueNode("reminder",
                    "Found the library yet? Straight up the main walkway, past the gardens. The front desk is just inside the entrance.",
                    new[] { C("On my way.", null) }))
                .Node(new DialogueNode("congratulate",
                    "Is that your library card? You're officially one of us now, {player}!",
                    new[]
                    {
                        C("Thanks for pointing me in the right direction.", "farewell", new[] { Remembers("met_player") }, DialogueEffect.Affinity(4)),
                        C("You were right. It wasn't hard to find.", "farewell_brief", new[] { Remembers("met_player_brief") }, DialogueEffect.Affinity(6), DialogueEffect.Trust(3)),
                        C("Hi! I'm {player}. I found the library on my own.", "farewell",
                            new[] { Forgot("met_player"), Forgot("met_player_brief") },
                            DialogueEffect.SetFlag(Ids.FlagMetEsi), DialogueEffect.Remember("met_player"), DialogueEffect.Affinity(2)),
                    },
                    new[] { DialogueEffect.SetFlag(Ids.FlagEsiCongratulated), DialogueEffect.Remember("saw_library_card", MemoryKind.SharedActivity) }))
                .Node(new DialogueNode("farewell",
                    "Any time! Lectures start properly next week. Until then, explore — the campus is yours.",
                    new[] { C("See you around, Esi.", null) }))
                .Node(new DialogueNode("farewell_brief",
                    "Ha! Independent — I like that. Still, if you ever need anything, you know where to find me.",
                    new[] { C("I'll remember that.", null) }))
                .Node(new DialogueNode("chat",
                    "Settling in okay, {player}? The campus gets really lively once lectures begin.",
                    new[] { C("Getting there. Thanks, Esi.", null) }));
        }

        static DialogueGraph Darko()
        {
            var issueEffects = new[]
            {
                DialogueEffect.Give(Ids.ItemLibraryCard),
                DialogueEffect.SetFlag(Ids.FlagGotLibraryCard),
                DialogueEffect.Remember("registered_player", MemoryKind.Favor),
                DialogueEffect.Affinity(3),
            };
            return new DialogueGraph("darko_library", Ids.NpcDarko, "greet")
                .Entry(new DialogueEntry("returning", Item(Ids.ItemLibraryCard)))
                .Node(new DialogueNode("greet",
                    "Good day, and welcome to Nkabom Library. How can I help you?",
                    new[]
                    {
                        C("I'm a new student. I'd like to register for a library card.", "register"),
                        C("Just looking around for now.", "browse"),
                    }))
                .Node(new DialogueNode("browse",
                    "Take your time. The reading tables are open to everyone, and the mezzanine upstairs is for quiet study. When you're ready, I can register your card.",
                    new[]
                    {
                        C("Actually, I'd like to register now.", "register"),
                        C("Maybe later.", null),
                    }))
                .Node(new DialogueNode("register",
                    "Wonderful. Let me find you on the admissions list... {player}, {program}. Everything is in order.",
                    new[]
                    {
                        C("Thank you, sir.", "issued", null, issueEffects),
                        C("Is there a fee for the card?", "fee"),
                    }))
                .Node(new DialogueNode("fee",
                    "Your first card is covered by your registration fees. A replacement costs GH₵20, so take good care of it.",
                    new[] { C("Understood. I'll take it now, please.", "issued", null, issueEffects) }))
                .Node(new DialogueNode("issued",
                    "Here you are. The reading rooms are open until ten at night. Welcome to AMU.",
                    new[] { C("Thank you, Mr. Darko!", null) }))
                .Node(new DialogueNode("returning",
                    "Ah, {player}! Good to see you again. The reading tables are free if you'd like to study.",
                    new[]
                    {
                        C("What are the opening hours again?", "hours"),
                        C("Thanks, Mr. Darko.", null),
                    }))
                .Node(new DialogueNode("hours",
                    "Seven in the morning until ten at night on weekdays, and until six on weekends.",
                    new[] { C("Got it. Thank you.", null) }));
        }

        static DialogueGraph Akosua()
        {
            long water = 50, malt = Money.FromCedis(8), pie = Money.FromCedis(10);
            var menu = new[]
            {
                C("Sachet water, please. (GH₵0.50)", "thanks", new[] { Afford(water) }, DialogueEffect.Buy(Ids.ItemSachetWater)),
                C("A bottle of malt, please. (GH₵8.00)", "thanks", new[] { Afford(malt) }, DialogueEffect.Buy(Ids.ItemMalt)),
                C("One meat pie, please. (GH₵10.00)", "thanks", new[] { Afford(pie) }, DialogueEffect.Buy(Ids.ItemMeatPie)),
                C("Just saying hello, Auntie.", "hello", new[] { Forgot("greeted") }, DialogueEffect.Affinity(3), DialogueEffect.Remember("greeted")),
                C("Nothing for now, thank you.", null),
            };
            return new DialogueGraph("akosua_kiosk", Ids.NpcAkosua, "greet")
                .Entry(new DialogueEntry("greet_regular", Remembers("greeted")))
                .Node(new DialogueNode("greet",
                    "Akwaaba, my dear! Cold sachet water, malt, fresh meat pie — what will you take?", menu))
                .Node(new DialogueNode("greet_regular",
                    "Ah, my student, you're back! What can I get you today?", menu))
                .Node(new DialogueNode("hello",
                    "Eii, such good manners! Welcome to AMU. Study hard, eat well, and come and greet me any time.",
                    new[] { C("I will, Auntie.", null) }))
                .Node(new DialogueNode("thanks",
                    "Here you go. Medaase — thank you!",
                    new[]
                    {
                        C("Something else, please.", "greet_regular"),
                        C("Bye, Auntie!", null),
                    }));
        }
    }
}
