# Roadmap

Status key: **Done** = implemented and verified as described in the phase notes; **Code complete, needs in-Editor verification** = written and compile-checked, not yet run in Unity; **Planned**.

| Phase | Scope | Status |
|---|---|---|
| 1 — 3D Foundation | Unity project, URP setup tooling, third-person controller, camera, character model + creator, campus exterior, Nkabom Library interior, interaction system, three NPCs with branching dialogue and memory, wallet + kiosk purchases, objectives, save/load with backups, day/night lighting, HUD, dashboard, map, settings, touch controls | **Code complete, needs in-Editor verification** ([PHASE1.md](PHASE1.md)) |
| 2 — Living Campus | 20+ autonomous NPCs on NavMesh with daily schedules (role/programme/weekday), social stops, benches, queues, crowd density by time of day, simulation LOD and pooling, security/cleaner/vendor roles, ambient audio | Planned |
| 3 — University Gameplay | Course registration, timetables per programme, physical lectures (seats, notes, short challenges), assignments, quizzes, exams, configurable AMU grading, GPA/CGPA, first semester | Planned |
| 4 — Student Life | Hostel rooms, roommates, sleep/energy, wardrobe changes, cafeteria and shops with vendors, clubs, campus events | Planned |
| 5 — Entrepreneurship | Campus jobs, student businesses (inventory, pricing, transparent demand model, P&L, reputation) | Planned |
| 6 — Four-Year Experience | Eight semesters, progression, 100+ narrative events with delayed consequences, graduation outcomes, careers | Planned |
| 7 — Social Features | Graduation cards, achievement sharing, friend challenges, opt-in server-validated leaderboards | Planned |
| 8 — Production | Profiling and optimisation per platform, accessibility pass, security review, backend deployment, monitoring | Planned |

## Immediate next steps

1. Open the project in Unity 6 LTS, run *Set Up Project*, play through the Phase 1 checklist in [TESTING.md](TESTING.md), and run the PlayMode tests. Fix anything found.
2. Profile on a mid-range desktop browser (WebGL) and a mid-range Android phone; adjust the graphics profiles in `GraphicsProfiles.cs`.
3. Start Phase 2: move NPC schedules into Core (unit-tested), bake a NavMesh for the campus at runtime, and add the first ten walking students.
