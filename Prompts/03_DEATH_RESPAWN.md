# Codex Task 03 — Normal Death, Lives, and Respawn

## Goal

Implement the active player's normal death and limited-life respawn loop without creating a corpse yet.

## Context

The player controller from Task 02 exists.

This task establishes the contracts that Task 04 will use to create a corpse from a death context.

## Constraints

- Only `DeathType.Normal`.
- Do not create fire, ice, electric, poison, explosion, water, or other states.
- Do not reload the scene for every ordinary respawn.
- Restart may reload or reset the scene, but the chosen behavior must be documented.
- Avoid static mutable state.
- Do not create a global singleton.
- Do not add final UI or animation.
- Death must be idempotent: repeated contacts in the same death transition must not spend multiple lives.

## Required implementation

- `DeathType.Normal`;
- `IDamageSource` or equivalent explicit hazard contract;
- immutable or read-only `DeathContext`;
- player alive/dead state;
- input and physics handling during death;
- configurable respawn delay;
- spawn/checkpoint reference;
- remaining lives;
- level-failed state;
- restart command/API;
- events needed by the future corpse system;
- a normal spike/hazard implementation.

## Tests

Verify:

- one hazard contact produces one death;
- one death spends one life;
- the player respawns at the configured point;
- controls are restored after respawn;
- zero remaining lives enters failure;
- restart restores initial lives and active-player state;
- repeated damage while dead has no extra effect.

## Done when

- the full no-corpse death/respawn loop works;
- tests pass;
- no corpse object is created yet;
- `Docs/CURRENT_STATUS.md` is updated after validation.
