# Codex Task 05 — Weight, Pressure Plate, and Door

## Goal

Implement a reusable weight contract, pressure plate, and linked door so either the active player or a normal corpse can activate the plate.

## Context

The active player and normal corpse exist.

## Constraints

- Use `IWeightedObject` or an equally explicit contract.
- Do not identify valid objects by name.
- Do not hard-code Player or Corpse component checks inside the pressure plate when an interface can express the rule.
- Correctly handle multiple colliders belonging to one weighted object.
- Correctly handle enter, stay/reconciliation if needed, exit, disable, destruction, and scene restart.
- Do not make the pressure plate search the whole scene.
- Door motion may be immediate or simple; avoid animation systems.
- No crushing damage or advanced door behavior.

## Required behavior

- configurable pressure threshold;
- total unique weight calculation;
- pressed/unpressed state;
- clear greybox feedback;
- door opens while linked activation condition is true;
- door closes when activation is removed;
- active player and corpse both contribute weight;
- multiple colliders do not double count;
- destroyed or disabled weighted objects are removed safely.

## Tests

Verify:

- player can activate the plate;
- corpse can activate the plate;
- insufficient weight does not activate it;
- combined weight can activate it if configured;
- one object with multiple colliders is counted once;
- removing the object deactivates the plate;
- door follows the plate state;
- restart restores default state.

## Done when

- the corpse can keep a door open while the new player moves away;
- tests pass;
- `Docs/CURRENT_STATUS.md` is updated after validation.
