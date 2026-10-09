using System;
using System.Collections.Generic;
using System.IO;
using CampusLife.Core.Content;
using CampusLife.Core.Player;
using CampusLife.Core.Save;
using CampusLife.Core.Serialization;
using CampusLife.Core.Session;
using CampusLife.Core.Social;
using NUnit.Framework;

namespace CampusLife.Tests
{
    public class FieldJsonTests
    {
        enum Mood { Calm, Excited }

        class Sample
        {
            public string text;
            public long big;
            public double precise;
            public float small;
            public bool flag;
            public Mood mood;
            public int[] numbers;
            public List<Sample> children = new List<Sample>();
        }

        [Test]
        public void RoundTripsSupportedTypes()
        {
            var s = new Sample
            {
                text = "Akwaaba \"GH₵\" \\ line\nbreak\u0001",
                big = long.MaxValue - 7,
                precise = 1234567.890123,
                small = 0.1f,
                flag = true,
                mood = Mood.Excited,
                numbers = new[] { 1, -2, 3 },
                children = { new Sample { text = "child", children = null } },
            };
            var back = FieldJson.Deserialize<Sample>(FieldJson.Serialize(s));
            Assert.AreEqual(s.text, back.text);
            Assert.AreEqual(s.big, back.big);
            Assert.AreEqual(s.precise, back.precise);
            Assert.AreEqual(s.small, back.small);
            Assert.IsTrue(back.flag);
            Assert.AreEqual(Mood.Excited, back.mood);
            CollectionAssert.AreEqual(s.numbers, back.numbers);
            Assert.AreEqual("child", back.children[0].text);
            Assert.IsNull(back.children[0].children);
        }

        [Test]
        public void OutputIsDeterministic()
        {
            var s = new Sample { text = "x", numbers = new[] { 1 } };
            Assert.AreEqual(FieldJson.Serialize(s), FieldJson.Serialize(s));
        }

        [Test]
        public void MissingFieldsKeepDefaults()
        {
            var back = FieldJson.Deserialize<Sample>("{\"text\":\"only\"}");
            Assert.AreEqual("only", back.text);
            Assert.IsNotNull(back.children);
            Assert.AreEqual(0, back.big);
        }

        [Test]
        public void RejectsMalformedJson()
        {
            Assert.Throws<FormatException>(() => FieldJson.Deserialize<Sample>("{\"text\":"));
            Assert.Throws<FormatException>(() => FieldJson.Deserialize<Sample>("{\"big\":\"notanumber\"}"));
            Assert.Throws<FormatException>(() => FieldJson.Deserialize<Sample>("{\"mood\":\"Furious\"}"));
            Assert.Throws<FormatException>(() => FieldJson.Deserialize<Sample>("{} trailing"));
            Assert.Throws<FormatException>(() => FieldJson.Deserialize<Sample>(new string('[', 100)));
        }

        [Test]
        public void RejectsNonFiniteNumbers()
        {
            Assert.Throws<NotSupportedException>(() => FieldJson.Serialize(new Sample { precise = double.NaN }));
        }
    }

    public class SaveServiceTests
    {
        static GameSession NewSession()
        {
            var profile = new PlayerProfileData { displayName = "Abena", pronouns = "she/her", programId = "nursing" };
            return GameSession.NewGame(profile, Phase1Content.CreateItemCatalog(), "save-1");
        }

        static readonly DateTime Now = new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void SaveAndLoadRoundTrip()
        {
            var storage = new MemorySaveStorage();
            var service = new SaveService(storage);
            var session = NewSession();
            session.Flags.Set(Ids.FlagMetEsi);
            session.Relationships.Get(Ids.NpcEsi).Remember("met_player", MemoryKind.Conversation, 420);
            session.Relationships.Get(Ids.NpcEsi).ChangeAffinity(7);
            session.Inventory.Add(Ids.ItemLibraryCard);
            session.Location = new LocationData { areaId = "library_interior", x = 1.5f, y = 0.25f, z = -3f, yaw = 90f, spawnId = null };

            service.Save("slot1", session.ToSave("0.1.0"), Now);
            var result = service.Load("slot1");

            Assert.AreEqual(LoadStatus.Loaded, result.Status);
            var restored = GameSession.FromSave(result.Game, Phase1Content.CreateItemCatalog());
            Assert.AreEqual("Abena", restored.Profile.displayName);
            Assert.AreEqual(session.Wallet.Balance, restored.Wallet.Balance);
            Assert.IsTrue(restored.Flags.Has(Ids.FlagMetEsi));
            Assert.IsTrue(restored.Relationships.Get(Ids.NpcEsi).Remembers("met_player"));
            Assert.AreEqual(7, restored.Relationships.Get(Ids.NpcEsi).Affinity);
            Assert.IsTrue(restored.Inventory.Has(Ids.ItemLibraryCard));
            Assert.AreEqual("library_interior", restored.Location.areaId);
            Assert.AreEqual(-3f, restored.Location.z);
            Assert.AreEqual(session.Clock.TotalMinutes, restored.Clock.TotalMinutes);
        }

        [Test]
        public void CorruptPrimaryRecoversFromBackup()
        {
            var storage = new MemorySaveStorage();
            var service = new SaveService(storage);
            var session = NewSession();
            service.Save("slot1", session.ToSave("0.1.0"), Now);
            session.Flags.Set("second_save");
            service.Save("slot1", session.ToSave("0.1.0"), Now.AddMinutes(5));

            storage.Write("slot1", "{\"format\":\"campus-life-africa-save\",\"payl");
            var result = service.Load("slot1");
            Assert.AreEqual(LoadStatus.RecoveredFromBackup, result.Status);
            Assert.IsFalse(result.Game.flags.Contains("second_save"), "Backup holds the previous good save.");
        }

        [Test]
        public void ChecksumDetectsEditedPayload()
        {
            var storage = new MemorySaveStorage();
            var service = new SaveService(storage);
            service.Save("slot1", NewSession().ToSave("0.1.0"), Now);
            storage.TryRead("slot1", out var text);
            storage.Write("slot1", text.Replace("Abena", "Hacker"));
            var result = service.Load("slot1");
            Assert.AreEqual(LoadStatus.Corrupt, result.Status);
            StringAssert.Contains("checksum", result.Error);
        }

        [Test]
        public void CorruptSaveDoesNotOverwriteGoodBackup()
        {
            var storage = new MemorySaveStorage();
            var service = new SaveService(storage);
            service.Save("slot1", NewSession().ToSave("0.1.0"), Now);
            storage.Write("slot1", "garbage");
            service.Save("slot1", NewSession().ToSave("0.1.0"), Now);
            Assert.IsFalse(storage.TryRead("slot1" + SaveService.BackupSuffix, out var backup) && backup == "garbage");
        }

        [Test]
        public void MissingSlotReportsNotFound()
        {
            var service = new SaveService(new MemorySaveStorage());
            Assert.AreEqual(LoadStatus.NotFound, service.Load("nope").Status);
            Assert.IsNull(service.ReadSummary("nope"));
        }

        [Test]
        public void NewerSaveVersionIsRefused()
        {
            var storage = new MemorySaveStorage();
            var payload = FieldJson.Serialize(new SaveGame { version = 99 });
            var envelope = new SaveEnvelope { version = 99, payload = payload, checksum = SaveService.Checksum(payload) };
            storage.Write("slot1", FieldJson.Serialize(envelope));
            var result = new SaveService(storage).Load("slot1");
            Assert.AreEqual(LoadStatus.TooNew, result.Status);
        }

        [Test]
        public void MigrationsRunStepByStep()
        {
            // Pretend v0 stored the player name at the root; v1 moved it into the profile.
            var migrator = new SaveMigrator(1).Register(0, root =>
            {
                var profile = new Dictionary<string, object> { ["displayName"] = root["playerName"], ["pronouns"] = "he/him", ["programId"] = "economics" };
                root.Remove("playerName");
                root["profile"] = profile;
            });
            var game = migrator.Migrate("{\"version\":0,\"playerName\":\"Kofi\",\"clockMinutes\":480}", 0);
            Assert.AreEqual(1, game.version);
            Assert.AreEqual("Kofi", game.profile.displayName);
            Assert.AreEqual(480, game.clockMinutes);

            Assert.Throws<InvalidOperationException>(() => new SaveMigrator(2).Migrate("{}", 0));
        }

        [Test]
        public void FileStorageWritesAtomicallyAndRejectsBadKeys()
        {
            string dir = Path.Combine(Path.GetTempPath(), "cla-save-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                var storage = new FileSaveStorage(dir);
                var service = new SaveService(storage);
                service.Save("slot1", NewSession().ToSave("0.1.0"), Now);
                service.Save("slot1", NewSession().ToSave("0.1.0"), Now);
                Assert.IsTrue(File.Exists(Path.Combine(dir, "slot1.json")));
                Assert.IsTrue(File.Exists(Path.Combine(dir, "slot1_backup.json")));
                Assert.IsFalse(File.Exists(Path.Combine(dir, "slot1.json.tmp")));
                Assert.AreEqual(LoadStatus.Loaded, service.Load("slot1").Status);
                Assert.Throws<ArgumentException>(() => storage.Write("../escape", "x"));
                service.Delete("slot1");
                Assert.IsFalse(service.Exists("slot1"));
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        [Test]
        public void TransactionIdsStayUniqueAcrossReload()
        {
            var session = NewSession();
            var first = session.NextTransactionId("buy");
            session.Wallet.Debit(first, 50, "purchase", "", 0);
            var restored = GameSession.FromSave(session.ToSave("0.1.0"), Phase1Content.CreateItemCatalog());
            var next = restored.NextTransactionId("buy");
            Assert.AreNotEqual(first, next);

            var damaged = session.ToSave("0.1.0");
            damaged.transactionSequence = 0;
            var fromDamaged = GameSession.FromSave(damaged, Phase1Content.CreateItemCatalog());
            Assert.AreNotEqual(first, fromDamaged.NextTransactionId("buy"));
        }

        [Test]
        public void InvalidSaveDataIsRejectedOnRestore()
        {
            var save = NewSession().ToSave("0.1.0");
            save.profile.displayName = "";
            Assert.Throws<InvalidOperationException>(() => GameSession.FromSave(save, Phase1Content.CreateItemCatalog()));
        }
    }
}
