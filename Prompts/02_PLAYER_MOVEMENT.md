# Codex Task 02 — Basic 2D Player Movement

## Goal

Implement a small, configurable 2D player controller that supports left/right movement and one jump.

## Context

Read the repository guidance and current architecture.

This controller will later participate in death, corpse creation, and respawn, but those systems are not part of this task.

## Constraints

- Use the project's existing input system.
- Collect input separately from physics application where appropriate.
- Keep Rigidbody2D movement deterministic enough for tests.
- Ground detection must not depend on object names.
- Expose movement speed, jump impulse/height, ground layers, and ground-check geometry.
- Provide a way to enable/disable control for future death handling.
- Do not add dash, double jump, coyote time, jump buffering, wall movement, combat, or animation state machines.
- Do not add third-party packages.
- Do not create a global player singleton.

## Required behavior

- move left and right;
- stop or decelerate according to the documented controller choice;
- jump only while grounded;
- stable ground detection;
- no repeated jump from held input unless explicitly designed and tested;
- debug visualization for ground check;
- a greybox test scene or bootstrap integration.

## Tests

Add relevant Edit Mode tests for pure calculations if extracted.

Add Play Mode tests for:

- horizontal movement;
- grounded jump;
- inability to jump repeatedly while airborne;
- disabling control.

## Done when

- behavior works in the greybox scene;
- relevant tests pass;
- inspector fields are documented with tooltips where useful;
- no unrelated gameplay system is introduced;
- `Docs/CURRENT_STATUS.md` is updated after validation.
