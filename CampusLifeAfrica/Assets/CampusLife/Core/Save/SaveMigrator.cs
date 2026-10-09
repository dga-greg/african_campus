using System;
using System.Collections.Generic;
using CampusLife.Core.Serialization;

namespace CampusLife.Core.Save
{
    /// <summary>
    /// Upgrades older save payloads step by step (v1 → v2 → … → current) by
    /// rewriting the raw JSON tree before it is bound to <see cref="SaveGame"/>.
    /// Working on the tree means renamed or removed fields can still be read.
    /// </summary>
    public sealed class SaveMigrator
    {
        readonly Dictionary<int, Action<Dictionary<string, object>>> _steps = new Dictionary<int, Action<Dictionary<string, object>>>();

        public int TargetVersion { get; }

        public SaveMigrator(int targetVersion = SaveGame.CurrentVersion)
        {
            TargetVersion = targetVersion;
            if (targetVersion >= 2)
            {
                // v1 → v2: academics arrived in Phase 3. A v1 player is still in
                // their first semester, which began on day 0.
                Register(1, root =>
                {
                    if (!root.ContainsKey("academic"))
                        root["academic"] = new Dictionary<string, object>
                        {
                            ["year"] = JsonNumber.Of(1),
                            ["semester"] = JsonNumber.Of(1),
                            ["semesterStartDay"] = JsonNumber.Of(0),
                            ["registered"] = false,
                        };
                });
            }
        }

        /// <summary>Registers the step that upgrades <paramref name="fromVersion"/> to fromVersion + 1.</summary>
        public SaveMigrator Register(int fromVersion, Action<Dictionary<string, object>> step)
        {
            if (fromVersion < 0 || fromVersion >= TargetVersion) throw new ArgumentOutOfRangeException(nameof(fromVersion));
            if (_steps.ContainsKey(fromVersion)) throw new ArgumentException($"Migration from v{fromVersion} already registered.");
            _steps.Add(fromVersion, step ?? throw new ArgumentNullException(nameof(step)));
            return this;
        }

        public SaveGame Migrate(string payloadJson, int payloadVersion)
        {
            if (payloadVersion > TargetVersion)
                throw new SaveTooNewException(payloadVersion, TargetVersion);
            var root = FieldJson.ParseTree(payloadJson) as Dictionary<string, object>
                       ?? throw new FormatException("Save payload is not a JSON object.");
            for (int v = payloadVersion; v < TargetVersion; v++)
            {
                if (!_steps.TryGetValue(v, out var step))
                    throw new InvalidOperationException($"No save migration registered from v{v} to v{v + 1}.");
                step(root);
            }
            root["version"] = JsonNumber.Of(TargetVersion);
            return FieldJson.FromTree<SaveGame>(root);
        }
    }

    public sealed class SaveTooNewException : Exception
    {
        public int SaveVersion { get; }
        public int SupportedVersion { get; }

        public SaveTooNewException(int saveVersion, int supportedVersion)
            : base($"Save version {saveVersion} is newer than this game supports ({supportedVersion}). Please update the game.")
        {
            SaveVersion = saveVersion;
            SupportedVersion = supportedVersion;
        }
    }
}
