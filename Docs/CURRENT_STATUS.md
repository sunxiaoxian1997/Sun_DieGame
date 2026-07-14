# Current Status

## Phase

Milestone 1 in progress — normal death, limited lives, and same-player respawn validated.

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

## Next task

Run `Prompts/04_CORPSE_SYSTEM.md` to implement the normal physical corpse only.

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
- Player, NormalHazard, and bootstrap scene generation: passed twice with stable file hashes;
- Edit Mode tests: 11 executed, 11 passed;
- Play Mode tests: 14 executed, 14 passed;
- Windows x64 Development Build: passed;
- build output: `Builds/Windows/DIEGAME.exe`.
