# Testing

## Automated

| Suite | Where | Runs | Covers |
|---|---|---|---|
| Core/EditMode | `CampusLifeAfrica/Assets/CampusLife/Tests/EditMode` | `dotnet test tools/CoreTests/CoreTests.csproj` **and** Unity Test Runner (EditMode) | Clock & pausing, money formatting, wallet ledger (duplicates, overdrafts, tampering), atomic purchases, inventory, JSON serializer (precision, escaping, malformed input), save service (round-trip, checksum, backup recovery, too-new saves, migrations, atomic file writes, unique transaction ids across reloads), dialogue engine and all Phase 1 content (validation, full orientation journey, alternative paths, kiosk purchases, memories), relationships, objectives, profile validation, movement/camera/interaction maths |
| Unity compile check | `tools/UnityCompileCheck` | `dotnet build` | All runtime scripts + PlayMode tests against UnityEngine 2021.3 reference assemblies |
| PlayMode smoke | `CampusLifeAfrica/Assets/CampusLife/Tests/PlayMode` | Unity Test Runner (PlayMode) only | New game spawn and allowance, walking and staying grounded, entering the library, climbing stairs to the mezzanine, exiting, talking to Mr. Darko (card, clock pause), save → load inside the library, sitting/standing, camera wall collision, kiosk purchase charging the wallet |

CI (`.github/workflows/ci.yml`) runs the first two on every push.

## Manual checklist (Phase 1)

Desktop (Editor and WebGL build):
1. Main menu shows; New Game opens the creator; every option updates the 3D preview; empty name shows an error.
2. Spawn at the gate; Esi is visible ahead-right; objective arrow points at her.
3. Walk/run (Shift); camera orbits with mouse, zooms with wheel; walk into a wall with the camera behind you — no clipping.
4. Talk to Esi (E); choose with mouse and number keys; Esc leaves without opening the pause menu.
5. Follow the guide to the library; climb the steps; "Enter Nkabom Library" prompt; fade; interior banner.
6. Climb the stairs to the mezzanine; sit on a chair (E) and stand (E or move).
7. Register with Mr. Darko; card appears in Dashboard → Inventory; People shows Darko's memory.
8. Exit, buy sachet water at the kiosk; balance and ledger update; option disappears when you cannot afford it.
9. Show Esi the card; journal shows all objectives done.
10. Esc → Save to slot 1; quit to main menu; Continue resumes at the same place; Load slot 1 works; delete asks to confirm.
11. Leave the game running past 18:15 in-game: street lights turn on, sky darkens.
12. Settings: sensitivity, invert Y, reduced motion (no FOV kick, no typewriter, no fades), graphics quality, UI size — all apply immediately and persist after restart.

Mobile (Android/iOS build, or Settings → Touch controls: On):
13. Left-thumb joystick appears where you touch; right-side swipe turns the camera; buttons do not rotate the camera.
14. Contextual interact button shows the right label; Walk/Run toggle works; Menu/Map/Student buttons open panels.
