# AGENTS.md

## Project

Unity 2D side-scrolling puzzle prototype.

Working concept: the player dies, leaves a persistent corpse, respawns, and uses prior corpses as level mechanisms.

## Current milestone

Build only the first greybox gameplay loop:

1. Move and jump.
2. Die on a hazard.
3. Spawn one normal physical corpse at the death position.
4. Respawn at a checkpoint.
5. Keep prior corpses in the level.
6. Use a corpse as weight on a pressure plate.
7. Open a door.
8. Reach the exit.

## Read before working

Always read:

- `Docs/GAME_VISION.md`
- `Docs/DESIGN_PILLARS.md`
- `Docs/CORE_RULES.md`
- `Docs/TECH_ARCHITECTURE.md`
- `Docs/MILESTONES.md`
- `Docs/CURRENT_STATUS.md`

## Repository layout

- `Assets/_Game/Runtime/`: player-build runtime code.
- `Assets/_Game/Editor/`: Unity Editor-only automation and tooling.
- `Assets/_Game/Tests/EditMode/`: deterministic rule tests.
- `Assets/_Game/Tests/PlayMode/`: scene and gameplay integration tests.
- `Assets/_Game/Scenes/`: greybox scenes.
- `Docs/`: design, architecture, milestones, and status.
- `Tools/`: PowerShell automation entry points.
- `Logs/`: generated logs and test result files; do not commit unless explicitly requested.
- `Builds/`: generated players; do not commit.

## Working method

For every non-trivial task:

1. Inspect the current repository and relevant docs.
2. State assumptions and produce a focused plan.
3. Keep the change limited to the current task.
4. Implement the smallest complete solution.
5. Add or update relevant tests.
6. Run available validation.
7. Review the diff.
8. Report changed files, commands run, results, and known limitations.
9. Update `Docs/CURRENT_STATUS.md` only after validation succeeds.

## Prompt contract

Each task should contain:

- Goal.
- Context.
- Constraints.
- Done when.

Do not silently broaden the goal.

## Engineering rules

- Use C# namespaces under `CorpseMechanism`.
- Prefer composition over deep inheritance.
- Keep runtime code independent of `UnityEditor`.
- Editor-only code must live under an Editor assembly or Editor folder.
- Prefer interfaces and events for cross-system interaction.
- Prefer serialized references, installers, or explicit composition over scene-wide searches.
- Do not use `GameObject.Find`, `FindObjectOfType`, object names, or tags as core architecture.
- Avoid global mutable singletons.
- Do not create a service locator in the prototype.
- Use `ScriptableObject` only for stable configuration data, not mutable scene state.
- Do not modify package dependencies unless the current task explicitly requires it.
- Do not add third-party packages without explicit approval.
- Do not manually edit complex Unity scene or prefab YAML when an Editor script can create or wire the asset safely.
- Keep gameplay constants configurable in the Inspector or configuration assets.
- Use `FixedUpdate` for physics movement and `Update` for input collection where applicable.
- Ensure event subscriptions are paired with unsubscriptions.
- Support scene restart without stale static state.
- Treat warnings, null references, and unhandled exceptions as failures.

## Scope restrictions

Until Milestone 1 is accepted, do not add:

- Fire, ice, electric, poison, explosion, water, or special corpse types.
- Dash, double jump, wall jump, climbing, ledge grabbing, combat, inventory, or skill trees.
- Save systems, level selection, achievements, localization, or analytics.
- Final art, final animation, final audio, shaders, post-processing, or camera polish.
- Procedural generation, multiplayer, online services, ECS/DOTS, Addressables, or dependency injection frameworks.

Do not introduce a new gameplay mechanic unless the current task explicitly requires it.

## Required first-loop concepts

Use explicit concepts similar to:

- `DeathType`
- `IDamageSource`
- `DeathContext`
- `PlayerLifeController`
- `RespawnController`
- `CorpseController`
- `IWeightedObject`
- `PressurePlate`
- `DoorController`
- `LevelSession`
- `LevelExit`

Names may change if the architecture plan gives a clear reason.

## Test expectations

Prefer Edit Mode tests for:

- life count;
- state transitions;
- weight calculation;
- pressure threshold;
- door state;
- restart/reset behavior.

Prefer Play Mode tests for:

- hazard collision;
- death and respawn;
- corpse creation and persistence;
- corpse pressure-plate interaction;
- level completion.

Tests must validate behavior, not private implementation details.

Do not delete, disable, ignore, or weaken a failing test merely to obtain a green result.

## Automation

Use the Unity executable from the `UNITY_EDITOR_PATH` environment variable when available.

Expected command-line capabilities:

- compile/validate project;
- run Edit Mode tests;
- run Play Mode tests;
- build a Windows x64 Development Player;
- write logs and XML results to project-local output folders.

When batch validation cannot run because the project is open in Unity, state that clearly and perform every other available check. Do not claim a test passed if it was not executed.

## Definition of done

A task is complete only when:

- the requested behavior exists;
- the code compiles, or the exact validation blocker is reported;
- relevant tests exist and pass when executable;
- no unrelated feature was introduced;
- no known exception occurs in the intended flow;
- the diff has been reviewed;
- changed files and validation results are summarized.

## Git

- Do not overwrite or discard user changes.
- Keep commits focused.
- Do not commit generated `Library`, `Temp`, `Logs`, `Builds`, or IDE cache files.
- Before a large change, recommend a checkpoint if the working tree is not clean.
- Never force-push or rewrite shared history unless explicitly requested.
