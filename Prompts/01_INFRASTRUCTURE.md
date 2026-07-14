# Codex Task 01 — Project Infrastructure

## Goal

Create the minimum repository structure, assembly boundaries, Unity automation, test entry points, and greybox bootstrap needed to implement Milestone 1 safely.

## Context

Read `AGENTS.md`, `README_START_HERE.md`, all `Docs/`, and the result of Task 00.

Use the existing Unity version and existing input setup.

## Constraints

- Do not implement player movement, death, corpses, pressure plates, doors, or the final puzzle yet.
- Do not add third-party packages.
- Keep Runtime code independent of UnityEditor.
- Do not hand-edit complex scene YAML.
- Use an Editor script to create or validate bootstrap assets when appropriate.
- Keep assembly definitions as simple as possible.
- Preserve existing user files and architecture unless the audit identified a concrete conflict.
- Generated logs, test results, and builds must not be committed.

## Required implementation

Create or adapt:

1. `Assets/_Game/Runtime/`
2. `Assets/_Game/Editor/`
3. `Assets/_Game/Tests/EditMode/`
4. `Assets/_Game/Tests/PlayMode/`
5. `Assets/_Game/Scenes/`
6. Runtime, Editor, and test assembly definitions.
7. An Editor-only `ProjectAutomation` class with:
   - a public static project validation entry point;
   - a public static Windows x64 Development Build entry point;
   - clear non-zero/failing behavior on build failure.
8. A minimal bootstrap scene or an Editor generator for it.
9. Update the provided PowerShell scripts if required by the real namespace/method names.
10. A minimal smoke test in Edit Mode and Play Mode proving the assemblies execute.
11. `.gitignore` updates for Unity-generated folders, Logs, TestResults, and Builds.

## Validation

When possible:

- compile the project;
- run Edit Mode tests;
- run Play Mode tests;
- invoke project validation;
- create a Windows x64 Development Build.

If the Unity project is open and blocks Batch Mode, report that exact blocker and run all remaining static checks.

## Done when

- the project compiles;
- test assemblies are discoverable;
- smoke tests pass;
- automation commands are documented and executable;
- a Windows development build succeeds when the environment allows it;
- no gameplay mechanic beyond infrastructure is added;
- `Docs/CURRENT_STATUS.md` is updated with verified results.
