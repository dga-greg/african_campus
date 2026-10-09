using System;
using System.Collections.Generic;

namespace CampusLife.Core.Social
{
    public enum MemoryKind { Conversation, Promise, Favor, Conflict, SharedActivity, AcademicCollaboration, Business }

    [Serializable]
    public class MemoryEntry
    {
        /// <summary>Stable key for the remembered event, e.g. "met_player" or "promised_notes".</summary>
        public string key;
        public MemoryKind kind;
        public double gameMinute;
    }

    [Serializable]
    public class RelationshipData
    {
        public string npcId;
        /// <summary>How much the NPC likes the player, -100..100.</summary>
        public int affinity;
        /// <summary>How much the NPC trusts the player, -100..100.</summary>
        public int trust;
        public int conversations;
        public double lastInteractionMinute = -1;
        public List<MemoryEntry> memories = new List<MemoryEntry>();
    }

    public sealed class Relationship
    {
        public const int Min = -100;
        public const int Max = 100;

        readonly RelationshipData _data;

        internal Relationship(RelationshipData data) { _data = data; }

        public string NpcId => _data.npcId;
        public int Affinity => _data.affinity;
        public int Trust => _data.trust;
        public int Conversations => _data.conversations;
        public double LastInteractionMinute => _data.lastInteractionMinute;
        public IReadOnlyList<MemoryEntry> Memories => _data.memories;

        public void ChangeAffinity(int delta) => _data.affinity = Clamp(_data.affinity + delta);
        public void ChangeTrust(int delta) => _data.trust = Clamp(_data.trust + delta);

        public void RecordConversation(double gameMinute)
        {
            _data.conversations++;
            _data.lastInteractionMinute = gameMinute;
        }

        public bool Remembers(string key) => FindMemory(key) != null;

        public MemoryEntry FindMemory(string key)
        {
            foreach (var m in _data.memories) if (m.key == key) return m;
            return null;
        }

        /// <summary>Stores a meaningful memory once; repeated events do not stack.</summary>
        public bool Remember(string key, MemoryKind kind, double gameMinute)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Memory key required.", nameof(key));
            if (Remembers(key)) return false;
            _data.memories.Add(new MemoryEntry { key = key, kind = kind, gameMinute = gameMinute });
            _data.lastInteractionMinute = Math.Max(_data.lastInteractionMinute, gameMinute);
            return true;
        }

        static int Clamp(int v) => v < Min ? Min : (v > Max ? Max : v);

        internal RelationshipData Copy()
        {
            var copy = new RelationshipData
            {
                npcId = _data.npcId, affinity = _data.affinity, trust = _data.trust,
                conversations = _data.conversations, lastInteractionMinute = _data.lastInteractionMinute,
            };
            foreach (var m in _data.memories) copy.memories.Add(new MemoryEntry { key = m.key, kind = m.kind, gameMinute = m.gameMinute });
            return copy;
        }
    }

    /// <summary>All player ↔ NPC relationship records.</summary>
    public sealed class RelationshipBook
    {
        readonly Dictionary<string, Relationship> _byNpc = new Dictionary<string, Relationship>(StringComparer.Ordinal);

        public Relationship Get(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) throw new ArgumentException("NPC id required.", nameof(npcId));
            if (!_byNpc.TryGetValue(npcId, out var rel))
            {
                rel = new Relationship(new RelationshipData { npcId = npcId });
                _byNpc.Add(npcId, rel);
            }
            return rel;
        }

        public bool Knows(string npcId) => npcId != null && _byNpc.ContainsKey(npcId);

        public IEnumerable<Relationship> All => _byNpc.Values;

        public List<RelationshipData> ToData()
        {
            var list = new List<RelationshipData>();
            foreach (var r in _byNpc.Values) list.Add(r.Copy());
            list.Sort((a, b) => string.CompareOrdinal(a.npcId, b.npcId));
            return list;
        }

        public static RelationshipBook FromData(IEnumerable<RelationshipData> data)
        {
            var book = new RelationshipBook();
            if (data == null) return book;
            foreach (var d in data)
            {
                if (d == null || string.IsNullOrEmpty(d.npcId) || book._byNpc.ContainsKey(d.npcId)) continue;
                if (d.memories == null) d.memories = new List<MemoryEntry>();
                d.affinity = Math.Max(Relationship.Min, Math.Min(Relationship.Max, d.affinity));
                d.trust = Math.Max(Relationship.Min, Math.Min(Relationship.Max, d.trust));
                book._byNpc.Add(d.npcId, new Relationship(d));
            }
            return book;
        }
    }
}
