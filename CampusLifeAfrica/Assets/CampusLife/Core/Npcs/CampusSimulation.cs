using System;
using System.Collections.Generic;
using CampusLife.Core.Time;

namespace CampusLife.Core.Npcs
{
    public enum NpcMode { AtPlace, Travelling }

    /// <summary>Abstract state of one resident; valid whether or not the NPC currently has a 3D body.</summary>
    public sealed class NpcSimState
    {
        public NpcProfile Profile { get; internal set; }
        public NpcMode Mode { get; internal set; }
        /// <summary>Current place when <see cref="NpcMode.AtPlace"/>, destination when travelling.</summary>
        public string PlaceId { get; internal set; }
        public ActivityKind Activity { get; internal set; }
        public float FromX { get; internal set; }
        public float FromZ { get; internal set; }
        public double DepartMinute { get; internal set; }
        public double ArriveMinute { get; internal set; }
        /// <summary>Minute this state was last changed; lets bodies restart behaviours when it changes.</summary>
        public double ChangedMinute { get; internal set; }

        public string Id => Profile.Id;

        public float Progress(double now)
        {
            if (Mode == NpcMode.AtPlace) return 1f;
            double span = ArriveMinute - DepartMinute;
            if (span <= 0) return 1f;
            return (float)Math.Max(0, Math.Min(1, (now - DepartMinute) / span));
        }
    }

    /// <summary>
    /// Lightweight schedule-driven simulation of every resident ("low detail"
    /// and "unloaded" levels). Each NPC is either at a place or travelling
    /// between access points at walking speed. Fully deterministic: state at a
    /// given time depends only on the roster, schedules and the clock, so it
    /// needs no save data — loading a game re-derives it.
    /// </summary>
    public sealed class CampusSimulation
    {
        /// <summary>Paths are longer than straight lines; this factor approximates walkway routing.</summary>
        public const float RouteFactor = 1.3f;
        /// <summary>Gaps longer than this (sleeping, loading) snap everyone to where they should be.</summary>
        public const double SnapThresholdMinutes = 30;

        readonly List<NpcSimState> _states = new List<NpcSimState>();
        readonly Dictionary<string, NpcSimState> _byId = new Dictionary<string, NpcSimState>(StringComparer.Ordinal);
        readonly Dictionary<string, DailySchedule> _scheduleCache = new Dictionary<string, DailySchedule>(StringComparer.Ordinal);
        readonly Func<NpcProfile, int, DailySchedule> _scheduleProvider;
        double _lastMinute = double.NaN;
        int _cacheDay = -1;

        public PlaceCatalog Places { get; }
        /// <summary>Game minutes that pass per real second; converts walking time into game time.</summary>
        public double MinutesPerRealSecond { get; set; } = GameClock.DefaultMinutesPerRealSecond;
        public IReadOnlyList<NpcSimState> States => _states;

        public event Action<NpcSimState> Departed;
        public event Action<NpcSimState> Arrived;

        public CampusSimulation(IEnumerable<NpcProfile> roster, PlaceCatalog places, Func<NpcProfile, int, DailySchedule> scheduleProvider = null)
        {
            Places = places ?? throw new ArgumentNullException(nameof(places));
            _scheduleProvider = scheduleProvider ?? ScheduleGenerator.Generate;
            foreach (var npc in roster)
            {
                var s = new NpcSimState { Profile = npc, Mode = NpcMode.AtPlace };
                _states.Add(s);
                _byId.Add(npc.Id, s);
            }
        }

        public NpcSimState Get(string id) => _byId.TryGetValue(id ?? "", out var s) ? s : null;

        public DailySchedule ScheduleFor(NpcProfile npc, int day)
        {
            if (day != _cacheDay)
            {
                _scheduleCache.Clear();
                _cacheDay = day;
            }
            if (!_scheduleCache.TryGetValue(npc.Id, out var schedule))
            {
                schedule = _scheduleProvider(npc, day);
                _scheduleCache.Add(npc.Id, schedule);
            }
            return schedule;
        }

        public ScheduleEntry CurrentEntry(NpcProfile npc, double now)
        {
            int day = (int)(now / GameClock.MinutesPerDay);
            int minute = (int)(now - (double)day * GameClock.MinutesPerDay);
            return ScheduleFor(npc, day).EntryAt(minute);
        }

        /// <summary>Puts every NPC where their schedule says they are, already arrived.</summary>
        public void Reset(double now)
        {
            foreach (var s in _states)
            {
                var entry = CurrentEntry(s.Profile, now);
                s.Mode = NpcMode.AtPlace;
                s.PlaceId = entry.PlaceId;
                s.Activity = entry.Activity;
                s.DepartMinute = s.ArriveMinute = now;
                s.ChangedMinute = now;
                var (x, z) = Places.AccessPoint(entry.PlaceId);
                s.FromX = x;
                s.FromZ = z;
            }
            _lastMinute = now;
        }

        public void Advance(double now)
        {
            if (double.IsNaN(_lastMinute) || now < _lastMinute || now - _lastMinute > SnapThresholdMinutes)
            {
                Reset(now);
                return;
            }
            _lastMinute = now;
            foreach (var s in _states)
            {
                var entry = CurrentEntry(s.Profile, now);
                if (s.Mode == NpcMode.Travelling && now >= s.ArriveMinute && entry.PlaceId == s.PlaceId)
                {
                    Arrive(s, entry.Activity, now);
                    continue;
                }
                if (entry.PlaceId != s.PlaceId) Depart(s, entry, now);
                else if (s.Mode == NpcMode.AtPlace && s.Activity != entry.Activity)
                {
                    s.Activity = entry.Activity;
                    s.ChangedMinute = now;
                }
            }
        }

        /// <summary>Called by a 3D body that physically reached its destination before the abstract timer.</summary>
        public void NotifyArrived(string id, double now)
        {
            var s = Get(id);
            if (s == null || s.Mode != NpcMode.Travelling) return;
            Arrive(s, CurrentEntry(s.Profile, now).Activity, now);
        }

        void Depart(NpcSimState s, ScheduleEntry entry, double now)
        {
            var (fx, fz) = PositionAt(s, now);
            var (tx, tz) = Places.AccessPoint(entry.PlaceId);
            s.FromX = fx;
            s.FromZ = fz;
            s.PlaceId = entry.PlaceId;
            s.Activity = entry.Activity;
            s.Mode = NpcMode.Travelling;
            s.DepartMinute = now;
            s.ArriveMinute = now + TravelMinutes(fx, fz, tx, tz, s.Profile.WalkSpeed);
            s.ChangedMinute = now;
            Departed?.Invoke(s);
        }

        void Arrive(NpcSimState s, ActivityKind activity, double now)
        {
            s.Mode = NpcMode.AtPlace;
            s.Activity = activity;
            s.ArriveMinute = Math.Min(s.ArriveMinute, now);
            var (x, z) = Places.AccessPoint(s.PlaceId);
            s.FromX = x;
            s.FromZ = z;
            s.ChangedMinute = now;
            Arrived?.Invoke(s);
        }

        public double TravelMinutes(float fx, float fz, float tx, float tz, float walkSpeed)
        {
            double dx = tx - fx, dz = tz - fz;
            double metres = Math.Sqrt(dx * dx + dz * dz) * RouteFactor;
            double seconds = metres / Math.Max(0.3, walkSpeed);
            return seconds * MinutesPerRealSecond;
        }

        /// <summary>Approximate outdoor position (straight line between access points while travelling).</summary>
        public (float x, float z) PositionAt(NpcSimState s, double now)
        {
            if (s.PlaceId == null) return (s.FromX, s.FromZ);
            var (tx, tz) = Places.AccessPoint(s.PlaceId);
            if (s.Mode == NpcMode.AtPlace) return (tx, tz);
            float p = s.Progress(now);
            return (s.FromX + (tx - s.FromX) * p, s.FromZ + (tz - s.FromZ) * p);
        }

        /// <summary>True when the NPC is out in the open on campus (travelling, or at an outdoor place).</summary>
        public bool IsOutdoors(NpcSimState s)
            => s.Mode == NpcMode.Travelling || Places.Get(s.PlaceId).AreaId == PlaceIds.CampusArea;

        public int CountOutdoors()
        {
            int n = 0;
            foreach (var s in _states) if (IsOutdoors(s)) n++;
            return n;
        }

        public int CountAt(string placeId, bool arrivedOnly = true)
        {
            int n = 0;
            foreach (var s in _states)
                if (s.PlaceId == placeId && (!arrivedOnly || s.Mode == NpcMode.AtPlace)) n++;
            return n;
        }
    }
}
