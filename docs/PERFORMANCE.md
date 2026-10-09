# Performance budgets

Targets from the brief: 60 FPS on a mid-range desktop, 30 FPS on supported mid-range mobile devices, no spikes during routine spawning, fast area transitions. **These budgets are starting points and have not been profiled yet** (see PHASE1 verification).

| Budget | Desktop (High) | WebGL (Medium) | Mobile (Low) |
|---|---|---|---|
| Frame time | ≤ 16.6 ms | ≤ 16.6 ms | ≤ 33 ms |
| Draw calls / SetPass (campus view) | ≤ 800 / ≤ 120 | ≤ 500 / ≤ 80 | ≤ 250 / ≤ 50 |
| Visible triangles | ≤ 1.5 M | ≤ 600 k | ≤ 250 k |
| Realtime lights | Sun (soft shadows) + 8 street lights | Sun (hard shadows) + 4 | Sun (no shadows) + 0 |
| Camera far clip | 350 m | 260 m | 180 m |
| Managed heap | ≤ 512 MB | ≤ 256 MB | ≤ 200 MB |
| Active NPCs (Phase 2) | 40 full detail | 30 | 20 |

Measures already in place: static batching of all non-moving campus and interior geometry; shared per-colour materials with GPU instancing (SRP Batcher compatible under URP); size-based LOD culling of trees, benches and props; lazy interior construction behind a fade with only one area active; no per-frame allocations in movement, camera or interaction code; procedural meshes reuse Unity's built-in primitives.

Known costs to watch: the IMGUI interface allocates every frame (planned UI Toolkit migration), and each character is ~40 small renderers (to be replaced by skinned meshes with an Animator in later art passes).
