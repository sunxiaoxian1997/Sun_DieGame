# Codex Task 00 — Project Audit and Plan Only

## Goal

Audit this Unity repository and produce a concrete implementation plan for Milestone 0 and Milestone 1.

Do not modify, create, delete, move, or format any file in this task.

## Context

Read:

- `AGENTS.md`
- `README_START_HERE.md`
- every file under `Docs/`
- `Packages/manifest.json`
- `ProjectSettings/ProjectVersion.txt`
- relevant existing scripts and assembly definitions.

The first target loop is:

```text
move → jump → die on hazard → create normal corpse → respawn
→ corpse presses pressure plate → door opens → reach exit
```

## Constraints

- This is a Unity 2D prototype.
- Preserve the current Unity version.
- Preserve the current input system unless there is a blocking reason.
- Do not propose third-party packages.
- Do not add special corpse states.
- Do not design final art, story, save systems, combat, or procedural levels.
- Do not silently assume the project currently compiles.
- Separate facts found in the repository from recommendations.

## Required audit

Report:

1. Unity version.
2. Current render/template setup.
3. Current input system.
4. Current package dependencies relevant to the prototype.
5. Existing code, scene, prefab, test, and assembly structure.
6. Current compilation risks visible from static inspection.
7. Whether version-control settings appear suitable.
8. Whether an existing project architecture should be preserved.
9. Exact proposed files and directories for Milestone 0.
10. Exact proposed task sequence for Milestone 1.
11. Validation commands that can run on this Windows machine.
12. Risks, assumptions, and decisions that require user attention.

## Done when

Return:

- repository findings;
- an ordered implementation plan;
- an architecture sketch;
- a proposed test strategy;
- a file creation/change list;
- no repository changes.
