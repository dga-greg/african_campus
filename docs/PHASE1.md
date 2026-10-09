# Phase 1 — 3D Foundation

Goal from the brief: *a student character can walk around a small 3D campus, enter a building, interact with an NPC, and save progress.*

## What is implemented

### Flow
- **Main menu** over a slow orbit of the campus: Continue (most recent save), New Game, Load Game (only shown when saves exist), Settings, Quit (hidden on WebGL where it cannot work).
- **Character creator** with live 3D preview: name, pronouns (presets or free text), hometown, programme (10 AMU programmes), ambition, background, skin tone (8), hairstyle (low cut, short afro, big afro, braids, locs, Bantu knots, headwrap, bald), hair colour, top style (T-shirt, kente-inspired trim shirt, hoodie, smock/fugu), top/trouser/shoe colours, accessory (glasses, beaded bracelet, backpack, cap), height. Appearance is cosmetic only.
- **New game** starts on Day 1, Monday 07:00 at the main gate with a GH₵250.00 family allowance recorded in the ledger.

### World (`CampusExteriorBuilder`, `LibraryInteriorBuilder`)
- ~180 m × 180 m walled campus: main gate with signage and security booth, main walkway, ring road with zebra crossing, Akwaaba Plaza with an original sculpture, flower beds and benches, shade-tree avenue and palms, street lights, sports field with goals and spectator benches, parking with cars, Auntie Akosua's kiosk, shuttle stop, exteriors of the Lecture Theatre Complex and Unity Hall (not enterable yet and presented without doors or prompts, so they do not appear interactive).
- **Nkabom Library** (enterable): portico, stairs up to the entrance, door prompt; interior lobby, front desk, reading tables with chairs you can sit on, book stacks, staircase to a quiet-study mezzanine with desks, warm interior lights, exit door.
- Area streaming: the interior is built on first entry behind a fade and only the current area is active.

### Player and camera
- Walk, run, turn, stop, gravity, stairs/slopes (step offset + ramp colliders), sit/stand on benches and chairs, interact, enter/exit buildings.
- Camera-relative movement with acceleration/braking; procedural walk/run/idle/sit/talk animation tied to real ground speed.
- Orbit camera with wall collision, zoom, sensitivity, invert-Y, interior profile, conversation framing, reduced-motion mode.
- Desktop, gamepad (Input System) and touch controls.

### NPCs, dialogue, memory
- **Esi Mensah** (orientation volunteer, main gate), **Mr. Kwabena Darko** (library assistant), **Auntie Akosua** (kiosk owner).
- Branching dialogue with entry rules (NPCs greet you differently once they know you), conditional choices and effects. Example: how you first treat Esi changes how she responds when you show her your library card; Darko recognises you after registering you; Akosua remembers that you stopped by to greet her.
- NPCs turn to face you while talking and look at you when you are nearby. The clock pauses during conversations.
- Relationship records (affinity, trust, conversation count, memories with timestamps) are shown in the dashboard and saved.

### Economy
- Wallet in integer pesewas with a full ledger; kiosk purchases (sachet water GH₵0.50, malt GH₵8.00, meat pie GH₵10.00) are atomic and only offered when affordable; duplicate transactions are impossible.

### Objectives and navigation
- Orientation objectives derived from story flags: meet Esi → register your library card → show Esi the card → free roam.
- HUD shows the objective with a direction arrow and distance; an optional floating marker points at the target or at the door leading to it; the map (M) shows buildings, paths, your position/heading and the objective.

### Saving
- Autosave on new game, area changes, after conversations, every 3 minutes, on app pause/quit; three manual slots; load and delete with confirmation; automatic recovery from the backup if a save is damaged; refusal of saves from newer versions.

### Other
- Day/night lighting from the clock (sunrise ~06:00, sunset ~18:00), street lights at night, fog and ambient colour by time of day.
- Settings: look sensitivity, invert Y, reduced motion, guide marker, FPS counter, touch controls (auto/on/off), graphics quality (low/medium/high), interface size.
- Student dashboard (Tab): profile, wallet ledger, inventory, people & memories, journal.

## Not in Phase 1 (by design)
Autonomous scheduled NPC movement, academics, hostel life, shops beyond the kiosk, businesses, audio, Addressables, cloud saves. See the roadmap.

## Verification
- `tools/CoreTests`: 54 passing tests cover the rules behind every feature above (including the full orientation journey through the real dialogue content, purchases, ledger tampering, save corruption/backup recovery, migrations, camera/movement maths).
- `tools/UnityCompileCheck`: all runtime scripts and the PlayMode tests compile against UnityEngine reference assemblies with warnings as errors.
- **Pending:** in-Editor play-through and PlayMode test run, WebGL/mobile builds and profiling. These could not be performed in the environment where this phase was written (no Unity Editor available). Expect a round of tuning (camera distances, light intensities, sizes of signs) once it is played.

## Known limitations
- IMGUI interface: functional and scalable, but allocation-heavy and not as polished as a UI Toolkit implementation; planned migration.
- Procedural primitive art: readable and consistent, but stylised placeholders for future artist-made models.
- `GraphicsProfiles` sets built-in quality values; under URP the shadow distance comes from the URP asset (set to 60 m by the setup script).
- 3D signs use Unity's legacy `TextMesh` and built-in font; TextMeshPro would give crisper text.
