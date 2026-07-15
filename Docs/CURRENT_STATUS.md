# Current Status

## Phase

Milestone 1 in progress - weight, pressure plate, and physical door validated.

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

## Next task

Run `Prompts/06_FIRST_LEVEL.md` to complete the first greybox level and exit flow only.

## Known issues

- This Windows machine blocks direct PowerShell script execution by policy; invoke the
  scripts with `powershell.exe -ExecutionPolicy Bypass -File <script>` or use an
  equivalent approved execution-policy configuration.
- Unity's BuildPlayerDataGenerator logs non-fatal `System.Numerics.Vector*` resolution
  diagnostics while scanning the bundled Code Coverage `ReportGeneratorMerged.dll`.
  The Tundra step, BuildReport, and final Windows build all succeed with zero reported
  build warnings or errors. Package dependencies remain unchanged.

## Last validated

2026-07-14 with Unity 2022.3.62f3:

- project import and compilation: passed;
- project validation: passed;
- Player, NormalHazard, NormalCorpse, PressurePlate, Door, and bootstrap scene generation:
  passed twice with stable file hashes;
- Edit Mode tests: 35 executed, 35 passed;
- Play Mode tests: 35 executed, 35 passed;
- Windows x64 Development Build: passed;
- build output: `Builds/Windows/DIEGAME.exe`.
