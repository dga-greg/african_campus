# Campus Life Africa

An original 3D university life simulation set at **Akwaaba Metropolitan University (AMU)**, a fictional university in Ghana. Players create a student, walk a living campus, meet people who remember them, manage money in Ghana cedis, and (in later phases) progress through four academic years to graduation.

> **Current milestone: Phase 1 — 3D Foundation.** See [docs/PHASE1.md](docs/PHASE1.md) for exactly what is implemented and how it was verified, and [docs/ROADMAP.md](docs/ROADMAP.md) for what comes next.

## Verification status (read this first)

| What | Status |
|---|---|
| Game rules (`CampusLife.Core`: clock, wallet/ledger, purchases, dialogue engine, NPC memory, objectives, save system, movement/camera maths) | **54 NUnit tests passing** under .NET 8 (`tools/CoreTests`) |
| Unity runtime scripts (player, camera, world builder, NPCs, UI, save integration) + PlayMode tests | **Compile cleanly** against UnityEngine 2021.3 reference assemblies (`tools/UnityCompileCheck`), warnings-as-errors |
| Running the game in the Unity Editor / building WebGL, Android, iOS, Windows | **Not yet done.** The development environment used to write this code could not download the Unity Editor (blocked by network policy), so the game has not been played, profiled or built. The first step on a machine with Unity is to open the project, run *Campus Life Africa → Set Up Project*, press Play, and run the PlayMode tests. |
| Input System (new) code path, Editor scripts | Written but **not compiled** here (those assemblies are not on NuGet). |

## Quick start (Unity)

1. Install **Unity 6 LTS (6000.0.x)** with WebGL (and optionally Android/iOS/Windows) build support.
2. Open the `CampusLifeAfrica/` folder as a project in Unity Hub. Packages (URP, Input System, Test Framework) resolve from `Packages/manifest.json`. If Unity asks to enable the new Input System backend, choose **Yes** (the code supports either backend).
3. Menu **Campus Life Africa → Set Up Project**. This creates and assigns a URP asset, shader-carrying materials, `Assets/Scenes/Campus.unity`, build settings and player settings.
4. Open `Assets/Scenes/Campus.unity` and press **Play**. (Pressing Play in *any* empty scene also works: the game bootstraps itself.)
5. Tests: **Window → General → Test Runner** → EditMode (game rules) and PlayMode (walking, stairs, building entry, dialogue, save/load, camera collision).
6. Builds: **Campus Life Africa → Build → WebGL / Windows / Android / iOS**, or headless:
   `Unity -batchmode -quit -projectPath CampusLifeAfrica -executeMethod CampusLife.EditorTools.BuildScript.BuildWebGL`

## Running the checks without Unity

```bash
dotnet test  tools/CoreTests/CoreTests.csproj            # game-rule unit tests
dotnet build tools/UnityCompileCheck/UnityCompileCheck.csproj  # Unity API compile check
```

## Controls

| Action | Desktop | Mobile |
|---|---|---|
| Move | WASD / arrows | Floating joystick (left half of screen) |
| Camera | Mouse (zoom: wheel) | Swipe right half of screen |
| Run | Hold Shift | Walk/Run toggle button |
| Interact / sit / stand | E | Contextual button |
| Student dashboard | Tab | "Student" button |
| Campus map | M | "Map" button |
| Pause / settings / save | Esc | "Menu" button |
| Dialogue choices | 1–5, click, Space to continue | Tap |

Gamepads (Input System backend): left stick move, right stick camera, South = interact, Start = menu, Select = dashboard, North = map.

## Repository layout

```
CampusLifeAfrica/                 Unity project
  Assets/CampusLife/Core/         Engine-independent game rules (no UnityEngine references)
  Assets/CampusLife/Runtime/      MonoBehaviours: bootstrap, world, player, camera, NPCs, UI
  Assets/CampusLife/Editor/       Project setup and build scripts
  Assets/CampusLife/Tests/        EditMode (rules) and PlayMode (scene) tests
tools/CoreTests/                  Runs Core + EditMode tests with .NET 8
tools/UnityCompileCheck/          Compiles Runtime + PlayMode tests against Unity reference DLLs
docs/                             Architecture, roadmap, phase notes, testing, performance
```

## Content and licensing

All code, characters, names, dialogue, the university and its buildings are original and fictional. Geometry and UI textures are generated procedurally at runtime; the project contains no third-party art, audio or fonts beyond Unity's built-in runtime font. The game has no loot boxes, gambling or pay-to-win mechanics, and its core loop works offline without any AI service.
