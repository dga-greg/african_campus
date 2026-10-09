using System;
using CampusLife.Core.Content;
using CampusLife.Core.Economy;
using CampusLife.Core.Narrative;
using CampusLife.Core.Player;
using CampusLife.Core.Save;
using CampusLife.Core.Session;
using CampusLife.Runtime.CameraSystem;
using CampusLife.Runtime.Characters;
using CampusLife.Runtime.Dialogue;
using CampusLife.Runtime.Infrastructure;
using CampusLife.Runtime.Interaction;
using CampusLife.Runtime.Lighting;
using CampusLife.Runtime.Npc;
using CampusLife.Runtime.Player;
using CampusLife.Runtime.Rendering;
using CampusLife.Runtime.UI;
using CampusLife.Runtime.World;
using UnityEngine;

namespace CampusLife.Runtime
{
    public enum GameFlowState { MainMenu, CharacterCreation, Playing }

    public enum Overlay { None, Pause, Dashboard, Map, Settings, Save, Load }

    /// <summary>
    /// Composition root. Creates services, builds the world, spawns the
    /// player, camera and NPCs, and runs the top-level game flow (menus,
    /// new game, save/load, autosave). Starts automatically in any scene
    /// that does not already contain one, so pressing Play is enough.
    /// </summary>
    // Runs after input, interaction and dialogue updates (so one key press is
    // consumed by exactly one system) and before the camera rig.
    [DefaultExecutionOrder(50)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        public const string GameVersion = "0.1.0";
        const float AutosaveIntervalSeconds = 180f;
        const string MenuPauseReason = "menu";

        /// <summary>Set to false (e.g. in tests) to stop the automatic bootstrap.</summary>
        public static bool AutoBootstrap = true;
        public static GameBootstrap Instance { get; private set; }

        public GameSettings Settings { get; private set; }
        public SaveSystem Saves { get; private set; }
        public ItemCatalog Items { get; private set; }
        public ObjectiveTracker Objectives { get; private set; }
        public WorldLocations Locations { get; private set; }
        public AreaManager Areas { get; private set; }
        public PlayerController Player { get; private set; }
        public PlayerInputReader Input { get; private set; }
        public InteractionDetector Interactions { get; private set; }
        public ThirdPersonCameraRig CameraRig { get; private set; }
        public DialogueController Dialogue { get; private set; }
        public DayNightCycle DayNight { get; private set; }
        public GameUI UI { get; private set; }
        public NpcFactory Npcs { get; private set; }
        public GuideBeacon Beacon { get; private set; }

        public GameSession Session { get; private set; }
        public GameFlowState State { get; private set; } = GameFlowState.MainMenu;
        public Overlay Overlay { get; private set; } = Overlay.None;
        Overlay _returnOverlay = Overlay.None;

        /// <summary>Profile edited by the character creator before a new game starts.</summary>
        public PlayerProfileData DraftProfile { get; private set; }

        MaterialLibrary _materials;
        WorldBuildContext _buildContext;
        Light _sun;
        float _autosaveTimer;
        bool _autosavePending;
        AppearanceData _builtAppearance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (!AutoBootstrap || FindFirstObjectByType<GameBootstrap>() != null) return;
            new GameObject("CampusLifeAfrica").AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Settings = SettingsStore.Load();
            Items = Phase1Content.CreateItemCatalog();
            Objectives = Phase1Content.CreateObjectives();
            Saves = new SaveSystem(Items);
            _materials = new MaterialLibrary();
            Locations = new WorldLocations();

            CreateLighting();
            CreatePlayerAndCamera();

            var dialogues = Phase1Content.CreateDialogues();
            Npcs = new NpcFactory(_materials, Phase1Content.Npcs, dialogues) { Player = Player.transform };

            Areas = gameObject.AddComponent<AreaManager>();
            Areas.Player = Player;
            Areas.CameraRig = CameraRig;

            Dialogue = gameObject.AddComponent<DialogueController>();
            Dialogue.Session = () => Session;
            Dialogue.CameraRig = CameraRig;
            Dialogue.Ended += _ => Autosave();

            UI = gameObject.AddComponent<GameUI>();
            UI.Game = this;
            Areas.Fader = UI;
            Dialogue.Feedback += UI.Toast;

            BuildWorld();
            Beacon = GuideBeacon.Create(_materials, transform);

            Areas.AreaEntered += OnAreaEntered;
            ApplySettings();
            EnterMainMenu();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------ setup

        void CreateLighting()
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(transform, false);
            _sun = sunGo.AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.shadows = LightShadows.Soft;
            RenderSettings.sun = _sun;
            DayNight = sunGo.AddComponent<DayNightCycle>();
            DayNight.Sun = _sun;
            DayNight.Clock = () => Session?.Clock;
        }

        void CreatePlayerAndCamera()
        {
            // Remove any default scene camera so only the rig renders.
            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (cam.GetComponentInParent<GameBootstrap>() == null) cam.gameObject.SetActive(false);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l != _sun) l.gameObject.SetActive(false);

            var playerGo = new GameObject("Player") { layer = 2 };
            playerGo.transform.SetParent(transform, false);
            PlayerController.ConfigureController(playerGo.AddComponent<CharacterController>());
            Input = playerGo.AddComponent<PlayerInputReader>();
            Player = playerGo.AddComponent<PlayerController>();
            Interactions = playerGo.AddComponent<InteractionDetector>();
            var animator = playerGo.AddComponent<CharacterAnimator>();
            Player.Animator = animator;

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(transform, false);
            camGo.AddComponent<AudioListener>();
            CameraRig = camGo.AddComponent<ThirdPersonCameraRig>();
            CameraRig.Target = playerGo.transform;
            CameraRig.Input = Input;
            CameraRig.Player = Player;
            Player.CameraRig = CameraRig;
            DayNight.Camera = CameraRig.Camera;

            RebuildPlayerVisual(new AppearanceData());
        }

        void BuildWorld()
        {
            var worldRoot = new GameObject("World").transform;
            worldRoot.SetParent(transform, false);
            _buildContext = new WorldBuildContext
            {
                Kit = new WorldKit(_materials),
                Locations = Locations,
                Npcs = Npcs,
                Lighting = DayNight,
                WorldRoot = worldRoot,
            };
            var campus = new CampusArea { Id = CampusExteriorBuilder.AreaId, DisplayName = "AMU Campus", IsInterior = false };
            campus.Root = CampusExteriorBuilder.Build(campus, _buildContext);
            Areas.Register(campus);
            // Interior locations are known before the interior is first built, so
            // objectives inside it can be guided to from outside.
            Locations.Register(Ids.LocationLibraryDesk, LibraryInteriorBuilder.AreaId, LibraryInteriorBuilder.DeskNpcPosition, "Library front desk");
            Areas.Register(new CampusArea
            {
                Id = LibraryInteriorBuilder.AreaId,
                DisplayName = "Nkabom Library",
                IsInterior = true,
                DefaultSpawn = "entrance",
                Builder = a => LibraryInteriorBuilder.Build(a, _buildContext),
            });
        }

        /// <summary>Rebuilds the player's modular character from appearance data.</summary>
        public void RebuildPlayerVisual(AppearanceData appearance)
        {
            var sanitized = CharacterOptions.Sanitize(appearance);
            if (_builtAppearance != null && FieldJsonEquals(_builtAppearance, sanitized)) return;
            var old = Player.GetComponentInChildren<CharacterVisual>(true);
            if (old != null)
            {
                old.gameObject.SetActive(false);
                Destroy(old.gameObject);
            }
            var visual = CharacterRigBuilder.Build(Player.transform, sanitized, _materials);
            Player.Animator.Visual = visual;
            _builtAppearance = sanitized;
        }

        static bool FieldJsonEquals(AppearanceData a, AppearanceData b)
            => Core.Serialization.FieldJson.Serialize(a) == Core.Serialization.FieldJson.Serialize(b);

        // ------------------------------------------------------------- flow

        public void EnterMainMenu()
        {
            State = GameFlowState.MainMenu;
            Overlay = Overlay.None;
            Session = null;
            if (Dialogue.IsActive) Dialogue.Cancel();
            Areas.PlaceAtSpawn(CampusExteriorBuilder.AreaId, "creator");
            Player.gameObject.SetActive(false);
            CameraRig.SetMenuOrbit(new Vector3(0f, 0f, -15f), 34f, 16f);
            DayNight.SetInterior(false);
            DayNight.UpdateLighting(16.5f);
            DayNight.enabled = false;
        }

        public void BeginCharacterCreation()
        {
            State = GameFlowState.CharacterCreation;
            Overlay = Overlay.None;
            DraftProfile = new PlayerProfileData
            {
                displayName = "",
                appearance = new AppearanceData
                {
                    skinTone = UnityEngine.Random.Range(0, CharacterOptions.SkinTones.Length),
                    hairStyle = UnityEngine.Random.Range(0, CharacterOptions.HairStyles.Length),
                    topColor = UnityEngine.Random.Range(0, CharacterOptions.ClothingColors.Length),
                    bottomColor = 1,
                },
            };
            Player.gameObject.SetActive(true);
            Player.ControlEnabled = false;
            Areas.PlaceAtSpawn(CampusExteriorBuilder.AreaId, "creator");
            RebuildPlayerVisual(DraftProfile.appearance);
            CameraRig.SetPortrait(Player.transform);
        }

        /// <summary>Called by the creator whenever an appearance option changes.</summary>
        public void PreviewDraftAppearance() => RebuildPlayerVisual(DraftProfile.appearance);

        public void RotatePreview(float degrees)
        {
            Player.transform.Rotate(0f, degrees, 0f);
        }

        public bool TryStartNewGame(out string error)
        {
            var errors = CharacterOptions.Validate(DraftProfile);
            if (errors.Count > 0)
            {
                error = string.Join(" ", errors);
                return false;
            }
            Session = GameSession.NewGame(DraftProfile, Items, Guid.NewGuid().ToString("N"));
            RebuildPlayerVisual(Session.Profile.appearance);
            Player.gameObject.SetActive(true);
            Areas.PlaceAtSpawn(CampusExteriorBuilder.AreaId, "start");
            EnterPlaying();
            UI.Toast($"Akwaaba, {Session.Profile.displayName}! Welcome to Akwaaba Metropolitan University.");
            Autosave();
            error = null;
            return true;
        }

        public bool TryLoad(string slot, out string message)
        {
            if (!Saves.TryLoad(slot, out var session, out message)) return false;
            if (Dialogue.IsActive) Dialogue.Cancel();
            Session = session;
            RebuildPlayerVisual(Session.Profile.appearance);
            Player.gameObject.SetActive(true);
            var l = Session.Location;
            var pos = new Vector3(l.x, l.y, l.z);
            bool validPosition = !float.IsNaN(pos.x) && !float.IsNaN(pos.y) && !float.IsNaN(pos.z) && pos.sqrMagnitude < 1e8f;
            if (!string.IsNullOrEmpty(l.spawnId) || !validPosition || !Areas.TryGet(l.areaId, out _))
                Areas.PlaceAtSpawn(Areas.TryGet(l.areaId, out _) ? l.areaId : CampusExteriorBuilder.AreaId, l.spawnId);
            else
                Areas.PlaceAt(l.areaId, pos + Vector3.up * 0.05f, l.yaw);
            EnterPlaying();
            return true;
        }

        void EnterPlaying()
        {
            State = GameFlowState.Playing;
            Overlay = Overlay.None;
            Session.Clock.Resume(MenuPauseReason);
            Player.ControlEnabled = true;
            CameraRig.SetFollow();
            CameraRig.SnapBehindTarget();
            DayNight.enabled = true;
            _autosaveTimer = 0f;
        }

        public bool SaveTo(string slot, out string message)
        {
            if (Session == null) { message = "No game in progress."; return false; }
            CaptureLocation();
            return Saves.TrySave(slot, Session, out message);
        }

        public void Autosave()
        {
            if (Session == null || State != GameFlowState.Playing || Areas.IsTransitioning || Dialogue.IsActive) return;
            CaptureLocation();
            if (Saves.TrySave(SaveSystem.AutosaveSlot, Session, out var message)) UI.ShowSaveIndicator();
            else UI.Toast(message);
            _autosaveTimer = 0f;
        }

        void CaptureLocation()
        {
            var p = Player.SafePosition;
            Session.Location = new LocationData
            {
                areaId = Areas.Current != null ? Areas.Current.Id : CampusExteriorBuilder.AreaId,
                x = p.x, y = p.y, z = p.z,
                yaw = Player.transform.eulerAngles.y,
                spawnId = null,
            };
        }

        public void OpenOverlay(Overlay overlay)
        {
            if (overlay == Overlay.Settings || overlay == Overlay.Load || overlay == Overlay.Save) _returnOverlay = Overlay;
            Overlay = overlay;
            if (Session != null)
            {
                if (overlay == Overlay.None) Session.Clock.Resume(MenuPauseReason);
                else Session.Clock.Pause(MenuPauseReason);
            }
            if (State == GameFlowState.Playing && !Dialogue.IsActive) Player.ControlEnabled = overlay == Overlay.None;
        }

        /// <summary>Closes a sub-menu (settings, save, load) back to whatever opened it.</summary>
        public void CloseSubMenu()
        {
            var back = _returnOverlay;
            _returnOverlay = Overlay.None;
            OpenOverlay(back);
        }

        public void CloseOverlay() => OpenOverlay(Overlay.None);

        public void QuitToMainMenu()
        {
            Autosave();
            EnterMainMenu();
        }

        public static bool CanQuitApplication => Application.platform != RuntimePlatform.WebGLPlayer;

        public void QuitApplication()
        {
            Autosave();
            Application.Quit();
        }

        public void ApplySettings()
        {
            SettingsStore.Save(Settings);
            CameraRig.ApplyUserSettings(Settings.lookSensitivity, Settings.invertY, Settings.reducedMotion);
            var profile = GraphicsProfiles.Apply(Settings.graphics, CameraRig.Camera, _sun);
            DayNight.SetMaxStreetLights(profile.MaxStreetLights);
            Input.TouchControlsEnabled = SettingsStore.TouchControlsActive(Settings);
        }

        // ------------------------------------------------------------ frame

        void Update()
        {
            bool playing = State == GameFlowState.Playing && Session != null;
            if (playing)
            {
                Session.Clock.Tick(Time.deltaTime);
                Session.PlayTimeSeconds += Time.unscaledDeltaTime;
                HandleGameplayShortcuts();
                _autosaveTimer += Time.unscaledDeltaTime;
                bool canSave = !Areas.IsTransitioning && !Dialogue.IsActive;
                if (canSave && (_autosavePending || (_autosaveTimer > AutosaveIntervalSeconds && Overlay == Overlay.None)))
                {
                    _autosavePending = false;
                    Autosave();
                }
            }
            UpdateGuide(playing);
            UpdateCursor(playing);
            CameraRig.LookEnabled = playing && Overlay == Overlay.None && !Dialogue.IsActive && !Areas.IsTransitioning;
        }

        void HandleGameplayShortcuts()
        {
            if (Dialogue.IsActive || Dialogue.LastEndedFrame == Time.frameCount || Areas.IsTransitioning) return;
            if (Input.PausePressed)
            {
                if (Overlay == Overlay.None) OpenOverlay(Overlay.Pause);
                else if (Overlay == Overlay.Settings || Overlay == Overlay.Save || Overlay == Overlay.Load) CloseSubMenu();
                else CloseOverlay();
            }
            else if (Input.DashboardPressed)
                OpenOverlay(Overlay == Overlay.Dashboard ? Overlay.None : Overlay.Dashboard);
            else if (Input.MapPressed)
                OpenOverlay(Overlay == Overlay.Map ? Overlay.None : Overlay.Map);
        }

        void UpdateCursor(bool playing)
        {
            bool lockCursor = playing && Overlay == Overlay.None && !Dialogue.IsActive && !Input.TouchControlsEnabled;
            Input.MouseLookEnabled = lockCursor;
            var mode = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            if (Cursor.lockState != mode) Cursor.lockState = mode;
            Cursor.visible = !lockCursor;
        }

        /// <summary>Current objective and where the guide marker points in this area.</summary>
        public ObjectiveDefinition CurrentObjective => Session != null ? Objectives.Current(Session.Flags) : null;

        public bool TryGetGuideTarget(out Vector3 position)
        {
            position = default;
            var objective = CurrentObjective;
            return objective != null && Areas.Current != null
                && Locations.TryResolveGuide(objective.TargetLocationId, Areas.Current.Id, out position);
        }

        void UpdateGuide(bool playing)
        {
            Vector3? beacon = null;
            if (playing && Settings.showGuideMarker && !Dialogue.IsActive && TryGetGuideTarget(out var target)
                && (target - Player.transform.position).sqrMagnitude > 9f)
                beacon = target;
            Beacon.SetTarget(beacon, Settings.reducedMotion);
        }

        void OnAreaEntered(CampusArea area)
        {
            DayNight.SetInterior(area.IsInterior);
            if (State == GameFlowState.Playing)
            {
                UI.ShowAreaBanner(area.DisplayName);
                // Saved on the next frame, once the transition has finished.
                _autosavePending = true;
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Autosave();
        }

        void OnApplicationQuit() => Autosave();
    }
}
