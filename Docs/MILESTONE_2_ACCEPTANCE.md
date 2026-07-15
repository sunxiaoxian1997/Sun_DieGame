# Milestone 2 Acceptance

This gate evaluates the existing Level_001 core interaction. It does not authorize another level,
another corpse type, or any new player ability.

## Test protocol

1. Produce a Windows Development Build from an identified commit or recorded working-tree
   snapshot.
2. Use at least three testers who have not been told the intended solution. Prefer testers who have
   not seen this project before.
3. Give only the existing controls: move, jump, and restart. Do not explain the pressure plate,
   door, death, corpse persistence, or solution path.
4. Start timing when the tester first gains control. Stop at the first `Completed` state.
5. Record each run in `Docs/PLAYTEST_LOG.md`, including incomplete runs and all observer prompts.
6. Run the automated validation suite and a fresh Development Build for the candidate being judged.

## Per-tester evidence

| Tester | First exposure | First completion time | Deaths | Restarts | No-hint completion | Understood plate-door relationship | Discovered voluntary-death solution | Notes/log link |
| --- | --- | ---: | ---: | ---: | --- | --- | --- | --- |
| Tester 1 |  |  |  |  |  |  |  |  |
| Tester 2 |  |  |  |  |  |  |  |  |
| Tester 3 |  |  |  |  |  |  |  |  |

Do not mark Milestone 2 accepted until all three rows contain real observations. Add rows rather
than replacing failed or incomplete attempts.

## Acceptance checklist

### Comprehension and discovery

- [ ] Without hints, players can understand that the pressure plate controls the door.
- [ ] Without hints, players can discover that voluntary death is the intended way to leave weight
      on the plate.
- [ ] The first completion time is recorded for every tester who completes the level.
- [ ] At least three tester results are recorded, including failures and hints if they occurred.

### Corpse and pressure-plate stability

- [ ] A corpse created by the standard Level_001 solution consistently falls into the pressure-plate
      area.
- [ ] Ordinary contact from the respawned player does not easily push the resting corpse off the
      pressure plate.
- [ ] The pressure plate remains pressed while the valid corpse rests in its detection area.
- [ ] No severe corpse jitter, tunnelling, penetration, or unstable repeated bouncing is observed.

### Door, route, and restart integrity

- [ ] The closed door physically blocks the player.
- [ ] The player cannot pass through, jump over, or otherwise bypass the closed door.
- [ ] The standard Level_001 solution remains: move and jump, die on the plate, leave one normal
      corpse, respawn, pass the open door, and reach the exit.
- [ ] Restart works from Playing, Failed, and Completed and restores three lives, no runtime corpses,
      a released pressure plate, a closed door, and a Playing session.

### Automated release checks

- [ ] Project Validation passes.
- [ ] All 46 Edit Mode tests pass.
- [ ] All 47 Play Mode tests pass.
- [ ] Windows x64 Development Build succeeds.
- [ ] Running the Level_001 Builder twice produces stable tracked-asset hashes.
- [ ] No warning, null reference, unhandled exception, or missing reference occurs in the intended
      playtest flow.

## Go / No-Go decision for a second corpse mechanism

Choose exactly one outcome after reviewing all playtest logs.

- [ ] **Go** - continue to select and design a second corpse mechanism.
- [ ] **No-Go** - do not add a second corpse mechanism; continue tuning, redesign the first loop, or
      stop the prototype.

Decision date:

Decision owner:

Evidence summary:

- Number of testers:
- No-hint completions:
- First-completion-time range:
- Repeated confusion pattern:
- Repeated physics failure pattern:
- Unintended solutions and whether they are valid systemic behavior or defects:
- Why the evidence supports Go or No-Go:

## Non-negotiable rules during tuning

- Initial lives remain `3`.
- Every accepted death creates exactly one normal corpse.
- The third death enters `Failed`.
- Player and corpse gameplay weight remain `1`.
- PressurePlate default threshold remains `1`.
- Restart reloads the active scene.
- No new mechanic, content type, formal UI, telemetry, package, animation, audio, or effect may be
  added under this acceptance task.
