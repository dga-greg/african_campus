using System;
using System.Collections.Generic;
using CampusLife.Core.Content;
using CampusLife.Core.Dialogue;
using CampusLife.Core.Npcs;
using CampusLife.Core.Player;
using CampusLife.Core.Session;
using CampusLife.Core.Social;
using CampusLife.Runtime.Characters;
using CampusLife.Runtime.Rendering;
using UnityEngine;
using UnityEngine.AI;

namespace CampusLife.Runtime.Npc
{
    /// <summary>
    /// Runs the living campus. Every resident is always simulated abstractly
    /// (<see cref="CampusSimulation"/>); only those near the observer get a 3D
    /// body, at one of these levels:
    /// High (nearest, full navigation quality, full-rate animation, social stops and reactions),
    /// Medium (cheaper avoidance, half-rate animation),
    /// Low (no body: position comes from the abstract simulation; shown on the map),
    /// Unloaded (inside a building or another area: schedule progression only).
    /// Bodies are built once per resident and reused, with a per-frame build
    /// budget so spawning never causes a spike.
    /// </summary>
    public sealed class ResidentPopulation : MonoBehaviour
    {
        const float LodInterval = 0.2f;
        const float Hysteresis = 8f;
        const int MaxBuildsPerPass = 2;
        // Re-showing an existing body is cheaper (no mesh building) but still computes a path.
        const int MaxSpawnsPerPass = 5;

        public CampusSimulation Sim { get; private set; }
        public PlaceSlots Slots { get; private set; }
        public MaterialLibrary Materials { get; private set; }

        /// <summary>Current game time in minutes (session clock, or the menu clock).</summary>
        public Func<double> Now;
        public Func<double> MinutesPerRealSecond;
        public Func<GameSession> Session;
        public Func<Vector3> ObserverPosition;
        public Func<string> ObserverArea;
        public Transform Player;
        public Func<float> PlayerSpeed;
        public Func<float> RunThreshold;

        public int MaxActive { get; set; } = 30;
        public float SpawnRadius { get; set; } = 60f;
        public float HighRadius { get; set; } = 26f;

        public int ActiveCount { get; private set; }
        public int HighCount { get; private set; }
        public int MediumCount { get; private set; }

        readonly Dictionary<string, ResidentAgent> _bodies = new Dictionary<string, ResidentAgent>();
        readonly Dictionary<string, NavMeshBaker> _navMeshes = new Dictionary<string, NavMeshBaker>();
        readonly Dictionary<string, DialogueGraph> _dialogues = new Dictionary<string, DialogueGraph>();
        readonly List<Candidate> _candidates = new List<Candidate>();
        readonly HashSet<string> _keep = new HashSet<string>();
        readonly List<ResidentAgent> _active = new List<ResidentAgent>();
        Transform _root;
        float _lodTimer;
        float _socialTimer;
        double _lastNow = double.NaN;

        struct Candidate
        {
            public NpcSimState State;
            public float Distance;
            public Vector3 Position;
            public bool HasBody;
        }

        public void Initialise(PlaceCatalog places, PlaceSlots slots, MaterialLibrary materials)
        {
            Slots = slots;
            Materials = materials;
            Sim = new CampusSimulation(Phase2Content.Roster, places);
            foreach (var g in Phase2Content.CreateDialogues()) _dialogues.Add(g.Id, g);
            _root = new GameObject("Residents").transform;
            _root.SetParent(transform, false);
        }

        /// <summary>Bakes navigation for an area (called by builders while the area's root is active).</summary>
        public void BakeArea(string areaId, Transform root, Bounds bounds)
        {
            if (_navMeshes.TryGetValue(areaId, out var old)) old.Remove();
            var baker = new NavMeshBaker();
            baker.BakeAsync(root, bounds);
            _navMeshes[areaId] = baker;
        }

        public bool IsNavigationReady(string areaId) => _navMeshes.TryGetValue(areaId, out var b) && b.IsReady;

        public float NavigationProgress(string areaId) => _navMeshes.TryGetValue(areaId, out var b) ? b.Progress : 0f;

        public IEnumerable<ResidentAgent> ActiveBodies => _active;

        /// <summary>Outdoor position of a resident for the map (live body if present, otherwise simulated).</summary>
        public bool TryGetCampusPosition(NpcSimState state, out Vector3 position)
        {
            if (_bodies.TryGetValue(state.Id, out var body) && body.gameObject.activeSelf && body.AreaId == PlaceIds.CampusArea)
            {
                position = body.transform.position;
                return true;
            }
            position = default;
            if (!Sim.IsOutdoors(state)) return false;
            var (x, z) = Sim.PositionAt(state, _lastNow);
            if (state.Mode == NpcMode.AtPlace) position = Slots.Center(state.PlaceId);
            else position = new Vector3(x, 0f, z);
            return true;
        }

        // --------------------------------------------------------------- frame

        void Update()
        {
            if (Sim == null || Now == null) return;
            double now = Now();
            if (MinutesPerRealSecond != null) Sim.MinutesPerRealSecond = MinutesPerRealSecond();
            // Large jumps (new game, loading, sleeping) re-derive everyone; bodies restart cleanly.
            if (double.IsNaN(_lastNow) || now < _lastNow || now - _lastNow > CampusSimulation.SnapThresholdMinutes) DespawnAll();
            _lastNow = now;
            Sim.Advance(now);

            _lodTimer -= Time.deltaTime;
            if (_lodTimer <= 0f)
            {
                _lodTimer = LodInterval;
                LodPass();
            }
            _socialTimer -= Time.deltaTime;
            if (_socialTimer <= 0f)
            {
                _socialTimer = 0.5f;
                SocialPass();
            }
            DisturbancePass();
        }

        public void DespawnAll()
        {
            foreach (var body in _active) body.Despawn();
            _active.Clear();
            ActiveCount = HighCount = MediumCount = 0;
        }

        void LodPass()
        {
            Vector3 observer = ObserverPosition();
            string area = ObserverArea();
            double now = _lastNow;
            _candidates.Clear();

            foreach (var state in Sim.States)
            {
                _bodies.TryGetValue(state.Id, out var body);
                bool hasBody = body != null && body.gameObject.activeSelf;
                Vector3 pos;
                if (hasBody)
                {
                    if (body.WantsDespawn || body.AreaId != area) continue;
                    pos = body.transform.position;
                }
                else
                {
                    if (!TryEstimate(state, area, now, out pos)) continue;
                    if (!IsNavigationReady(area)) continue;
                }
                float d = Vector3.Distance(new Vector3(pos.x, 0f, pos.z), new Vector3(observer.x, 0f, observer.z));
                float limit = SpawnRadius + (hasBody ? Hysteresis : 0f);
                if (d > limit) continue;
                _candidates.Add(new Candidate { State = state, Distance = d, Position = pos, HasBody = hasBody });
            }
            _candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            _keep.Clear();
            int builds = 0, spawns = 0;
            HighCount = MediumCount = 0;
            for (int i = 0; i < _candidates.Count && _keep.Count < MaxActive; i++)
            {
                var c = _candidates[i];
                var body = c.HasBody ? _bodies[c.State.Id] : null;
                if (body == null)
                {
                    bool fresh = !_bodies.ContainsKey(c.State.Id);
                    if (spawns >= MaxSpawnsPerPass || (fresh && builds >= MaxBuildsPerPass)) continue;
                    body = SpawnBody(c.State, area, c.Position, now);
                    if (body == null) continue;
                    spawns++;
                    if (fresh) builds++;
                }
                _keep.Add(c.State.Id);
                var level = c.Distance <= HighRadius ? DetailLevel.High : DetailLevel.Medium;
                if (level != body.Detail) body.SetDetail(level);
                if (level == DetailLevel.High) HighCount++; else MediumCount++;
                body.Sync(c.State);
            }

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var body = _active[i];
                if (_keep.Contains(body.Profile.Id)) continue;
                body.Despawn();
                _active.RemoveAt(i);
            }
            ActiveCount = _active.Count;
        }

        /// <summary>Where an un-bodied resident would be seen from inside <paramref name="area"/>, if at all.</summary>
        bool TryEstimate(NpcSimState state, string area, double now, out Vector3 position)
        {
            position = default;
            if (state.Mode == NpcMode.Travelling)
            {
                if (area != PlaceIds.CampusArea) return false;
                var (x, z) = Sim.PositionAt(state, now);
                position = new Vector3(x, 0f, z);
                return true;
            }
            var place = Sim.Places.Get(state.PlaceId);
            if (place.Hidden || place.AreaId != area) return false;
            position = Slots.Center(state.PlaceId);
            return true;
        }

        ResidentAgent SpawnBody(NpcSimState state, string area, Vector3 estimate, double now)
        {
            if (!_bodies.TryGetValue(state.Id, out var body))
            {
                body = BuildBody(state.Profile);
                _bodies.Add(state.Id, body);
            }
            Vector3 position = estimate;
            float yaw = 0f;
            bool atPlace = state.Mode == NpcMode.AtPlace;
            if (!atPlace && TryPointAlongRoute(state, now, out var routePoint, out var heading))
            {
                position = routePoint;
                yaw = heading;
            }
            if (!NavMesh.SamplePosition(position, out var hit, 3f, NavMesh.AllAreas)) return null;
            if (!ConnectedToArea(area, hit.position)) return null;
            body.Spawn(state, area, hit.position, yaw, instantArrival: atPlace);
            _active.Add(body);
            return body;
        }

        /// <summary>
        /// The runtime bake also produces small unreachable "islands" (booth and
        /// car roofs). A spawn point must connect to the area's main walkable space.
        /// </summary>
        bool ConnectedToArea(string area, Vector3 point)
        {
            Vector3 anchor;
            if (area == PlaceIds.CampusArea) { if (!Slots.TryGetAccess(PlaceIds.Plaza, out anchor)) return true; }
            else if (!Slots.TryGetAreaExit(area, out anchor)) return true;
            if (!NavMesh.SamplePosition(anchor, out var a, 3f, NavMesh.AllAreas)) return true;
            var path = new NavMeshPath();
            return NavMesh.CalculatePath(point, a.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        /// <summary>Places a travelling resident at the right fraction along the real walking path.</summary>
        bool TryPointAlongRoute(NpcSimState state, double now, out Vector3 point, out float yaw)
        {
            point = default;
            yaw = 0f;
            if (!Slots.TryGetAccess(state.PlaceId, out var dest)) return false;
            var from = new Vector3(state.FromX, 0f, state.FromZ);
            if (!NavMesh.SamplePosition(from, out var a, 6f, NavMesh.AllAreas) || !NavMesh.SamplePosition(dest, out var b, 6f, NavMesh.AllAreas)) return false;
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) || path.corners.Length < 2) return false;
            var corners = path.corners;
            float total = 0f;
            for (int i = 1; i < corners.Length; i++) total += Vector3.Distance(corners[i - 1], corners[i]);
            float target = total * state.Progress(now);
            for (int i = 1; i < corners.Length; i++)
            {
                float seg = Vector3.Distance(corners[i - 1], corners[i]);
                if (target <= seg || i == corners.Length - 1)
                {
                    point = Vector3.Lerp(corners[i - 1], corners[i], seg > 0f ? Mathf.Clamp01(target / seg) : 1f);
                    var dir = corners[i] - corners[i - 1];
                    yaw = dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z)).eulerAngles.y : 0f;
                    return true;
                }
                target -= seg;
            }
            return false;
        }

        ResidentAgent BuildBody(NpcProfile profile)
        {
            var def = profile.Definition;
            var go = new GameObject("Resident_" + def.DisplayName) { layer = 2 };
            go.SetActive(false);
            go.transform.SetParent(_root, false);
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.height = 1.8f;
            col.radius = 0.28f;
            go.AddComponent<NavMeshAgent>().enabled = false;
            var visual = CharacterRigBuilder.Build(go.transform, def.Appearance, Materials);
            var animator = go.AddComponent<CharacterAnimator>();
            animator.Visual = visual;
            var talk = go.AddComponent<NpcCharacter>();
            talk.ControlsBody = false;
            talk.Initialise(def, _dialogues[def.DialogueId], animator, Player);
            talk.Range = 2.2f;
            talk.ConfigureRunner = runner => FillDialogueVariables(runner, profile);
            var agent = go.AddComponent<ResidentAgent>();
            agent.Initialise(profile, this, talk, animator);
            return agent;
        }

        void FillDialogueVariables(DialogueRunner runner, NpcProfile profile)
        {
            var state = Sim.Get(profile.Id);
            var program = AcademicPrograms.Find(profile.ProgramId);
            runner.SetVariable("first", profile.Definition.DisplayName.Split(' ')[0]);
            runner.SetVariable("their_program", program != null ? program.Name : profile.Definition.Role);
            runner.SetVariable("status", Phase2Content.DescribeIntention(state.Activity, state.Mode == NpcMode.Travelling));
            runner.SetVariable("news", Phase2Content.NewsFor(profile.Id, (int)(_lastNow / 1440)));
        }

        /// <summary>Called by a body that physically reached its destination's access point.</summary>
        public void OnReachedAccess(ResidentAgent body) => Sim.NotifyArrived(body.Profile.Id, _lastNow);

        // -------------------------------------------------------------- social

        /// <summary>Friends who pass each other on the way somewhere stop for a quick chat.</summary>
        void SocialPass()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                var a = _active[i];
                if (a.Detail != DetailLevel.High || !a.IsTravelling || a.IsBusy || Time.time < a.NextChatTime) continue;
                for (int j = i + 1; j < _active.Count; j++)
                {
                    var b = _active[j];
                    if (b.Detail != DetailLevel.High || !b.IsTravelling || b.IsBusy || Time.time < b.NextChatTime) continue;
                    if (!a.Profile.IsFriendOf(b.Profile)) continue;
                    if ((a.transform.position - b.transform.position).sqrMagnitude > 2.5f * 2.5f) continue;
                    float seconds = UnityEngine.Random.Range(5f, 9f);
                    a.ChatWith(b, seconds);
                    b.ChatWith(a, seconds);
                    a.Say("Hey!");
                    break;
                }
            }
        }

        static readonly string[] BumpLines = { "Eish! Watch where you're going!", "Hey, slow down!", "Careful!", "Ah! Mind yourself!" };

        /// <summary>Residents react when the player runs past or into them, and greet friends they know.</summary>
        void DisturbancePass()
        {
            if (Player == null || !Player.gameObject.activeInHierarchy) return;
            var session = Session?.Invoke();
            bool running = PlayerSpeed != null && RunThreshold != null && PlayerSpeed() > RunThreshold();
            Vector3 p = Player.position;
            foreach (var body in _active)
            {
                if (body.Detail != DetailLevel.High || Time.time < body.NextReactTime || body.Talk.IsTalking) continue;
                float d = (body.transform.position - p).sqrMagnitude;
                if (running && d < 1.3f * 1.3f)
                {
                    body.React(p, BumpLines[UnityEngine.Random.Range(0, BumpLines.Length)]);
                    if (session != null && d < 0.9f * 0.9f)
                    {
                        var rel = session.Relationships.Get(body.Profile.Id);
                        if (rel.Remember("bumped_into", MemoryKind.Conflict, session.Clock.TotalMinutes)) rel.ChangeAffinity(-2);
                    }
                }
                else if (!running && session != null && d < 3.5f * 3.5f && session.Relationships.Knows(body.Profile.Id))
                {
                    var rel = session.Relationships.Get(body.Profile.Id);
                    if (rel.Remembers("introduced") && rel.Affinity >= 3)
                    {
                        body.React(p, $"Hi, {session.Profile.displayName}!");
                        body.NextReactTime = Time.time + 75f;
                    }
                }
            }
        }
    }
}
