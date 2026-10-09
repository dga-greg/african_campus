# Architecture

Campus Life Africa is split so that the rules of the game can be tested without the engine, and the engine layer stays thin.

```
┌──────────────────────────── CampusLife.Runtime (Unity) ────────────────────────────┐
│ GameBootstrap (composition root, game flow, autosave)                              │
│  ├─ World: AreaManager · CampusExteriorBuilder · LibraryInteriorBuilder · WorldKit │
│  │          DoorInteractable · SeatInteractable · WorldLocations · GuideBeacon     │
│  ├─ Player: PlayerInputReader · PlayerController · InteractionDetector             │
│  ├─ CameraSystem: ThirdPersonCameraRig                                             │
│  ├─ Characters: CharacterRigBuilder · CharacterVisual · CharacterAnimator          │
│  ├─ Npc: NpcFactory · NpcCharacter · ResidentPopulation · ResidentAgent · PlaceSlots│
│  │       NavMeshBaker · SpeechBubble        Dialogue: DialogueController           │
│  ├─ Lighting: DayNightCycle                 Rendering: MaterialLibrary, profiles   │
│  ├─ Academics: AcademicController (seat options, lecture/exam/assignment flows)   │
│  ├─ Infrastructure: SaveSystem (file / browser storage) · SettingsStore            │
│  └─ UI: GameUI (menus, creator, HUD, dialogue, dashboard, map, touch controls)     │
└───────────────────────────────────────┬────────────────────────────────────────────┘
                                        │ uses (one-way)
┌───────────────────────── CampusLife.Core (no UnityEngine) ─────────────────────────┐
│ Session/GameSession — single source of truth for a playthrough                     │
│ Time/GameClock · Economy/Wallet, Inventory, ItemCatalog, Commerce                  │
│ Dialogue/DialogueGraph, DialogueRunner · Social/RelationshipBook (NPC memory)      │
│ Narrative/NarrativeFlags, ObjectiveTracker · Player/PlayerProfile, programmes      │
│ Save/SaveGame, SaveService, SaveMigrator, storage · Serialization/FieldJson        │
│ Simulation/MotionMath, CameraOrbit, InteractionScoring · Content/Phase1/2Content   │
│ Npcs/CampusSimulation, ScheduleGenerator, PlaceCatalog, SlotQueue, Deterministic…  │
│ Academics/AcademicRecord, Calendar, TimetableBuilder, GradingScale, CourseCatalog  │
└────────────────────────────────────────────────────────────────────────────────────┘
```

`CampusLife.Core.asmdef` sets `noEngineReferences: true`, so the compiler enforces the boundary.

## Key decisions

**Procedural world, no hand-authored scene dependencies.** The campus, library interior, characters and UI textures are built in code from Unity primitive meshes. This keeps the repository reviewable as text, avoids binary merge conflicts, and let the foundation be written without an Editor. Every builder uses `WorldKit`, so artist-made prefabs can replace pieces one at a time later (e.g. swap `ShadeTree()` for a prefab instantiation) without touching gameplay.

**Areas instead of one giant scene.** `AreaManager` keeps only the area the player is in active. Interiors are built on first entry (while the screen is faded) and then toggled. Doors, saves, objectives and the map only know *area ids* and *spawn ids*, so a later move to additive scenes or Addressables is local to `AreaManager`.

**State lives in `GameSession`.** MonoBehaviours read and mutate the session; nothing else is persisted. `GameSession.ToSave()/FromSave()` is the only mapping to disk, and `FromSave` re-validates everything (wallet ledger replay, profile validation, appearance sanitising).

**Dialogue changes state only through declared effects.** `DialogueGraph` nodes and choices carry data-only `DialogueCondition`s and `DialogueEffect`s (set flag, change affinity/trust, remember event, give item, purchase). `DialogueRunner` is deterministic. Optional AI-generated lines (future) can vary *text* but cannot change state, as required by the design.

**Money is integer pesewas in a ledger.** `Wallet` applies transactions with unique ids, rejects duplicates and overdrafts, and can be rebuilt from its ledger; a tampered or inconsistent ledger fails to load. `Commerce.TryPurchase` is atomic (no items without payment). `GameSession.NextTransactionId` persists its counter so ids never repeat across reloads.

**Saves are versioned, checksummed and recoverable.** `SaveService` writes `{format, version, checksum, payload}` envelopes; the previous good save is kept as `<slot>_backup` and used automatically if the primary is damaged. `FileSaveStorage` writes via temp-file-and-replace. `SaveMigrator` upgrades old payloads step by step on the raw JSON tree. WebGL uses PlayerPrefs (IndexedDB-backed) because browser file APIs are unreliable. `FieldJson` is a small reflection serializer used everywhere (Unity and tests) so behaviour is identical and 64-bit values round-trip exactly.

**Input is abstracted once.** `PlayerInputReader` reads the Input System package (keyboard, mouse, gamepad, touch) or the legacy Input Manager, and exposes intent (`Move`, `Look`, `InteractPressed`, …). Touch controls (floating joystick, swipe-look, contextual buttons) feed the same properties.

**Camera.** A custom orbit rig (`ThirdPersonCameraRig`) is used instead of Cinemachine for Phase 1: its maths are unit-tested in Core (`CameraOrbit`), and it covers collision, zoom, sensitivity/invert, interior profile, conversation framing and reduced motion. Cinemachine 3 can replace it behind the same API if richer shots are needed later.

**Animation.** Characters are modular primitive rigs animated procedurally (`CharacterAnimator`): idle breathing, walk/run cycles with cadence derived from actual ground speed (to avoid foot sliding), sitting with knee bends, talking gestures and head look-at. When rigged character art arrives, a Mecanim Animator Controller replaces this component; gameplay code only calls `SetSpeed`, `SetPose` and `LookAt`.

**UI.** Phase 1 uses Unity's immediate-mode GUI on a scaled 1280×720 virtual canvas, with generated rounded textures. It is dependency-free and works on every platform. Planned migration: UI Toolkit, once screens stabilise (see roadmap).

## NPC architecture (Phase 2)

The six concerns named in the brief are separate pieces:

1. **Schedule planning:** `ScheduleGenerator` (Core) produces a deterministic day plan per resident from role, programme, traits, weekday and day number.
2. **Goal selection:** `CampusSimulation` (Core) turns the plan into "be at place X doing Y" and runs abstract travel at walking speed. It is the authority for where everyone *should* be, and it needs no save data.
3. **Navigation:** `ResidentAgent` + `NavMeshAgent` on a NavMesh baked at runtime (`NavMeshBaker`). A body that physically arrives first tells the simulation (`NotifyArrived`).
4. **Animation:** `CharacterAnimator` body poses plus a separate hands layer (phone/read/eat/sweep), with update rate set by detail level.
5. **Social interactions:** `PlaceSlots` (seats, chat circles, queues, routes), friend chat stops, reactions to the player, and ambient dialogue graphs filled with live variables.
6. **Persistent NPC memory:** the same `RelationshipBook` as the story NPCs, keyed by the resident's stable id.

`ResidentPopulation` decides each 0.2 s which residents get bodies (detail levels, distance with hysteresis, population cap, spawn budget) and re-derives everything after time jumps.

## Execution order

`PlayerInputReader` (−500) → interaction, player, dialogue (0) → `GameBootstrap` (50) → `ThirdPersonCameraRig` (100). Each key press is consumed by one system; `PlayerController.ControlEnabledFrame` and `DialogueController.LastEndedFrame` stop the same press from also triggering the next system (e.g. Esc that leaves a conversation does not also open the pause menu).

## Planned services (later phases)

- **Backend (Phase 7–8):** ASP.NET Core + PostgreSQL for accounts, cloud saves (same envelope format, server-side validation, conflict detection by `saveId` + `savedUtc`), opt-in leaderboards validated server-side, account deletion and data export. The game remains fully playable offline.
