using System.Collections.Generic;
using CampusLife.Core.Dialogue;
using CampusLife.Core.Npcs;
using CampusLife.Core.Player;
using CampusLife.Core.Social;
using static CampusLife.Core.Dialogue.DialogueCondition;

namespace CampusLife.Core.Content
{
    /// <summary>
    /// Phase 2 "living campus" content: a deterministic roster of autonomous
    /// residents, their ambient conversations, and player-facing descriptions
    /// of what they are doing.
    /// </summary>
    public static class Phase2Content
    {
        public const string DialogueStudent = "ambient_student";
        public const string DialogueLecturer = "ambient_lecturer";
        public const string DialogueSecurity = "ambient_security";
        public const string DialogueCleaner = "ambient_cleaner";

        public const int StudentCount = 24;

        static readonly string[] FirstNames =
        {
            "Kwame", "Ama", "Kofi", "Abena", "Yaw", "Adwoa", "Kojo", "Efua", "Kwabena", "Afia", "Kwaku", "Yaa",
            "Selorm", "Elikem", "Dzifa", "Mawuli", "Fatima", "Ibrahim", "Zainab", "Fuseini", "Naa", "Nii", "Dede",
            "Lamisi", "Mensima", "Kekeli", "Senyo", "Asantewaa", "Baba", "Mariam",
        };

        static readonly string[] Surnames =
        {
            "Mensah", "Owusu", "Asante", "Boateng", "Addo", "Quaye", "Tetteh", "Agyeman", "Danso", "Ofori",
            "Amoah", "Osei", "Adjei", "Agbeko", "Dagadu", "Issah", "Abubakar", "Yeboah", "Larbi", "Ansah",
            "Kumah", "Sackey", "Amankwah", "Bonsu",
        };

        static IReadOnlyList<NpcProfile> _roster;

        /// <summary>The same 29 residents in every playthrough (ids are stable for saves and memories).</summary>
        public static IReadOnlyList<NpcProfile> Roster
        {
            get
            {
                if (_roster == null) _roster = BuildRoster();
                return _roster;
            }
        }

        static IReadOnlyList<NpcProfile> BuildRoster()
        {
            var list = new List<NpcProfile>();
            var rng = new DeterministicRandom(0xA3C0_2026UL);
            var usedNames = new HashSet<string>();
            for (int i = 0; i < StudentCount; i++)
            {
                string first, last;
                do
                {
                    first = rng.Pick(FirstNames);
                    last = rng.Pick(Surnames);
                } while (!usedNames.Add(first + last));
                var program = AcademicPrograms.All[i % AcademicPrograms.All.Count];
                string id = $"student_{i:00}";
                var def = new NpcDefinition(id, $"{first} {last}", $"{program.Name} student", DialogueStudent, RandomAppearance(ref rng, i));
                list.Add(new NpcProfile(def, NpcRole.Student, program.Id, i % 6,
                    rng.Range(0f, 1f), rng.Range(0f, 1f), rng.Range(0f, 1f), false, rng.Range(1.15f, 1.45f)));
            }
            list.Add(Staff("lecturer_00", "Dr. Akua Frimpong", "Lecturer, Faculty of Computing", NpcRole.Lecturer, DialogueLecturer,
                new AppearanceData { skinTone = 6, hairStyle = 6, hairColor = 0, topStyle = 3, topColor = 5, bottomColor = 1, shoeColor = 1, accessory = 1, height = 0.98f }, false, 1.2f));
            list.Add(Staff("lecturer_01", "Prof. Emmanuel Tagoe", "Lecturer, School of Business", NpcRole.Lecturer, DialogueLecturer,
                new AppearanceData { skinTone = 5, hairStyle = 0, hairColor = 5, topStyle = 1, topColor = 8, bottomColor = 1, shoeColor = 1, accessory = 1, height = 1.02f }, false, 1.15f));
            list.Add(Staff("security_day", "Mr. Godwin Asamoah", "Campus security (day shift)", NpcRole.Security, DialogueSecurity,
                new AppearanceData { skinTone = 7, hairStyle = 0, hairColor = 0, topStyle = 0, topColor = 1, bottomColor = 1, shoeColor = 1, accessory = 4, height = 1.04f }, false, 1.25f));
            list.Add(Staff("security_night", "Mr. Joseph Nartey", "Campus security (night shift)", NpcRole.Security, DialogueSecurity,
                new AppearanceData { skinTone = 4, hairStyle = 7, hairColor = 0, topStyle = 0, topColor = 1, bottomColor = 1, shoeColor = 1, accessory = 4, height = 1.0f }, true, 1.25f));
            list.Add(Staff("cleaner_00", "Madam Comfort Agyei", "Campus cleaner", NpcRole.Cleaner, DialogueCleaner,
                new AppearanceData { skinTone = 6, hairStyle = 6, hairColor = 1, topStyle = 0, topColor = 3, bottomColor = 4, shoeColor = 8, accessory = 0, height = 0.95f }, false, 1.1f));
            return list;
        }

        static NpcProfile Staff(string id, string name, string role, NpcRole kind, string dialogue, AppearanceData look, bool night, float speed)
            => new NpcProfile(new NpcDefinition(id, name, role, dialogue, look), kind, null, -1, 0.5f, 0.3f, 0.5f, night, speed);

        static AppearanceData RandomAppearance(ref DeterministicRandom rng, int i)
        {
            return new AppearanceData
            {
                skinTone = rng.Range(0, CharacterOptions.SkinTones.Length),
                hairStyle = rng.Range(0, CharacterOptions.HairStyles.Length),
                hairColor = rng.Chance(0.75) ? rng.Range(0, 2) : rng.Range(0, CharacterOptions.HairColors.Length),
                topStyle = rng.Range(0, CharacterOptions.TopStyles.Length),
                topColor = rng.Range(0, CharacterOptions.ClothingColors.Length),
                bottomColor = rng.Range(0, CharacterOptions.ClothingColors.Length),
                shoeColor = rng.Range(0, CharacterOptions.ClothingColors.Length),
                accessory = rng.Range(0, CharacterOptions.Accessories.Length),
                height = rng.Range(0.93f, 1.07f),
            };
        }

        public static NpcProfile FindProfile(string npcId)
        {
            foreach (var p in Roster) if (p.Id == npcId) return p;
            return null;
        }

        /// <summary>Looks up any NPC (story character or resident) by id.</summary>
        public static NpcDefinition FindAnyNpc(string npcId)
            => Phase1Content.FindNpc(npcId) ?? FindProfile(npcId)?.Definition;

        // --------------------------------------------------------- descriptions

        /// <summary>What the NPC would say about where they are going / what they are doing.</summary>
        public static string DescribeIntention(ActivityKind activity, bool travelling)
        {
            switch (activity)
            {
                case ActivityKind.Lecture: return travelling ? "I'm on my way to a lecture at the Lecture Theatre Complex." : "I've just come out of a lecture.";
                case ActivityKind.Study: return travelling ? "Heading to Nkabom Library to study. Exams don't wait!" : "Studying. The library is the quietest place on campus.";
                case ActivityKind.Eat: return travelling ? "Off to Auntie Akosua's kiosk to grab something to eat." : "Just getting some food. Auntie's meat pies are the best.";
                case ActivityKind.Socialize: return travelling ? "Meeting my friends at Akwaaba Plaza." : "Just catching up with friends.";
                case ActivityKind.Exercise: return travelling ? "Football at the sports field — come and watch!" : "Training! Got to stay fit.";
                case ActivityKind.Relax: return travelling ? "Going to relax on the west lawn for a bit." : "Just taking a breather.";
                case ActivityKind.WaitForShuttle: return "Waiting for the shuttle into town.";
                case ActivityKind.OffCampus: return "Catching the shuttle into town.";
                case ActivityKind.Sleep: return travelling ? "Heading back to Unity Hall. Long day!" : "Resting.";
                case ActivityKind.Patrol: return "Doing my rounds of the campus.";
                case ActivityKind.Clean: return "Keeping the walkways clean for everyone.";
                case ActivityKind.Work: return "On duty at the security booth.";
                default: return "Just going about my day.";
            }
        }

        static readonly string[] News =
        {
            "They say the football team is looking for new players this semester.",
            "Course registration opens soon — check the notice boards at the Lecture Theatre Complex.",
            "Someone said the shuttle comes every half hour on weekends.",
            "The library stays open until ten at night. Perfect for night owls.",
            "There's talk of a talent show later in the semester.",
            "Auntie Akosua gives the friendliest greetings on campus. Always greet her!",
        };

        public static string NewsFor(string npcId, int day)
        {
            var rng = DeterministicRandom.From(npcId, day * 31 + 7);
            return rng.Pick(News);
        }

        static readonly Dictionary<string, string> MemoryText = new Dictionary<string, string>
        {
            ["introduced"] = "You introduced yourselves.",
            ["offered_company"] = "You offered to walk with them some time.",
            ["wished_luck"] = "You wished them luck.",
            ["thanked_staff"] = "You thanked them for their work on campus.",
            ["bumped_into"] = "You bumped into them while running.",
        };

        public static string DescribeMemory(string key)
            => key != null && MemoryText.TryGetValue(key, out var text) ? text : Phase1Content.DescribeMemory(key);

        // ------------------------------------------------------------ dialogue

        static DialogueChoice C(string text, string next, DialogueCondition[] conditions = null, params DialogueEffect[] effects)
            => new DialogueChoice(text, next, conditions, effects);

        public static IReadOnlyList<DialogueGraph> CreateDialogues() => new[] { Student(), Lecturer(), Security(), Cleaner() };

        /// <summary>
        /// Ambient dialogue is shared by every student; the runner fills in
        /// {first} (their first name), {their_program}, {status} (what they are
        /// doing right now) and {news}. The graph uses the conversation partner's
        /// own relationship record, so each student remembers the player individually.
        /// </summary>
        static DialogueGraph Student()
        {
            return new DialogueGraph(DialogueStudent, null, "intro")
                .Entry(new DialogueEntry("again", Remembers("introduced")))
                .Entry(new DialogueEntry("bumped", Remembers("bumped_into"), Forgot("introduced")))
                .Node(new DialogueNode("intro", "Oh, hi! I don't think we've met. {status}",
                    new[]
                    {
                        C("Hi! I'm {player}, first year, {program}.", "introduced", null,
                            DialogueEffect.Remember("introduced"), DialogueEffect.Affinity(4)),
                        C("Where are you heading?", "status_only"),
                        C("Sorry, I thought you were someone else.", null),
                    }))
                .Node(new DialogueNode("bumped", "You again! You nearly knocked me over earlier. {status}",
                    new[]
                    {
                        C("Sorry about that. I'm {player}, by the way.", "introduced", null,
                            DialogueEffect.Remember("introduced"), DialogueEffect.Affinity(3)),
                        C("You should watch where you're going.", null, null, DialogueEffect.Affinity(-4)),
                    }))
                .Node(new DialogueNode("introduced", "Nice to meet you, {player}! I'm {first}, {their_program}. Welcome to AMU.",
                    new[]
                    {
                        C("Where are you heading now?", "status_only"),
                        C("Maybe I'll walk with you some time.", "goodbye", null,
                            DialogueEffect.Remember("offered_company", MemoryKind.SharedActivity), DialogueEffect.Affinity(2)),
                        C("See you around, {first}.", null),
                    }))
                .Node(new DialogueNode("status_only", "{status}",
                    new[]
                    {
                        C("Good luck with that!", "goodbye", new[] { Forgot("wished_luck") }, DialogueEffect.Remember("wished_luck"), DialogueEffect.Affinity(1)),
                        C("Any campus news?", "news"),
                        C("Okay, see you.", null),
                    }))
                .Node(new DialogueNode("again", "Hey {player}! {status}",
                    new[]
                    {
                        C("Any campus news?", "news"),
                        C("We should hang out some time.", "goodbye", new[] { Forgot("offered_company") },
                            DialogueEffect.Remember("offered_company", MemoryKind.SharedActivity), DialogueEffect.Affinity(2)),
                        C("Catch you later, {first}.", null),
                    }))
                .Node(new DialogueNode("news", "{news}", new[] { C("Thanks for the tip!", "goodbye"), C("Interesting. Bye!", null) }))
                .Node(new DialogueNode("goodbye", "See you around, {player}!", new[] { C("Bye!", null) }));
        }

        static DialogueGraph Lecturer()
        {
            return new DialogueGraph(DialogueLecturer, null, "greet")
                .Entry(new DialogueEntry("again", Remembers("introduced")))
                .Node(new DialogueNode("greet", "Good day. I'm {npc}. {status}",
                    new[]
                    {
                        C("Good day! I'm {player}, a first-year {program} student.", "welcome", null,
                            DialogueEffect.Remember("introduced"), DialogueEffect.Affinity(3), DialogueEffect.Trust(2)),
                        C("Sorry to bother you.", null),
                    }))
                .Node(new DialogueNode("welcome", "Welcome to AMU, {player}. My advice: attend every lecture, ask questions, and start assignments early. Lectures begin after course registration.",
                    new[] { C("Thank you for the advice.", null, null, DialogueEffect.Affinity(1)) }))
                .Node(new DialogueNode("again", "Ah, {player}. How is orientation week treating you? {status}",
                    new[] { C("Very well, thank you.", null), C("Still finding my way around.", "tip") }))
                .Node(new DialogueNode("tip", "The campus map helps. And the library staff are very helpful — introduce yourself to Mr. Darko.",
                    new[] { C("I will. Thank you.", null) }));
        }

        static DialogueGraph Security()
        {
            return new DialogueGraph(DialogueSecurity, null, "greet")
                .Entry(new DialogueEntry("again", Remembers("thanked_staff")))
                .Node(new DialogueNode("greet", "Good day. I'm {npc}, campus security. {status}",
                    new[]
                    {
                        C("Thank you for keeping us safe.", "thanks", null, DialogueEffect.Remember("thanked_staff"), DialogueEffect.Affinity(4), DialogueEffect.Trust(3)),
                        C("Is the campus safe at night?", "night"),
                        C("Good day.", null),
                    }))
                .Node(new DialogueNode("thanks", "It's my pleasure. If you ever need help, find me at the booth by the main gate.", new[] { C("I'll remember that.", null) }))
                .Node(new DialogueNode("night", "We patrol day and night, and the walkways are well lit after dark. Still, walk with friends when you can.", new[] { C("Good to know.", null) }))
                .Node(new DialogueNode("again", "Ah, {player}! All good today? {status}", new[] { C("All good, thanks.", null), C("Is the campus safe at night?", "night") }));
        }

        static DialogueGraph Cleaner()
        {
            return new DialogueGraph(DialogueCleaner, null, "greet")
                .Entry(new DialogueEntry("again", Remembers("thanked_staff")))
                .Node(new DialogueNode("greet", "Good morning, my child. {status}",
                    new[]
                    {
                        C("Thank you, the campus looks lovely.", "thanks", null, DialogueEffect.Remember("thanked_staff"), DialogueEffect.Affinity(5)),
                        C("Good morning, Madam.", null, null, DialogueEffect.Affinity(1)),
                    }))
                .Node(new DialogueNode("thanks", "Eii, thank you! Not many students stop to say that. Use the bins and we'll keep it lovely together.", new[] { C("I will.", null) }))
                .Node(new DialogueNode("again", "Ah, my polite student! {status}", new[] { C("Have a good day, Madam.", null) }));
        }
    }
}
