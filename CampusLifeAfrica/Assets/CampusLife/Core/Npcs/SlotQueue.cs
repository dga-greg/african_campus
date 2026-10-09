using System;
using System.Collections.Generic;

namespace CampusLife.Core.Npcs
{
    /// <summary>
    /// First-come, first-served queue (e.g. at the kiosk). Members are told
    /// their position; position 0 is being served.
    /// </summary>
    public sealed class SlotQueue
    {
        readonly List<string> _members = new List<string>();

        public int Capacity { get; }
        public int Count => _members.Count;

        public SlotQueue(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
        }

        /// <summary>Joins the back of the queue. Returns false if full; joining twice keeps the original place.</summary>
        public bool Join(string id)
        {
            if (_members.Contains(id)) return true;
            if (_members.Count >= Capacity) return false;
            _members.Add(id);
            return true;
        }

        public void Leave(string id) => _members.Remove(id);

        /// <summary>0 = front of the queue, -1 = not queueing.</summary>
        public int PositionOf(string id) => _members.IndexOf(id);

        public string Front => _members.Count > 0 ? _members[0] : null;
    }
}
