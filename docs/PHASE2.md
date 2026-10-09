# Phase 2 — Living Campus

Goal from the brief: *demonstrate students independently moving between destinations according to schedules.*

## What is implemented

### Residents (`Core/Content/Phase2Content.cs`)
29 autonomous residents, identical in every playthrough (stable ids for saves and memories):
- 24 students across all ten programmes, each with traits (studious, sporty, social), a friend group and walking speed.
- 2 lecturers, a day-shift and a night-shift security guard, and a cleaner.
- Appearances are generated from the same cosmetic options as the player's character.

### Schedules (`Core/Npcs/ScheduleGenerator.cs`)
Deterministic daily plans, generated per resident and per day:
- **Students:** breakfast at the kiosk, lectures on their programme's timetable (programmes differ: Mon/Wed/Fri mornings vs Tue/Thu, plus one afternoon practical), free time chosen by personality (library, plaza, lawn, sports field), lunch, afternoon lectures, evening football/socialising/relaxing, back to Unity Hall; social students go out again at night.
- **Weekends:** later starts and free time; about a third take the shuttle into town in the afternoon.
- **Staff:** lecturers teach on weekdays and are off campus at weekends. Guards cover the whole day between the two shifts (booth duty and patrols). The cleaner sweeps the plaza and walkways in the morning.
- Every start time is jittered per resident and per day, so people never move all at once or repeat an identical day. Meal blocks are long enough to walk to the kiosk.

### Simulation layers
- **Abstract simulation (`CampusSimulation`)** runs for everyone all the time. Each resident is either at a place or walking between access points at their walking speed (converted to game time). It is deterministic, so it needs no save data: loading a game re-derives where everyone is.
- **Bodies (`ResidentPopulation`)** are given only to residents near the observer, by detail level:
  - **High** (nearest): high-quality avoidance, full-rate animation, chatting with friends, reactions to the player.
  - **Medium:** cheaper avoidance, half-rate animation.
  - **Low:** no body; the position comes from the simulation (shown on the map).
  - **Unloaded:** inside a building or in another area; schedule progression only.
- **Limits per graphics profile:** 20, 30 or 40 bodies within 45, 60 or 70 m. Bodies are built once per resident and reused. At most 2 new bodies are built and 5 shown per 0.2 s pass, so spawning cannot spike a frame.
- A travelling resident who appears mid-journey is placed at the right fraction along the real NavMesh path, not on a straight line.

### Navigation
- A NavMesh is baked at runtime from the colliders of the procedural campus (and of the library interior on first entry), asynchronously, with no pre-baked assets.
- Characters are excluded from the bake. The player and the story NPCs are NavMesh obstacles, so residents steer around them with local avoidance.
- Spawn points must connect to the main walkable area (this rejects unreachable spots such as the security booth roof). An agent that makes no progress for a while is moved on.

### Behaviour at places (`ResidentAgent`, `PlaceSlots`)
- **Seats:** two per bench (plaza, sports field, shuttle stop) plus library chairs. Residents and the player never share a seat.
- **Chat circles:** three on the plaza. Friends gather at the same circle, alternate talking and checking phones, and sit on the grass on the west lawn.
- **Kiosk queue:** first come, first served. A resident is served at the front, then eats at a spot nearby holding a meat pie.
- **Routes:** footballers run laps of the pitch, guards patrol from the gate to the library with pauses, and the cleaner sweeps the plaza and walkway with pauses.
- **Library:** residents study at reading tables and mezzanine desks (book in hand), browse the stacks, and walk out of the door when it is time to leave.
- **Unity Hall and the Lecture Theatre Complex:** residents walk to the new doorways and go inside (they are hidden in the simulation while there). Each doorway has a readable notice explaining when the building opens to the player.
- **Activity animations:** walking, running, sitting, sitting on the grass, talking, phone, reading, eating and sweeping, with matching hand props.

### Social life and reactions
- Two friends who pass on a walkway stop, face each other and chat for a few seconds, then carry on.
- Running past or into a resident makes them stop, look and react with a speech bubble ("Eish! Watch where you're going!"). Actually bumping into someone is remembered: it shows on their relationship record, lowers their liking for you a little, and they bring it up next time you talk.
- Residents you have introduced yourself to, and who like you, greet you by name when you walk past.

### Talking to residents
- Every resident can be approached (E). Shared ambient dialogues for students, lecturers, security and the cleaner fill in who they are and what they are doing right now ("Heading to Nkabom Library to study…"), plus campus news.
- Each resident keeps their own memory of you (introductions, offers to hang out, thanking staff, being bumped into), and it is saved.

### Map, settings, time
- The campus map shows everyone currently out on campus (students and staff in different colours) and a live count.
- Settings → "Show living-campus stats" shows resident, body and NavMesh counts on the HUD.
- The default clock is now 0.3 game minutes per real second (an 80-minute day), so a resident can walk across campus between schedule blocks. (At the previous 1:1 rate, a 90 m walk took 90 game minutes and nobody arrived anywhere. A unit test guards this.)
- Residents also live in the main-menu background.

## Verification
- **Core:** 72 unit tests pass (18 new in Phase 2). They cover roster stability, schedule validity for every resident on every day of two weeks, determinism, variety, role and weekday rules, 24-hour security coverage, walking times fitting between blocks, travel and arrival timing, early physical arrival, time-jump snapping, day versus night crowd density, the queue, the RNG, and ambient dialogue with per-resident memory.
- **Unity scripts and PlayMode tests:** compile against the UnityEngine reference assemblies (warnings as errors).
- **New PlayMode tests (written, not yet run):** the NavMesh bakes within a minute, residents appear within limits, travellers make progress, entering the library unloads outdoor bodies, a resident remembers an introduction, slots are never shared, and the kiosk queue order is correct.
- **Pending:** an in-Editor run and profiling (NavMesh bake time on WebGL and mobile especially). As in Phase 1, the Unity Editor could not be installed in the environment where this was written.

## Known limitations
- Residents do not ride a visible shuttle bus; "off campus" means walking out of the main gate.
- Interiors of Unity Hall and the Lecture Theatre Complex are not built yet (Phases 3–4). Residents are simulated abstractly while inside.
- The story NPCs (Esi, Mr. Darko, Auntie Akosua) keep fixed posts so the orientation objectives stay reliable.
- Campus events and crowds gathering for them arrive with Phase 4.
