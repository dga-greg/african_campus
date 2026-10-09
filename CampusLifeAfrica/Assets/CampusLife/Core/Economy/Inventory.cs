using System;
using System.Collections.Generic;

namespace CampusLife.Core.Economy
{
    [Serializable]
    public class InventoryEntry
    {
        public string itemId;
        public int count;
    }

    public sealed class Inventory
    {
        readonly Dictionary<string, int> _counts = new Dictionary<string, int>(StringComparer.Ordinal);

        public event Action<Inventory, string> Changed;

        public int Count(string itemId) => itemId != null && _counts.TryGetValue(itemId, out var c) ? c : 0;
        public bool Has(string itemId) => Count(itemId) > 0;
        public IEnumerable<KeyValuePair<string, int>> Items => _counts;

        public void Add(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId)) throw new ArgumentException("Item id required.", nameof(itemId));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            _counts[itemId] = checked(Count(itemId) + count);
            Changed?.Invoke(this, itemId);
        }

        public bool Remove(string itemId, int count = 1)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            int current = Count(itemId);
            if (current < count) return false;
            if (current == count) _counts.Remove(itemId);
            else _counts[itemId] = current - count;
            Changed?.Invoke(this, itemId);
            return true;
        }

        public List<InventoryEntry> ToData()
        {
            var list = new List<InventoryEntry>();
            foreach (var kv in _counts) list.Add(new InventoryEntry { itemId = kv.Key, count = kv.Value });
            list.Sort((a, b) => string.CompareOrdinal(a.itemId, b.itemId));
            return list;
        }

        public static Inventory FromData(IEnumerable<InventoryEntry> entries)
        {
            var inv = new Inventory();
            if (entries == null) return inv;
            foreach (var e in entries)
                if (e != null && !string.IsNullOrEmpty(e.itemId) && e.count > 0) inv.Add(e.itemId, e.count);
            return inv;
        }
    }
}
