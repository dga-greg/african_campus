using System;
using System.Globalization;
using System.Text;
using CampusLife.Core.Serialization;

namespace CampusLife.Core.Save
{
    public enum LoadStatus { Loaded, RecoveredFromBackup, NotFound, Corrupt, TooNew }

    public sealed class LoadResult
    {
        public LoadStatus Status;
        public SaveGame Game;
        public string Error;
        public bool Success => Status == LoadStatus.Loaded || Status == LoadStatus.RecoveredFromBackup;
    }

    /// <summary>
    /// Writes versioned, checksummed saves with a rolling backup and recovers
    /// from the backup when the primary file is damaged.
    /// </summary>
    public sealed class SaveService
    {
        public const string BackupSuffix = "_backup";

        readonly ISaveStorage _storage;
        readonly SaveMigrator _migrator;

        public SaveService(ISaveStorage storage, SaveMigrator migrator = null)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _migrator = migrator ?? new SaveMigrator();
        }

        public bool Exists(string slot) => _storage.Exists(slot) || _storage.Exists(slot + BackupSuffix);

        public void Save(string slot, SaveGame game, DateTime utcNow)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            game.version = SaveGame.CurrentVersion;
            game.savedUtc = utcNow.ToString("o", CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(game.createdUtc)) game.createdUtc = game.savedUtc;

            string payload = FieldJson.Serialize(game);
            var envelope = new SaveEnvelope { version = game.version, checksum = Checksum(payload), payload = payload };
            string text = FieldJson.Serialize(envelope);

            // Keep the previous good save as a backup before replacing it.
            if (_storage.TryRead(slot, out var previous) && TryDecode(previous, out _, out _) == LoadStatus.Loaded)
                _storage.Write(slot + BackupSuffix, previous);
            _storage.Write(slot, text);
        }

        public LoadResult Load(string slot)
        {
            bool hasPrimary = _storage.TryRead(slot, out var primaryText);
            if (hasPrimary)
            {
                var status = TryDecode(primaryText, out var game, out var error);
                if (status == LoadStatus.Loaded) return new LoadResult { Status = LoadStatus.Loaded, Game = game };
                if (status == LoadStatus.TooNew) return new LoadResult { Status = LoadStatus.TooNew, Error = error };
                var fromBackup = LoadBackup(slot);
                if (fromBackup != null) return fromBackup;
                return new LoadResult { Status = LoadStatus.Corrupt, Error = error };
            }
            return LoadBackup(slot) ?? new LoadResult { Status = LoadStatus.NotFound };
        }

        LoadResult LoadBackup(string slot)
        {
            if (!_storage.TryRead(slot + BackupSuffix, out var backupText)) return null;
            var status = TryDecode(backupText, out var game, out var error);
            if (status == LoadStatus.Loaded) return new LoadResult { Status = LoadStatus.RecoveredFromBackup, Game = game };
            return null;
        }

        public void Delete(string slot)
        {
            _storage.Delete(slot);
            _storage.Delete(slot + BackupSuffix);
        }

        public SaveSummary ReadSummary(string slot)
        {
            var result = Load(slot);
            if (!result.Success) return null;
            var g = result.Game;
            long balance = g.wallet?.openingBalancePesewas ?? 0;
            if (g.wallet?.transactions != null && g.wallet.transactions.Count > 0)
                balance = g.wallet.transactions[g.wallet.transactions.Count - 1].balanceAfterPesewas;
            return new SaveSummary
            {
                Slot = slot,
                PlayerName = g.profile?.displayName,
                ProgramId = g.profile?.programId,
                SavedUtc = g.savedUtc,
                ClockMinutes = g.clockMinutes,
                BalancePesewas = balance,
                RecoveredFromBackup = result.Status == LoadStatus.RecoveredFromBackup,
            };
        }

        LoadStatus TryDecode(string text, out SaveGame game, out string error)
        {
            game = null;
            error = null;
            try
            {
                var envelope = FieldJson.Deserialize<SaveEnvelope>(text);
                if (envelope == null || envelope.format != SaveEnvelope.FormatId || envelope.payload == null)
                {
                    error = "Not a Campus Life Africa save file.";
                    return LoadStatus.Corrupt;
                }
                if (Checksum(envelope.payload) != envelope.checksum)
                {
                    error = "Save checksum mismatch (file damaged or edited).";
                    return LoadStatus.Corrupt;
                }
                game = _migrator.Migrate(envelope.payload, envelope.version);
                return LoadStatus.Loaded;
            }
            catch (SaveTooNewException e)
            {
                error = e.Message;
                return LoadStatus.TooNew;
            }
            catch (Exception e) when (e is FormatException || e is InvalidOperationException || e is InvalidCastException)
            {
                error = e.Message;
                return LoadStatus.Corrupt;
            }
        }

        /// <summary>FNV-1a 64-bit over UTF-8 bytes. Detects corruption; it is not a security measure.</summary>
        public static string Checksum(string payload)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            foreach (byte b in Encoding.UTF8.GetBytes(payload))
            {
                hash ^= b;
                hash *= prime;
            }
            return "fnv1a64:" + hash.ToString("x16", CultureInfo.InvariantCulture);
        }
    }
}
