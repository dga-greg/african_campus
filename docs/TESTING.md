# Testing

## Automated

| Suite | Where | Runs | Covers |
|---|---|---|---|
| Core/EditMode | `CampusLifeAfrica/Assets/CampusLife/Tests/EditMode` | `dotnet test tools/CoreTests/CoreTests.csproj` **and** Unity Test Runner (EditMode) | Clock & pausing, money formatting, wallet ledger (duplicates, overdrafts, tampering), atomic purchases, inventory, JSON serializer (precision, escaping, malformed input), save service (round-trip, checksum, backup recovery, too-new saves, migrations, atomic file writes, unique transaction ids across reloads), dialogue engine and all Phase 1 content (validation, full orientation journey, alternative paths, kiosk purchases, memories), relationships, objectives, profile validation, movement/camera/interaction maths; **Phase 2:** roster, schedules (validity, determinism, variety, roles, weekdays, guard coverage, walking-time fit), campus simulation (travel, arrival, snapping, day/night density), queue, deterministic RNG, ambient dialogue and per-resident memory; **Phase 3:** grading/GPA, curriculum validity, question shuffling and hints, timetables, calendar, registration rules and prerequisites, attendance timing, study, assignments and late penalties, quiz/exam rules, results/CGPA/probation/resits, save v2 and migration, rest timing |
| Unity compile check | `tools/UnityCompileCheck` | `dotnet build` | All runtime scripts + PlayMode tests against UnityEngine 2021.3 reference assemblies |
| PlayMode smoke | `CampusLifeAfrica/Assets/CampusLife/Tests/PlayMode` | Unity Test Runner (PlayMode) only | New game spawn and allowance, walking and staying grounded, entering the library, climbing stairs to the mezzanine, exiting, talking to Mr. Darko (card, clock pause), save → load inside the library, sitting/standing, camera wall collision, kiosk purchase charging the wallet; **Phase 2:** NavMesh bake, residents appearing within limits, travellers making progress, interior/exterior unloading, resident memory, slot exclusivity, kiosk queue order; **Phase 3:** attending a lecture in the hall end to end, studying in the library, resting until morning |

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

Living campus (Phase 2) — turn on Settings → "Show living-campus stats":
15. After the NavMesh finishes baking (stats show "ready"), students walk from Unity Hall toward the kiosk around 06:20–07:30; a queue forms at the counter and people eat nearby.
16. Around 07:45 programme groups head into the Lecture Theatre Complex and disappear through its doors; others go to the library or plaza.
17. The map (M) shows dots moving; the "people out on campus" count rises by day and drops to about one (the night guard) after 23:00.
18. Two friends meeting on the walkway stop to chat. Sprint into someone: they react with a speech bubble; talk to them afterwards and they mention it.
19. Introduce yourself to a student; walk past them later and they greet you by name.
20. Inside the library, residents sit at tables and desks with books; when their schedule changes they walk out of the door.
21. The guard patrols gate ↔ library; footballers run laps in the evening; the cleaner sweeps the plaza in the morning.
22. Graphics quality Low/Medium/High changes how many residents have bodies (20/30/40) — check FPS stays on target.

University (Phase 3):
23. After orientation the objective says to register: Tab → Academics, toggle an elective, Register; errors appear if you untick to below 15 credits; timetable appears.
24. Follow the guide arrow to the Lecture Theatre Complex, enter, read the timetable board in the lobby, go into the right hall and sit down within an hour of the lecture.
25. "Wait for the lecture to start" skips time; the board shows the topic and the lecturer appears; take notes, answer the question, finish — time jumps to the end.
26. Arrive 20 minutes late: attendance is "Late"; 31+ minutes: you cannot join.
27. In the library, sit and study a topic: notes appear in Academics → Notes; preparation and hints increase. Work on an assignment; the score shows in Course progress.
28. Week 3 lecture includes the four-question quiz.
29. Rest at Unity Hall (night → 06:30 next day; day → +2 hours).
30. Exam week: sit an exam in LT1, use a 50/50 hint; day 42: the results screen appears with GPA/CGPA; the Academics tab keeps the results.
31. Load a save made with v0.2.0 (Phase 2): it loads, and you can register courses.
