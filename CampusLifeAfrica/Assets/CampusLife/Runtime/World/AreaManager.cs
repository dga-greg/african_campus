using System;
using System.Collections;
using System.Collections.Generic;
using CampusLife.Runtime.CameraSystem;
using CampusLife.Runtime.Player;
using UnityEngine;

namespace CampusLife.Runtime.World
{
    /// <summary>A streamable region of the world: the outdoor campus or a building interior.</summary>
    public sealed class CampusArea
    {
        public string Id;
        public string DisplayName;
        public bool IsInterior;
        public GameObject Root;
        public readonly Dictionary<string, Transform> Spawns = new Dictionary<string, Transform>();
        /// <summary>Builds the area's content on first use (lazy streaming of interiors).</summary>
        public Func<CampusArea, GameObject> Builder;
        public string DefaultSpawn = "start";

        public bool IsBuilt => Root != null;

        public Transform AddSpawn(string id, Transform parent, Vector3 localPosition, float yaw)
        {
            var t = new GameObject("Spawn_" + id).transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Spawns[id] = t;
            return t;
        }
    }

    public interface IScreenFader
    {
        IEnumerator FadeOut(float seconds, string caption);
        IEnumerator FadeIn(float seconds);
    }

    /// <summary>
    /// Owns area activation. Only the area the player is in stays active;
    /// interiors are built on first entry and then toggled. Behind this API,
    /// areas can later move to additive scenes or Addressables without
    /// changing doors, saves or UI.
    /// </summary>
    public sealed class AreaManager : MonoBehaviour
    {
        public static AreaManager Instance { get; private set; }

        readonly Dictionary<string, CampusArea> _areas = new Dictionary<string, CampusArea>();

        public PlayerController Player { get; set; }
        public ThirdPersonCameraRig CameraRig { get; set; }
        public IScreenFader Fader { get; set; }
        public CampusArea Current { get; private set; }
        public bool IsTransitioning { get; private set; }

        /// <summary>Raised after the player arrives in an area (also after loading a save).</summary>
        public event Action<CampusArea> AreaEntered;
        /// <summary>Raised when the player is about to leave an area.</summary>
        public event Action<CampusArea> AreaLeaving;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Register(CampusArea area)
        {
            if (_areas.ContainsKey(area.Id)) throw new ArgumentException($"Area '{area.Id}' already registered.");
            _areas.Add(area.Id, area);
            if (area.Root != null) area.Root.SetActive(false);
        }

        public bool TryGet(string id, out CampusArea area) => _areas.TryGetValue(id ?? string.Empty, out area);

        public CampusArea Get(string id)
        {
            if (!TryGet(id, out var area)) throw new KeyNotFoundException($"Unknown area '{id}'.");
            return area;
        }

        public IEnumerable<CampusArea> All => _areas.Values;

        public void EnsureBuilt(CampusArea area)
        {
            if (area.IsBuilt || area.Builder == null) return;
            area.Root = area.Builder(area);
            area.Root.SetActive(false);
        }

        void Activate(CampusArea area)
        {
            EnsureBuilt(area);
            foreach (var a in _areas.Values)
                if (a.Root != null && a != area) a.Root.SetActive(false);
            area.Root.SetActive(true);
            Current = area;
            if (CameraRig != null) CameraRig.UseInteriorProfile(area.IsInterior);
        }

        /// <summary>Instantly places the player at a named spawn (new game, debug).</summary>
        public void PlaceAtSpawn(string areaId, string spawnId)
        {
            var area = Get(areaId);
            Activate(area);
            var spawn = ResolveSpawn(area, spawnId);
            Player.Teleport(spawn.position, spawn.eulerAngles.y);
            if (CameraRig != null) CameraRig.SnapBehindTarget();
            AreaEntered?.Invoke(area);
        }

        /// <summary>Instantly places the player at a saved position.</summary>
        public void PlaceAt(string areaId, Vector3 position, float yaw)
        {
            if (!TryGet(areaId, out var area)) area = Get("campus");
            Activate(area);
            Player.Teleport(position, yaw);
            if (CameraRig != null) CameraRig.SnapBehindTarget();
            AreaEntered?.Invoke(area);
        }

        public void RespawnInCurrentArea()
        {
            if (Current == null) return;
            var spawn = ResolveSpawn(Current, Current.DefaultSpawn);
            Player.Teleport(spawn.position, spawn.eulerAngles.y);
            if (CameraRig != null) CameraRig.SnapBehindTarget();
        }

        static Transform ResolveSpawn(CampusArea area, string spawnId)
        {
            if (spawnId != null && area.Spawns.TryGetValue(spawnId, out var t)) return t;
            if (area.Spawns.TryGetValue(area.DefaultSpawn, out t)) return t;
            throw new InvalidOperationException($"Area '{area.Id}' has no spawn '{spawnId}' or default spawn.");
        }

        public void RequestTransition(string areaId, string spawnId)
        {
            if (IsTransitioning) return;
            StartCoroutine(TransitionRoutine(areaId, spawnId));
        }

        IEnumerator TransitionRoutine(string areaId, string spawnId)
        {
            IsTransitioning = true;
            bool hadControl = Player.ControlEnabled;
            Player.ControlEnabled = false;
            var target = Get(areaId);
            if (Current != null) AreaLeaving?.Invoke(Current);
            if (Fader != null) yield return Fader.FadeOut(0.3f, target.DisplayName);
            // Building happens while the screen is dark, so any hitch is hidden.
            EnsureBuilt(target);
            yield return null;
            Activate(target);
            var spawn = ResolveSpawn(target, spawnId);
            Player.Teleport(spawn.position, spawn.eulerAngles.y);
            if (CameraRig != null) CameraRig.SnapBehindTarget();
            AreaEntered?.Invoke(target);
            if (Fader != null) yield return Fader.FadeIn(0.35f);
            Player.ControlEnabled = hadControl;
            IsTransitioning = false;
        }
    }
}
