# Codex Task 06 — First Complete Greybox Level

## Goal

Build and validate the first complete puzzle level demonstrating the game's core promise.

## Level statement

“The player realizes that dying on the pressure plate allows the next life to pass through the door.”

## Required flow

```text
spawn
→ learn movement
→ see closed door and nearby pressure plate
→ encounter normal hazard
→ die with body positioned on the plate
→ corpse remains
→ respawn
→ door remains open because the corpse supplies weight
→ pass through the door
→ reach the exit
```

## Constraints

- Maximum 3 lives.
- Only normal death and normal corpse.
- Use greybox geometry and simple colors.
- Do not add dialogue, story, final UI, special effects, extra corpse states, enemies, moving platforms, keys, or optional routes.
- The intended solution must be readable without a long text tutorial.
- Do not hand-edit complex scene YAML; use Unity Editor tooling or safe scene APIs.
- Keep the scene small enough to understand in one screen or a short camera movement.

## Required implementation

- complete scene;
- spawn;
- player;
- normal hazard;
- corpse generation;
- pressure plate;
- door;
- exit;
- minimal lives/failure/restart display or debug UI;
- visual feedback for plate, door, death, respawn, completion, and failure;
- scene added to build configuration;
- level validation checks for required references.

## Automated tests

Add a Play Mode integration test that programmatically verifies the main state chain where practical:

- death accepted;
- corpse created;
- respawn completed;
- corpse activates plate;
- door opens;
- exit can complete the level.

Do not create a brittle test that depends on exact frame timing or pixel-perfect transforms.

## Manual playtest checklist

Report:

- time to first successful solution;
- whether the hazard location makes intentional death possible;
- whether accidental corpse movement can break the puzzle;
- whether the player can become permanently stuck;
- whether restart always restores the scene;
- any non-intended but rule-consistent solutions.

## Done when

- a human can complete the intended loop;
- automated checks pass;
- Windows Development Build launches into the level;
- no console exception occurs;
- `Docs/CURRENT_STATUS.md` records verified status and remaining design concerns.
