using System;
using System.Collections.Generic;
using System.IO;
using CampusLife.Core.Economy;
using CampusLife.Core.Save;
using CampusLife.Core.Session;
using UnityEngine;

namespace CampusLife.Runtime.Infrastructure
{
    /// <summary>PlayerPrefs storage used on WebGL, where PlayerPrefs is persisted to IndexedDB.</summary>
    public sealed class PlayerPrefsSaveStorage : ISaveStorage
    {
        const string Prefix = "cla.save.";

        public bool TryRead(string key, out string content)
        {
            string k = Prefix + key;
            if (!PlayerPrefs.HasKey(k)) { content = null; return false; }
            content = PlayerPrefs.GetString(k);
            return true;
        }

        public void Write(string key, string content)
        {
            PlayerPrefs.SetString(Prefix + key, content);
            PlayerPrefs.Save();
        }

        public bool Exists(string key) => PlayerPrefs.HasKey(Prefix + key);

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(Prefix + key);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Unity-facing save facade: picks storage per platform and converts sessions.</summary>
    public sealed class SaveSystem
    {
        public const string AutosaveSlot = "autosave";
        public static readonly string[] ManualSlots = { "slot1", "slot2", "slot3" };

        readonly SaveService _service;
        readonly ItemCatalog _items;

        public string StorageDescription { get; }

        /// <summary>When set before the game starts, replaces platform storage (used by PlayMode tests).</summary>
        public static ISaveStorage StorageOverride { get; set; }

        public SaveSystem(ItemCatalog items)
        {
            _items = items;
            ISaveStorage storage;
            if (StorageOverride != null)
            {
                storage = StorageOverride;
                StorageDescription = "Test storage (in memory)";
            }
            else if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                storage = new PlayerPrefsSaveStorage();
                StorageDescription = "Browser storage";
            }
            else
            {
                string dir = Path.Combine(Application.persistentDataPath, "saves");
                storage = new FileSaveStorage(dir);
                StorageDescription = dir;
            }
            _service = new SaveService(storage);
        }

        public static string SlotLabel(string slot)
        {
            if (slot == AutosaveSlot) return "Autosave";
            int i = Array.IndexOf(ManualSlots, slot);
            return i >= 0 ? $"Slot {i + 1}" : slot;
        }

        public IEnumerable<string> AllSlots()
        {
            yield return AutosaveSlot;
            foreach (var s in ManualSlots) yield return s;
        }

        public bool TrySave(string slot, GameSession session, out string message)
        {
            try
            {
                _service.Save(slot, session.ToSave(GameBootstrap.GameVersion), DateTime.UtcNow);
                message = $"Saved to {SlotLabel(slot)}";
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException)
            {
                Debug.LogError($"[Save] Failed to save {slot}: {e}");
                message = "Could not save: " + e.Message;
                return false;
            }
        }

        public bool TryLoad(string slot, out GameSession session, out string message)
        {
            session = null;
            LoadResult result;
            try { result = _service.Load(slot); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                message = "Could not read save: " + e.Message;
                return false;
            }
            switch (result.Status)
            {
                case LoadStatus.NotFound: message = $"{SlotLabel(slot)} is empty."; return false;
                case LoadStatus.TooNew: message = result.Error; return false;
                case LoadStatus.Corrupt: message = $"{SlotLabel(slot)} is damaged and could not be recovered."; return false;
            }
            try
            {
                session = GameSession.FromSave(result.Game, _items);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"[Save] Rejected inconsistent save {slot}: {e.Message}");
                message = $"{SlotLabel(slot)} contains inconsistent data and was not loaded.";
                return false;
            }
            message = result.Status == LoadStatus.RecoveredFromBackup
                ? $"{SlotLabel(slot)} was damaged; restored the previous save."
                : $"Loaded {SlotLabel(slot)}";
            return true;
        }

        public SaveSummary Summary(string slot)
        {
            try { return _service.ReadSummary(slot); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { return null; }
        }

        public void Delete(string slot) => _service.Delete(slot);

        /// <summary>The slot with the most recent save time, or null when there are no saves.</summary>
        public string MostRecentSlot()
        {
            string best = null;
            string bestTime = null;
            foreach (var slot in AllSlots())
            {
                var s = Summary(slot);
                if (s == null) continue;
                // ISO-8601 round-trip strings sort chronologically.
                if (bestTime == null || string.CompareOrdinal(s.SavedUtc, bestTime) > 0)
                {
                    best = slot;
                    bestTime = s.SavedUtc;
                }
            }
            return best;
        }
    }
}
