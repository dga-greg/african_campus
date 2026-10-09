using System.Collections.Generic;
using CampusLife.Core.Npcs;
using CampusLife.Runtime.World;
using UnityEngine;

namespace CampusLife.Runtime.Npc
{
    public enum SlotKind { Seat, Stand, GroundSeat, Queue, Route }

    /// <summary>A spot where a resident performs an activity.</summary>
    public sealed class Slot
    {
        public string PlaceId;
        public string AreaId;
        public SlotKind Kind;
        public Vector3 Position;
        public float Yaw;
        public SeatInteractable Seat;
        /// <summary>Standing/ground slots sharing a group face each other (friends cluster).</summary>
        public int Group = -1;
        /// <summary>Route waypoints (looped) for patrols, laps and sweeping.</summary>
        public Vector3[] Route;
        public string ReservedBy;

        public bool Free => ReservedBy == null && (Seat == null || !Seat.Occupied);
    }

    /// <summary>
    /// Registry of places and their slots, filled in by the world builders.
    /// Hands out slots so residents never share a seat, and runs the kiosk queue.
    /// </summary>
    public sealed class PlaceSlots
    {
        readonly Dictionary<string, List<Slot>> _slots = new Dictionary<string, List<Slot>>();
        readonly Dictionary<string, Vector3> _access = new Dictionary<string, Vector3>();
        readonly Dictionary<string, Vector3> _areaExits = new Dictionary<string, Vector3>();
        readonly Dictionary<string, Slot> _byNpc = new Dictionary<string, Slot>();
        readonly Dictionary<string, List<Vector3>> _queuePoints = new Dictionary<string, List<Vector3>>();
        readonly Dictionary<string, float> _queueYaw = new Dictionary<string, float>();
        readonly Dictionary<string, SlotQueue> _queues = new Dictionary<string, SlotQueue>();

        public void SetAccess(string placeId, Vector3 position) => _access[placeId] = position;
        public bool TryGetAccess(string placeId, out Vector3 p) => _access.TryGetValue(placeId, out p);
        public IEnumerable<KeyValuePair<string, Vector3>> AccessPoints => _access;

        /// <summary>Where residents leave an interior area (inside the exit door).</summary>
        public void SetAreaExit(string areaId, Vector3 position) => _areaExits[areaId] = position;
        public bool TryGetAreaExit(string areaId, out Vector3 p) => _areaExits.TryGetValue(areaId, out p);

        public void Add(Slot slot)
        {
            if (!_slots.TryGetValue(slot.PlaceId, out var list)) _slots[slot.PlaceId] = list = new List<Slot>();
            list.Add(slot);
        }

        public void AddSeat(string placeId, string areaId, SeatInteractable seat)
            => Add(new Slot { PlaceId = placeId, AreaId = areaId, Kind = SlotKind.Seat, Seat = seat, Position = seat.StandPose.position, Yaw = seat.transform.eulerAngles.y });

        public void AddStand(string placeId, string areaId, Vector3 position, float yaw, int group = -1, SlotKind kind = SlotKind.Stand)
            => Add(new Slot { PlaceId = placeId, AreaId = areaId, Kind = kind, Position = position, Yaw = yaw, Group = group });

        /// <summary>A circle of standing (or ground-sitting) spots facing a shared centre.</summary>
        public void AddGroup(string placeId, string areaId, Vector3 center, int members, float radius, int group, SlotKind kind = SlotKind.Stand)
        {
            for (int i = 0; i < members; i++)
            {
                float ang = (i / (float)members) * 360f + group * 17f;
                var offset = Quaternion.Euler(0f, ang, 0f) * Vector3.forward * radius;
                AddStand(placeId, areaId, center + offset, ang + 180f, group, kind);
            }
        }

        public void AddRoute(string placeId, string areaId, Vector3[] waypoints, int copies)
        {
            for (int i = 0; i < copies; i++)
                Add(new Slot { PlaceId = placeId, AreaId = areaId, Kind = SlotKind.Route, Route = waypoints, Position = waypoints[(i * 3) % waypoints.Length] });
        }

        public void SetQueue(string placeId, List<Vector3> points, float yaw)
        {
            _queuePoints[placeId] = points;
            _queueYaw[placeId] = yaw;
            _queues[placeId] = new SlotQueue(points.Count);
        }

        public bool HasQueue(string placeId) => _queues.ContainsKey(placeId);

        public bool JoinQueue(string placeId, string npcId) => _queues.TryGetValue(placeId, out var q) && q.Join(npcId);

        public void LeaveQueue(string placeId, string npcId)
        {
            if (_queues.TryGetValue(placeId, out var q)) q.Leave(npcId);
        }

        /// <summary>Queue position (0 = being served) and where to stand, or -1 when not queueing.</summary>
        public int QueuePosition(string placeId, string npcId, out Vector3 point, out float yaw)
        {
            point = default;
            yaw = 0f;
            if (!_queues.TryGetValue(placeId, out var q)) return -1;
            int index = q.PositionOf(npcId);
            if (index < 0) return -1;
            point = _queuePoints[placeId][index];
            yaw = _queueYaw[placeId];
            return index;
        }

        public bool HasSlots(string placeId) => _slots.ContainsKey(placeId);

        /// <summary>
        /// Reserves a free slot for an NPC, preferring their friend group's spot
        /// and the given kinds in order. Returns null if the place is full.
        /// </summary>
        public Slot Reserve(string placeId, string npcId, int friendGroup, int seed, params SlotKind[] preferred)
        {
            Release(npcId);
            if (!_slots.TryGetValue(placeId, out var list) || list.Count == 0) return null;
            Slot pick = null;
            int groups = GroupCount(list);
            foreach (var kind in preferred)
            {
                // Friends gather around the same group spot when one is free.
                if (friendGroup >= 0)
                    foreach (var s in list)
                        if (s.Kind == kind && s.Free && s.Group >= 0 && s.Group == friendGroup % groups) { pick = s; break; }
                if (pick != null) break;
                int count = list.Count;
                for (int i = 0; i < count; i++)
                {
                    var s = list[(i + seed) % count];
                    if (s.Kind == kind && s.Free) { pick = s; break; }
                }
                if (pick != null) break;
            }
            if (pick == null) return null;
            pick.ReservedBy = npcId;
            _byNpc[npcId] = pick;
            return pick;
        }

        static int GroupCount(List<Slot> list)
        {
            int max = -1;
            foreach (var s in list) if (s.Group > max) max = s.Group;
            return Mathf.Max(1, max + 1);
        }

        public void Release(string npcId)
        {
            if (_byNpc.TryGetValue(npcId, out var slot))
            {
                if (slot.ReservedBy == npcId) slot.ReservedBy = null;
                _byNpc.Remove(npcId);
            }
            foreach (var q in _queues.Values) q.Leave(npcId);
        }

        public Slot ReservedFor(string npcId) => _byNpc.TryGetValue(npcId, out var s) ? s : null;

        /// <summary>Centre of a place's slots, used to place NPCs that could not get a slot.</summary>
        public Vector3 Center(string placeId)
        {
            if (!_slots.TryGetValue(placeId, out var list) || list.Count == 0)
                return _access.TryGetValue(placeId, out var a) ? a : Vector3.zero;
            Vector3 sum = Vector3.zero;
            foreach (var s in list) sum += s.Position;
            return sum / list.Count;
        }

        public IEnumerable<Slot> SlotsAt(string placeId) => _slots.TryGetValue(placeId, out var l) ? l : (IEnumerable<Slot>)new Slot[0];
    }
}
