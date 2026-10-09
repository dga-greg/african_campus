using System;
using System.Collections.Generic;
using CampusLife.Core.Content;

namespace CampusLife.Core.Npcs
{
    public enum NpcRole { Student, Lecturer, Security, Cleaner }

    public enum ActivityKind
    {
        Sleep, OffCampus, Eat, Lecture, Study, Socialize, Exercise, Patrol, Clean, Relax, WaitForShuttle, Work,
    }

    /// <summary>An autonomous campus resident: identity plus traits that shape their schedule.</summary>
    public sealed class NpcProfile
    {
        public NpcDefinition Definition { get; }
        public NpcRole Role { get; }
        public string ProgramId { get; }
        /// <summary>Friends share a group id and prefer to hang out together.</summary>
        public int FriendGroup { get; }
        public float Studious { get; }
        public float Sporty { get; }
        public float Social { get; }
        /// <summary>Night-shift security guards follow an inverted schedule.</summary>
        public bool NightShift { get; }
        /// <summary>Walking speed in metres per second.</summary>
        public float WalkSpeed { get; }

        public string Id => Definition.Id;

        public NpcProfile(NpcDefinition definition, NpcRole role, string programId, int friendGroup,
            float studious, float sporty, float social, bool nightShift, float walkSpeed)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Role = role; ProgramId = programId; FriendGroup = friendGroup;
            Studious = studious; Sporty = sporty; Social = social; NightShift = nightShift; WalkSpeed = walkSpeed;
        }

        public bool IsFriendOf(NpcProfile other) => other != null && other != this && Role == NpcRole.Student
            && other.Role == NpcRole.Student && FriendGroup == other.FriendGroup;
    }

    /// <summary>
    /// A destination in the simulation. Every place has an access point on the
    /// outdoor campus (a door, a bench area…). Hidden places (inside buildings
    /// without interiors yet, or off campus) make the NPC invisible on arrival.
    /// </summary>
    public sealed class PlaceDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        /// <summary>Area whose slots the NPC occupies after arriving ("campus" or an interior id); null when hidden.</summary>
        public string AreaId { get; }
        public bool Hidden => AreaId == null;

        public PlaceDefinition(string id, string displayName, string areaId)
        {
            Id = id; DisplayName = displayName; AreaId = areaId;
        }
    }

    public sealed class PlaceCatalog
    {
        readonly Dictionary<string, PlaceDefinition> _places = new Dictionary<string, PlaceDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, (float x, float z)> _access = new Dictionary<string, (float, float)>(StringComparer.Ordinal);

        public PlaceCatalog Add(PlaceDefinition place)
        {
            if (_places.ContainsKey(place.Id)) throw new ArgumentException($"Duplicate place '{place.Id}'.");
            _places.Add(place.Id, place);
            return this;
        }

        public bool Contains(string id) => id != null && _places.ContainsKey(id);
        public PlaceDefinition Get(string id) => _places.TryGetValue(id ?? "", out var p) ? p : throw new KeyNotFoundException($"Unknown place '{id}'.");
        public IEnumerable<PlaceDefinition> All => _places.Values;

        /// <summary>Sets where on the outdoor campus a place is entered (supplied by the world builder).</summary>
        public void SetAccessPoint(string id, float x, float z)
        {
            Get(id);
            _access[id] = (x, z);
        }

        public bool HasAccessPoint(string id) => _access.ContainsKey(id);

        public (float x, float z) AccessPoint(string id)
            => _access.TryGetValue(id, out var p) ? p : throw new InvalidOperationException($"Place '{id}' has no access point.");
    }

    public struct ScheduleEntry
    {
        /// <summary>Minute of day (0..1439) at which the NPC sets off for <see cref="PlaceId"/>.</summary>
        public int StartMinute;
        public string PlaceId;
        public ActivityKind Activity;

        public ScheduleEntry(int startMinute, string placeId, ActivityKind activity)
        {
            StartMinute = startMinute; PlaceId = placeId; Activity = activity;
        }

        public override string ToString() => $"{StartMinute / 60:00}:{StartMinute % 60:00} {Activity} @ {PlaceId}";
    }

    /// <summary>One day of intentions, ordered by start time; the first entry starts at 00:00.</summary>
    public sealed class DailySchedule
    {
        readonly List<ScheduleEntry> _entries;

        public IReadOnlyList<ScheduleEntry> Entries => _entries;

        public DailySchedule(IEnumerable<ScheduleEntry> entries)
        {
            _entries = new List<ScheduleEntry>(entries);
            _entries.Sort((a, b) => a.StartMinute.CompareTo(b.StartMinute));
            if (_entries.Count == 0 || _entries[0].StartMinute != 0)
                throw new ArgumentException("A schedule must start at 00:00.");
        }

        public ScheduleEntry EntryAt(int minuteOfDay)
        {
            var current = _entries[0];
            foreach (var e in _entries)
            {
                if (e.StartMinute <= minuteOfDay) current = e;
                else break;
            }
            return current;
        }

        public List<string> Validate(PlaceCatalog places)
        {
            var errors = new List<string>();
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.StartMinute < 0 || e.StartMinute >= 1440) errors.Add($"Entry {i} starts outside the day: {e}.");
                if (!places.Contains(e.PlaceId)) errors.Add($"Entry {i} uses unknown place: {e}.");
                if (i > 0 && e.StartMinute == _entries[i - 1].StartMinute) errors.Add($"Entries {i - 1} and {i} start at the same time.");
            }
            return errors;
        }
    }
}
