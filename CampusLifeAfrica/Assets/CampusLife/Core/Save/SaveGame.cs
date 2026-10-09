using System;
using System.Collections.Generic;
using CampusLife.Core.Economy;
using CampusLife.Core.Player;
using CampusLife.Core.Social;

namespace CampusLife.Core.Save
{
    [Serializable]
    public class LocationData
    {
        public string areaId = "campus";
        public float x;
        public float y;
        public float z;
        public float yaw;
        /// <summary>When set, the player is restored at this named spawn point instead of x/y/z.</summary>
        public string spawnId = "start";
    }

    /// <summary>
    /// Complete persisted state of one playthrough. Only plain data lives here;
    /// <see cref="Session.GameSession"/> converts to and from live objects.
    /// Bump <see cref="CurrentVersion"/> and register a migration in
    /// <see cref="SaveMigrator"/> whenever the shape changes.
    /// </summary>
    [Serializable]
    public class SaveGame
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string saveId;
        public string createdUtc;
        public string savedUtc;
        public string gameVersion;
        public double playTimeSeconds;
        public double clockMinutes;
        public long transactionSequence;
        public PlayerProfileData profile = new PlayerProfileData();
        public WalletData wallet = new WalletData();
        public List<InventoryEntry> inventory = new List<InventoryEntry>();
        public List<string> flags = new List<string>();
        public List<RelationshipData> relationships = new List<RelationshipData>();
        public LocationData location = new LocationData();
    }

    /// <summary>Wrapper written to storage: format marker, version and checksum around the payload.</summary>
    [Serializable]
    public class SaveEnvelope
    {
        public const string FormatId = "campus-life-africa-save";

        public string format = FormatId;
        public int version;
        public string checksum;
        public string payload;
    }

    /// <summary>Lightweight info for save-slot lists.</summary>
    public sealed class SaveSummary
    {
        public string Slot;
        public string PlayerName;
        public string ProgramId;
        public string SavedUtc;
        public double ClockMinutes;
        public long BalancePesewas;
        public bool RecoveredFromBackup;
    }
}
