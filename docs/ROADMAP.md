# Roadmap

Status key: **Done** = implemented and verified as described in the phase notes; **Code complete, needs in-Editor verification** = written and compile-checked, not yet run in Unity; **Planned**.

| Phase | Scope | Status |
|---|---|---|
| 1 — 3D Foundation | Unity project, URP setup tooling, third-person controller, camera, character model + creator, campus exterior, Nkabom Library interior, interaction system, three NPCs with branching dialogue and memory, wallet + kiosk purchases, objectives, save/load with backups, day/night lighting, HUD, dashboard, map, settings, touch controls | **Code complete, needs in-Editor verification** ([PHASE1.md](PHASE1.md)) |
| 2 — Living Campus | 29 autonomous residents on a runtime-baked NavMesh with deterministic daily schedules (role/programme/weekday), seats, chat circles, kiosk queue, patrol/lap/sweep routes, friends stopping to chat, reactions to the player, ambient dialogue with memory, simulation detail levels with spawn budgets, map overlay | **Code complete, needs in-Editor verification** ([PHASE2.md](PHASE2.md)) — ambient audio moved to Phase 4 |
| 3 — University Gameplay | Course registration, timetables per programme, physical lectures (seats, notes, short challenges), assignments, quizzes, exams, configurable AMU grading, GPA/CGPA, first semester | Planned |
| 4 — Student Life | Hostel rooms, roommates, sleep/energy, wardrobe changes, cafeteria and shops with vendors, clubs, campus events | Planned |
| 5 — Entrepreneurship | Campus jobs, student businesses (inventory, pricing, transparent demand model, P&L, reputation) | Planned |
| 6 — Four-Year Experience | Eight semesters, progression, 100+ narrative events with delayed consequences, graduation outcomes, careers | Planned |
| 7 — Social Features | Graduation cards, achievement sharing, friend challenges, opt-in server-validated leaderboards | Planned |
| 8 — Production | Profiling and optimisation per platform, accessibility pass, security review, backend deployment, monitoring | Planned |

## Immediate next steps

1. Open the project in Unity 6 LTS, run *Set Up Project*, play through the Phase 1 and 2 checklists in [TESTING.md](TESTING.md), and run the PlayMode tests. Fix anything found.
2. Profile on a mid-range desktop browser (WebGL) and a mid-range Android phone — especially the runtime NavMesh bake and 30 animated residents — and tune `GraphicsProfiles.cs` and `NavMeshBaker.AgentSettings()` (voxel size).
3. Start Phase 3: programme course structures, registration, timetables that drive the player's day (and residents' lectures), the Lecture Theatre Complex interior with seats and short lecture activities, assessments and GPA.
