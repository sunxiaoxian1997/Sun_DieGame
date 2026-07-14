# Technical Architecture

## Architectural objective

Create a small but extensible prototype without prematurely building a framework.

## Suggested modules

```text
CorpseMechanism.Core
CorpseMechanism.Player
CorpseMechanism.Death
CorpseMechanism.Corpse
CorpseMechanism.Interaction
CorpseMechanism.Level
CorpseMechanism.UI
CorpseMechanism.Editor
CorpseMechanism.Tests.EditMode
CorpseMechanism.Tests.PlayMode
```

The exact assembly split may be reduced if it adds unnecessary friction. Runtime and Editor code must remain separated.

## Recommended event flow

```text
Hazard detects player
→ emits damage/death request
→ player life controller enters Dead state
→ death context is created
→ corpse factory creates corpse
→ level session spends one life
→ respawn controller creates or reactivates active player
→ prior corpse remains
```

## Scene composition

The first playable scene should contain explicit references for:

- level session;
- spawn point;
- player prefab or player instance;
- hazard;
- corpse factory;
- pressure plate;
- door;
- exit.

Do not depend on object names for discovery.

## State ownership

- Player state belongs to the active player.
- Remaining lives and spawned-corpse registry belong to the current level session.
- Pressure state belongs to the pressure plate.
- Door state belongs to the door.
- Restart/reset orchestration belongs to the level/session layer.
- Configuration belongs in serialized fields or small immutable configuration assets.

## Automation

Create an Editor-only `ProjectAutomation` class with public static entry points for:

- project validation;
- Windows x64 Development Build.

Unity Test Framework commands are invoked directly through command-line arguments for Edit Mode and Play Mode tests.

## Error policy

- Fail fast on invalid required configuration.
- Use useful validation messages in the Editor.
- Do not silently recover from missing critical references.
- Avoid log spam during normal physics updates.
