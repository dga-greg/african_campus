using System;
using System.Globalization;
using CampusLife.Core.Economy;
using CampusLife.Core.Narrative;
using CampusLife.Core.Player;
using CampusLife.Core.Save;
using CampusLife.Core.Social;
using CampusLife.Core.Time;

namespace CampusLife.Core.Session
{
    /// <summary>
    /// Live state of one playthrough: the single source of truth that gameplay
    /// systems read and modify, and that converts to and from <see cref="SaveGame"/>.
    /// </summary>
    public sealed class GameSession
    {
        public const long StartingAllowancePesewas = 250 * Money.PesewasPerCedi;
        public const double StartMinutes = 7 * GameClock.MinutesPerHour; // Day 1, Monday 07:00

        long _transactionSequence;

        public string SaveId { get; private set; }
        public string CreatedUtc { get; private set; }
        public PlayerProfileData Profile { get; private set; }
        public GameClock Clock { get; private set; }
        public Wallet Wallet { get; private set; }
        public Inventory Inventory { get; private set; }
        public NarrativeFlags Flags { get; private set; }
        public RelationshipBook Relationships { get; private set; }
        public ItemCatalog Items { get; }
        public LocationData Location { get; set; } = new LocationData();
        public double PlayTimeSeconds { get; set; }

        GameSession(ItemCatalog items)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public static GameSession NewGame(PlayerProfileData profile, ItemCatalog items, string saveId)
        {
            var errors = CharacterOptions.Validate(profile);
            if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors), nameof(profile));
            profile.displayName = profile.displayName.Trim();
            profile.appearance = CharacterOptions.Sanitize(profile.appearance);

            var session = new GameSession(items)
            {
                SaveId = saveId,
                Profile = profile,
                Clock = new GameClock(StartMinutes),
                Wallet = new Wallet(0),
                Inventory = new Inventory(),
                Flags = new NarrativeFlags(),
                Relationships = new RelationshipBook(),
            };
            session.Wallet.Credit("allowance:y1s1:arrival", StartingAllowancePesewas, "allowance",
                "Family allowance for the first weeks of semester", session.Clock.TotalMinutes);
            return session;
        }

        /// <summary>
        /// Generates a unique, persistent transaction id. The counter is saved,
        /// so ids never repeat across save/load cycles.
        /// </summary>
        public string NextTransactionId(string prefix)
        {
            _transactionSequence++;
            return prefix + ":" + _transactionSequence.ToString(CultureInfo.InvariantCulture);
        }

        public SaveGame ToSave(string gameVersion)
        {
            return new SaveGame
            {
                saveId = SaveId,
                createdUtc = CreatedUtc,
                gameVersion = gameVersion,
                playTimeSeconds = PlayTimeSeconds,
                clockMinutes = Clock.TotalMinutes,
                transactionSequence = _transactionSequence,
                profile = Profile,
                wallet = Wallet.ToData(),
                inventory = Inventory.ToData(),
                flags = Flags.ToData(),
                relationships = Relationships.ToData(),
                location = new LocationData
                {
                    areaId = Location.areaId, x = Location.x, y = Location.y, z = Location.z,
                    yaw = Location.yaw, spawnId = Location.spawnId,
                },
            };
        }

        /// <summary>Rebuilds a session, validating ledger and profile. Throws on inconsistent data.</summary>
        public static GameSession FromSave(SaveGame save, ItemCatalog items)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (save.clockMinutes < 0 || double.IsNaN(save.clockMinutes)) throw new InvalidOperationException("Save has an invalid clock value.");
            var profile = save.profile ?? throw new InvalidOperationException("Save has no player profile.");
            var errors = CharacterOptions.Validate(profile);
            if (errors.Count > 0) throw new InvalidOperationException("Save profile is invalid: " + string.Join(" ", errors));
            profile.appearance = CharacterOptions.Sanitize(profile.appearance);

            var session = new GameSession(items)
            {
                SaveId = save.saveId,
                CreatedUtc = save.createdUtc,
                Profile = profile,
                Clock = new GameClock(save.clockMinutes),
                Wallet = Wallet.FromData(save.wallet ?? new WalletData()),
                Inventory = Inventory.FromData(save.inventory),
                Flags = NarrativeFlags.FromData(save.flags),
                Relationships = RelationshipBook.FromData(save.relationships),
                Location = save.location ?? new LocationData(),
                PlayTimeSeconds = Math.Max(0, save.playTimeSeconds),
                _transactionSequence = Math.Max(0, save.transactionSequence),
            };
            // Never hand out an id that already exists in the ledger, even if the counter was damaged.
            foreach (var t in session.Wallet.Ledger)
            {
                int colon = t.id.LastIndexOf(':');
                if (colon >= 0 && long.TryParse(t.id.Substring(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out long n))
                    session._transactionSequence = Math.Max(session._transactionSequence, n);
            }
            return session;
        }
    }
}
