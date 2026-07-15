# Current Status

## Phase

Milestone 1 first greybox gameplay loop implemented and validated.

## Completed

- Design concept selected.
- First prototype scope defined.
- Codex guidance package created.
- Runtime, Editor, Edit Mode test, and Play Mode test assembly boundaries created.
- Idempotent 2D greybox bootstrap scene generation created.
- Bootstrap scene added to Build Settings through Unity Editor APIs.
- Project validation and Windows x64 Development Build automation created.
- Edit Mode and Play Mode smoke tests created and executed.
- Windows x64 Development Build generated successfully.
- Configurable Rigidbody2D horizontal movement and one grounded jump implemented.
- Legacy Input Manager access isolated behind a player input adapter.
- Ground detection implemented with a configurable Collider2D cast and Scene gizmo.
- Player control can be disabled, clears pending input, and can be restored.
- Idempotent Player prefab and greybox scene integration created through Editor APIs.
- Player movement behavior tests created and executed.
- Immutable normal-death context and explicit damage-source contract implemented.
- Idempotent player Alive/Dead transition and accepted-death event implemented.
- Three-life level session, Failed state, and public scene-restart API implemented.
- Configurable delayed respawn restores the same player at the explicit spawn point.
- Player control, physics, collider, ground probe, visibility, velocity, and input state are
  restored deterministically after respawn.
- Normal trigger hazard prefab and Bootstrap scene hazard integration created through Editor APIs.
- Death, life, respawn, failure, and restart behavior tests created and executed.
- Dedicated normal corpse prefab and idempotent corpse initialization implemented.
- CorpseFactory independently consumes accepted deaths and creates exactly one corpse per death.
- LevelSession registers unique runtime corpses without changing life or failure semantics.
- Runtime corpses persist across ordinary respawns and are removed by scene restart lifecycle.
- Player standing and GroundProbe support on normal corpse colliders validated in Play Mode.
- NormalHazard ignores corpses because only PlayerLifeController can accept damage.
- Explicit `IWeightedObject` and `WeightProvider` contracts implemented for the player
  and normal corpse without type-specific pressure-plate coupling.
- PressurePlate rebuilds unique active weight from local 2D overlap state and emits only
  real pressed/released transitions.
- DoorController follows its explicitly assigned PressurePlate, disables its blocking
  Collider2D while open, and restores the closed state without transform drift.
- Idempotent PressurePlate and Door prefab generation plus Bootstrap scene wiring created
  through Unity Editor APIs.
- Weight, pressure threshold, duplicate collider, disable/destroy cleanup, corpse persistence,
  door physics, and fresh lifecycle behavior tests created and executed.
- Explicit Playing, Failed, and Completed level-session states implemented with one-shot
  completion notification and terminal-state death protection.
- LevelExit accepts only the explicitly assigned, currently alive player and rejects corpses,
  hazards, unrelated colliders, dead players, and repeated completion attempts.
- Minimal runtime status display reports remaining lives, current state, controls, and terminal
  restart guidance without adding a formal UI framework.
- Scene restart input is isolated in an explicit adapter and remains available after failure or
  completion.
- Idempotent LevelExit prefab and complete `Level_001` greybox generation implemented through
  Unity Editor APIs.
- `Level_001` contains the intended movement, normal death, persistent normal corpse, respawn,
  pressure-plate, door, exit, completion, and restart flow with explicit scene references.
- Build Settings now start with `Level_001`, while preserving `GreyboxBootstrap` as the second
  validation scene.
- Completion rules, exit filtering, status, restart, first-level structure, anti-bypass layout,
  and the complete gameplay loop are covered by Edit Mode and Play Mode tests.
- Milestone 2 playtest recording and acceptance-gate templates created for blind external testing,
  first-completion timing, observed behavior, physics issues, unintended solutions, and Go/No-Go.
- All ten Level_001 feel-tuning parameters are documented with their current values, exact
  Prefab/scene Inspector locations, and Builder source locations.
- Existing custom movement, ground probe, respawn, pressure threshold, and door offset fields now
  provide clearer Inspector guidance and bounded sliders where a scalar tuning range is useful.
- No additional tuning configuration asset or debug console was introduced because every required
  parameter is already directly editable in its existing Prefab or scene component.

## Next task

Run blind Level_001 playtests with at least three people, record each result in `PLAYTEST_LOG.md`,
evaluate `MILESTONE_2_ACCEPTANCE.md`, and make a Go/No-Go decision before designing another corpse
mechanism.

## Known issues

- This Windows machine blocks direct PowerShell script execution by policy; invoke the
  scripts with `powershell.exe -ExecutionPolicy Bypass -File <script>` or use an
  equivalent approved execution-policy configuration.
- Unity's BuildPlayerDataGenerator logs non-fatal `System.Numerics.Vector*` resolution
  diagnostics while scanning the bundled Code Coverage `ReportGeneratorMerged.dll`.
  The Tundra step, BuildReport, and final Windows build all succeed with zero reported
  build warnings or errors. Package dependencies remain unchanged.

## Last validated

2026-07-15 with Unity 2022.3.62f3:

- project import and compilation: passed;
- project validation: passed;
- Player, NormalHazard, NormalCorpse, PressurePlate, Door, LevelExit, bootstrap scene, and
  `Level_001` generation: passed twice with stable file hashes;
- Milestone 2 infrastructure Builder check: passed twice with eight tracked generated-asset hashes
  unchanged between runs;
- Edit Mode tests: 46 executed, 46 passed;
- Play Mode tests: 47 executed, 47 passed;
- Windows x64 Development Build: passed;
- build output: `Builds/Windows/DIEGAME.exe`.
