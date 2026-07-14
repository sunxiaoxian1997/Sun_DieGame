# Core Rules

## Current rules

1. The player has a finite number of lives in a level.
2. A valid hazard can kill the active player.
3. Death records a death context, including position and death type.
4. A normal corpse is created at the death position.
5. The corpse persists through player respawn.
6. The active player respawns at the current spawn point.
7. Player and corpse can both provide physical weight.
8. A pressure plate activates when total valid weight reaches its threshold.
9. An active pressure plate opens its linked door.
10. The active player completes the level by reaching the exit.
11. Restarting the level clears spawned corpses and restores the initial state.

## Current death types

Only:

```text
Normal
```

No other death type is authorized in Milestone 1.

## Current corpse behavior

A normal corpse:

- has a stable physical body;
- has collision;
- can be stood on;
- provides configurable weight;
- can rest on a pressure plate;
- persists until level restart;
- has no fire, ice, electricity, poison, explosion, floating, or decay behavior.
