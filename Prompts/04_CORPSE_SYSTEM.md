# Codex Task 04 — Persistent Normal Corpse

## Goal

Create one normal physical corpse from the player's death context and keep it in the level across ordinary respawns.

## Context

Task 03 emits or exposes a validated death context.

The normal corpse is the first persistent level mechanism.

## Constraints

- Only a normal corpse.
- The corpse is a separate gameplay object, not the disabled active player.
- Do not clone the full player hierarchy with unnecessary runtime components.
- Do not retain input, camera, life, or respawn components on a corpse.
- Do not add ragdoll complexity.
- Do not add special corpse properties.
- Avoid unstable physics that causes the corpse to roll or jitter excessively.
- Corpse cleanup occurs on level restart.
- Do not use object names or tags as identity architecture.

## Required behavior

- spawn at or near the validated death position;
- use a dedicated prefab or safe factory-created object;
- have Rigidbody2D and Collider2D behavior appropriate for greybox play;
- support standing on it;
- support future `IWeightedObject`;
- persist after the active player respawns;
- register with the level session for reset/cleanup;
- expose configurable mass/weight and collision settings;
- visibly distinguish active player and corpse using greybox colors or simple sprites.

## Tests

Verify:

- exactly one corpse is created per accepted death;
- corpse position is within tolerance of the death position;
- corpse remains after respawn;
- corpse has no active-player control;
- restart removes all runtime-created corpses;
- multiple deaths create multiple corpses within the current life limit.

## Done when

- the player can die, respawn, and stand on the prior corpse;
- tests pass;
- no special corpse state is introduced;
- `Docs/CURRENT_STATUS.md` is updated after validation.
