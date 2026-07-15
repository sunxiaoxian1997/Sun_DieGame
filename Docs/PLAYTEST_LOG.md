# Playtest Log

Use one copy of the session record below for each tester. Preserve raw observations before
changing tuning values. Do not explain the intended solution until the run has ended.

## Build and setup

- Build commit: `<full commit SHA or working-tree snapshot identifier>`
- Build date:
- Unity version:
- Windows build path:
- Observer:
- Test environment and controller/keyboard:
- Tuning changes from the baseline table below:

## Tester

- Tester identifier:
- First time seeing this game: `Yes / No`
- Prior puzzle-platformer experience (optional):
- Received any gameplay hint before completion: `Yes / No`

## Session results

- Started at:
- Finished or stopped at:
- First completion time (`mm:ss`, blank if not completed):
- Death count:
- Restart count:
- Completed without hints: `Yes / No / Not completed`
- Final result: `Completed / Failed / Stopped`

## Observed behavior path

Record actions in order, including failed hypotheses. Describe what the player did rather than
what the observer expected.

1.
2.
3.

## Findings

### Confusion points

-

### Physics problems

Record corpse bounce, sliding, jitter, penetration, missed plate detection, or unstable landings
with the approximate location and reproduction steps.

-

### Unintended solutions

Distinguish valid systemic solutions from technical exploits such as passing through or jumping
over the closed door.

-

### Suggested changes

-

## Observer conclusion

- Decision: `Keep / Modify / Remove`
- What should be kept:
- What should be modified:
- What should be removed:
- Evidence for the decision:
- Follow-up build or issue:

## Level_001 tuning reference

These values are already editable in existing Prefabs or the scene. No additional configuration
asset is used. Keep the gameplay weight of both Player and NormalCorpse at `1`; corpse `Mass` is
physical mass and does not change the pressure-plate weight rule.

| Parameter | Baseline | Inspector location | Builder source |
| --- | ---: | --- | --- |
| Player movement speed | `5` | `Assets/_Game/Prefabs/Player.prefab` > Player > Player Motor 2D > Move Speed | `PlayerPrototypeBuilder.ConfigurePrefab` |
| Player jump speed | `7` | `Assets/_Game/Prefabs/Player.prefab` > Player > Player Motor 2D > Jump Speed | `PlayerPrototypeBuilder.ConfigurePrefab` |
| Player gravity | `2.5` | `Assets/_Game/Prefabs/Player.prefab` > Player > Rigidbody 2D > Gravity Scale | `PlayerPrototypeBuilder.ConfigurePrefab` |
| GroundProbe distance | `0.08` | `Assets/_Game/Prefabs/Player.prefab` > Player > Ground Probe 2D > Cast Distance | `PlayerPrototypeBuilder.ConfigurePrefab` |
| Respawn delay | `0.65 s` | `Assets/_Game/Scenes/Level_001.unity` > LevelRoot/LevelSystems > Respawn Controller > Respawn Delay | `FirstLevelBuilder.ConfigureScene` |
| Corpse mass | `1.5` | `Assets/_Game/Prefabs/NormalCorpse.prefab` > NormalCorpse > Rigidbody 2D > Mass | `CorpsePrototypeBuilder.ConfigurePrefab` |
| Corpse gravity | `2.5` | `Assets/_Game/Prefabs/NormalCorpse.prefab` > NormalCorpse > Rigidbody 2D > Gravity Scale | `CorpsePrototypeBuilder.ConfigurePrefab` |
| PressurePlate threshold | `1` | `Assets/_Game/Prefabs/PressurePlate.prefab` > PressurePlate > Pressure Plate > Activation Threshold | `InteractionPrototypeBuilder.ConfigurePressurePlate` |
| Door open offset | `(0, 4, 0)` | `Assets/_Game/Prefabs/Door.prefab` > Door > Door Controller > Open Local Offset | `InteractionPrototypeBuilder.ConfigureDoor`; scene wiring repeats the value in `FirstLevelBuilder.ConfigureScene` |
| Camera orthographic size | `5.5` | `Assets/_Game/Scenes/Level_001.unity` > LevelRoot/LevelCamera > Camera > Size | `FirstLevelBuilder.ConfigureCamera` |

### Tuning workflow and Builder safety

1. Create a commit or record the exact working-tree snapshot before a playtest build.
2. Adjust the source Prefab for Player, NormalCorpse, PressurePlate, or Door values. Adjust
   LevelSystems and LevelCamera directly in `Level_001` for the two scene-owned values.
3. Build and record every deviation from the baseline in the session record.
4. Do not run `Corpse Mechanism/Level/Create or Update Level 001` after manual tuning: the current
   Builder intentionally restores its code-defined baseline and will overwrite these values.
5. If a tuning candidate is accepted, update the matching Builder baseline and generated asset
   together, then run the Builder twice and compare hashes to reconfirm idempotency.

The parameters are directly editable in their existing assets, so a separate Editor configuration
asset would duplicate state and is not justified for this milestone task.
