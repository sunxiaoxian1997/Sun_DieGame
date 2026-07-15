using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CorpseMechanism.Corpse;
using CorpseMechanism.Death;
using CorpseMechanism.Interaction;
using CorpseMechanism.Level;
using CorpseMechanism.Player;
using CorpseMechanism.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorpseMechanism.Editor
{
    public static class ProjectAutomation
    {
        private const string MenuRoot = "Corpse Mechanism/Infrastructure/";
        private const string WindowsBuildPath = "Builds/Windows/DIEGAME.exe";

        private static readonly string[] RequiredDirectories =
        {
            "Assets/_Game/Runtime",
            "Assets/_Game/Editor",
            "Assets/_Game/Runtime/Corpse",
            "Assets/_Game/Runtime/Death",
            "Assets/_Game/Runtime/Interaction",
            "Assets/_Game/Runtime/Level",
            "Assets/_Game/Runtime/Player",
            "Assets/_Game/Runtime/UI",
            "Assets/_Game/Prefabs",
            "Assets/_Game/Tests/EditMode",
            "Assets/_Game/Tests/PlayMode",
            "Assets/_Game/Scenes"
        };

        private static readonly string[] RequiredAssemblyDefinitions =
        {
            "Assets/_Game/Runtime/CorpseMechanism.Runtime.asmdef",
            "Assets/_Game/Editor/CorpseMechanism.Editor.asmdef",
            "Assets/_Game/Tests/EditMode/CorpseMechanism.Tests.EditMode.asmdef",
            "Assets/_Game/Tests/PlayMode/CorpseMechanism.Tests.PlayMode.asmdef"
        };

        [MenuItem(MenuRoot + "Validate Project")]
        public static void ValidateProject()
        {
            IReadOnlyList<string> errors = CollectValidationErrors();
            if (errors.Count > 0)
            {
                ThrowValidationFailure(errors);
            }

            Debug.Log("Project validation succeeded.");
        }

        [MenuItem(MenuRoot + "Build Windows Development")]
        public static void BuildWindowsDevelopment()
        {
            ValidateProject();

            string[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            string absoluteBuildPath = Path.GetFullPath(
                Path.Combine(GetProjectRootPath(), WindowsBuildPath));
            string buildDirectory = Path.GetDirectoryName(absoluteBuildPath);
            if (string.IsNullOrEmpty(buildDirectory))
            {
                throw new BuildFailedException(
                    $"Could not determine build directory for '{absoluteBuildPath}'.");
            }

            Directory.CreateDirectory(buildDirectory);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = enabledScenes,
                locationPathName = absoluteBuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Windows Development Build failed with result {summary.result}. " +
                    $"Errors: {summary.totalErrors}, warnings: {summary.totalWarnings}.");
            }

            if (summary.totalErrors > 0 || summary.totalWarnings > 0)
            {
                throw new BuildFailedException(
                    "Windows Development Build produced diagnostics. " +
                    $"Errors: {summary.totalErrors}, warnings: {summary.totalWarnings}.");
            }

            Debug.Log(
                $"Windows Development Build succeeded: {absoluteBuildPath} " +
                $"({summary.totalSize} bytes, {summary.totalTime}).");
        }

        private static IReadOnlyList<string> CollectValidationErrors()
        {
            List<string> errors = new List<string>();

            ValidateRequiredPaths(errors);
            ValidateRuntimeAssemblyBoundary(errors);
            ValidateBuildSettings(errors);
            ValidatePlayerPrefab(errors);
            ValidateNormalHazardPrefab(errors);
            ValidateNormalCorpsePrefab(errors);
            ValidatePressurePlatePrefab(errors);
            ValidateDoorPrefab(errors);
            ValidateLevelExitPrefab(errors);
            ValidateAllPrefabMissingScripts(errors);
            ValidateBootstrapScene(errors);
            ValidateFirstLevelScene(errors);

            return errors;
        }

        private static void ValidateRequiredPaths(ICollection<string> errors)
        {
            foreach (string directory in RequiredDirectories)
            {
                if (!AssetDatabase.IsValidFolder(directory))
                {
                    errors.Add($"Required directory is missing: {directory}");
                }
            }

            foreach (string assemblyDefinition in RequiredAssemblyDefinitions)
            {
                if (AssetDatabase.LoadAssetAtPath<TextAsset>(assemblyDefinition) == null)
                {
                    errors.Add($"Required assembly definition is missing: {assemblyDefinition}");
                }
            }
        }

        private static void ValidateRuntimeAssemblyBoundary(ICollection<string> errors)
        {
            const string runtimeAssemblyPath =
                "Assets/_Game/Runtime/CorpseMechanism.Runtime.asmdef";

            string projectRootPath = GetProjectRootPath();
            string absolutePath = Path.GetFullPath(
                Path.Combine(projectRootPath, runtimeAssemblyPath));

            if (!File.Exists(absolutePath))
            {
                return;
            }

            string contents = File.ReadAllText(absolutePath);
            if (contents.IndexOf("UnityEditor", StringComparison.Ordinal) >= 0 ||
                contents.IndexOf("\"Editor\"", StringComparison.Ordinal) >= 0)
            {
                errors.Add("Runtime assembly definition must not reference or target UnityEditor.");
            }

            string runtimeSourceDirectory = Path.Combine(
                projectRootPath,
                "Assets",
                "_Game",
                "Runtime");

            foreach (string sourceFile in Directory.GetFiles(
                runtimeSourceDirectory,
                "*.cs",
                SearchOption.AllDirectories))
            {
                string sourceContents = File.ReadAllText(sourceFile);
                if (sourceContents.IndexOf("UnityEditor", StringComparison.Ordinal) >= 0)
                {
                    errors.Add(
                        "Runtime source must not reference UnityEditor: " +
                        sourceFile.Substring(projectRootPath.Length + 1));
                }

                if (sourceContents.IndexOf("UnityEngine.InputSystem", StringComparison.Ordinal) >= 0)
                {
                    errors.Add(
                        "Runtime source must not reference the new Input System: " +
                        sourceFile.Substring(projectRootPath.Length + 1));
                }
            }
        }

        private static void ValidatePlayerPrefab(ICollection<string> errors)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PlayerPrototypeBuilder.PrefabPath);

            if (prefab == null)
            {
                errors.Add($"Player prefab is missing: {PlayerPrototypeBuilder.PrefabPath}");
                return;
            }

            if (prefab.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                prefab.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                errors.Add("Player prefab must not contain 3D physics components.");
            }

            Rigidbody2D[] bodies = prefab.GetComponentsInChildren<Rigidbody2D>(true);
            Collider2D[] colliders = prefab.GetComponentsInChildren<Collider2D>(true);
            LegacyPlayerInputSource[] inputs =
                prefab.GetComponentsInChildren<LegacyPlayerInputSource>(true);
            GroundProbe2D[] probes = prefab.GetComponentsInChildren<GroundProbe2D>(true);
            PlayerMotor2D[] motors = prefab.GetComponentsInChildren<PlayerMotor2D>(true);
            PlayerLifeController[] lifeControllers =
                prefab.GetComponentsInChildren<PlayerLifeController>(true);
            SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            WeightProvider[] weights = prefab.GetComponentsInChildren<WeightProvider>(true);

            if (bodies.Length != 1 || colliders.Length != 1 || inputs.Length != 1 ||
                probes.Length != 1 || motors.Length != 1 || lifeControllers.Length != 1 ||
                renderers.Length != 1 || weights.Length != 1)
            {
                errors.Add(
                    "Player prefab must contain exactly one Rigidbody2D, Collider2D, " +
                    "LegacyPlayerInputSource, GroundProbe2D, PlayerMotor2D, " +
                    "PlayerLifeController, SpriteRenderer, and WeightProvider.");
                return;
            }

            if (weights[0].Weight <= 0f)
            {
                errors.Add("Player WeightProvider must have positive weight.");
            }

            if ((bodies[0].constraints & RigidbodyConstraints2D.FreezeRotation) == 0)
            {
                errors.Add("Player Rigidbody2D must freeze Z rotation.");
            }

            SerializedObject serializedMotor = new SerializedObject(motors[0]);
            string[] requiredMotorReferences =
            {
                "_body",
                "_groundProbe",
                "_inputSourceComponent"
            };

            foreach (string propertyName in requiredMotorReferences)
            {
                SerializedProperty property = serializedMotor.FindProperty(propertyName);
                if (property == null || property.objectReferenceValue == null)
                {
                    errors.Add(
                        $"PlayerMotor2D prefab reference is missing: {propertyName}");
                }
            }

            SerializedObject serializedLife = new SerializedObject(lifeControllers[0]);
            string[] requiredLifeReferences =
            {
                "_motor",
                "_body",
                "_bodyCollider",
                "_groundProbe",
                "_renderer"
            };

            foreach (string propertyName in requiredLifeReferences)
            {
                SerializedProperty property = serializedLife.FindProperty(propertyName);
                if (property == null || property.objectReferenceValue == null)
                {
                    errors.Add(
                        $"PlayerLifeController prefab reference is missing: {propertyName}");
                }
            }
        }

        private static void ValidateNormalHazardPrefab(ICollection<string> errors)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DeathPrototypeBuilder.HazardPrefabPath);
            if (prefab == null)
            {
                errors.Add(
                    $"Normal hazard prefab is missing: {DeathPrototypeBuilder.HazardPrefabPath}");
                return;
            }

            if (prefab.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                prefab.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                errors.Add("Normal hazard prefab must not contain 3D physics components.");
            }

            NormalHazard[] hazards = prefab.GetComponentsInChildren<NormalHazard>(true);
            Collider2D[] colliders = prefab.GetComponentsInChildren<Collider2D>(true);
            SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            if (hazards.Length != 1 || colliders.Length != 1 || renderers.Length != 1)
            {
                errors.Add(
                    "Normal hazard prefab must contain exactly one NormalHazard, " +
                    "Collider2D, and SpriteRenderer.");
            }
            else if (!colliders[0].isTrigger)
            {
                errors.Add("Normal hazard prefab Collider2D must be a trigger.");
            }
        }

        private static void ValidateNormalCorpsePrefab(ICollection<string> errors)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CorpsePrototypeBuilder.PrefabPath);
            if (prefab == null)
            {
                errors.Add($"Normal corpse prefab is missing: {CorpsePrototypeBuilder.PrefabPath}");
                return;
            }

            if (prefab.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                prefab.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                errors.Add("Normal corpse prefab must not contain 3D physics components.");
            }

            CorpseController[] corpses = prefab.GetComponentsInChildren<CorpseController>(true);
            Rigidbody2D[] bodies = prefab.GetComponentsInChildren<Rigidbody2D>(true);
            Collider2D[] colliders = prefab.GetComponentsInChildren<Collider2D>(true);
            SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            WeightProvider[] weights = prefab.GetComponentsInChildren<WeightProvider>(true);
            if (corpses.Length != 1 || bodies.Length != 1 ||
                colliders.Length < 1 || renderers.Length != 1 || weights.Length != 1)
            {
                errors.Add(
                    "Normal corpse prefab must contain exactly one CorpseController, " +
                    "Rigidbody2D, SpriteRenderer, and WeightProvider plus at least one Collider2D.");
            }

            else if (weights[0].Weight <= 0f)
            {
                errors.Add("Normal corpse WeightProvider must have positive weight.");
            }

            if (bodies.Length == 1 &&
                (bodies[0].constraints & RigidbodyConstraints2D.FreezeRotation) == 0)
            {
                errors.Add("Normal corpse Rigidbody2D must freeze Z rotation.");
            }

            if (prefab.GetComponentsInChildren<LegacyPlayerInputSource>(true).Length > 0 ||
                prefab.GetComponentsInChildren<PlayerMotor2D>(true).Length > 0 ||
                prefab.GetComponentsInChildren<PlayerLifeController>(true).Length > 0 ||
                prefab.GetComponentsInChildren<GroundProbe2D>(true).Length > 0 ||
                prefab.GetComponentsInChildren<RespawnController>(true).Length > 0 ||
                prefab.GetComponentsInChildren<Camera>(true).Length > 0 ||
                prefab.GetComponentsInChildren<AudioListener>(true).Length > 0)
            {
                errors.Add("Normal corpse prefab contains an active-player or scene component.");
            }
        }

        private static void ValidatePressurePlatePrefab(ICollection<string> errors)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                InteractionPrototypeBuilder.PressurePlatePrefabPath);
            if (prefab == null)
            {
                errors.Add("PressurePlate prefab is missing.");
                return;
            }

            if (prefab.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                prefab.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                errors.Add("PressurePlate prefab must not contain 3D physics components.");
            }

            PressurePlate[] plates = prefab.GetComponentsInChildren<PressurePlate>(true);
            Collider2D[] triggers = prefab.GetComponentsInChildren<Collider2D>(true)
                .Where(collider => collider.isTrigger)
                .ToArray();
            if (plates.Length != 1 || triggers.Length < 1 ||
                plates[0].ActivationThreshold <= 0f)
            {
                errors.Add(
                    "PressurePlate prefab requires one PressurePlate, a 2D trigger, " +
                    "and a positive threshold.");
            }

            if (prefab.GetComponentsInChildren<PlayerLifeController>(true).Length > 0 ||
                prefab.GetComponentsInChildren<CorpseController>(true).Length > 0 ||
                prefab.GetComponentsInChildren<DoorController>(true).Length > 0 ||
                prefab.GetComponentsInChildren<LevelSession>(true).Length > 0)
            {
                errors.Add("PressurePlate prefab contains a forbidden gameplay component.");
            }
        }

        private static void ValidateDoorPrefab(ICollection<string> errors)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                InteractionPrototypeBuilder.DoorPrefabPath);
            if (prefab == null)
            {
                errors.Add("Door prefab is missing.");
                return;
            }

            if (prefab.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                prefab.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                errors.Add("Door prefab must not contain 3D physics components.");
            }

            DoorController[] doors = prefab.GetComponentsInChildren<DoorController>(true);
            Collider2D[] blockers = prefab.GetComponentsInChildren<Collider2D>(true)
                .Where(collider => !collider.isTrigger)
                .ToArray();
            if (doors.Length != 1 || blockers.Length < 1)
            {
                errors.Add("Door prefab requires one DoorController and a 2D blocking collider.");
            }

            if (prefab.GetComponentsInChildren<PressurePlate>(true).Length > 0 ||
                prefab.GetComponentsInChildren<WeightProvider>(true).Length > 0 ||
                prefab.GetComponentsInChildren<PlayerLifeController>(true).Length > 0 ||
                prefab.GetComponentsInChildren<CorpseController>(true).Length > 0 ||
                prefab.GetComponentsInChildren<NormalHazard>(true).Length > 0 ||
                prefab.GetComponentsInChildren<LevelSession>(true).Length > 0)
            {
                errors.Add("Door prefab contains a forbidden gameplay component.");
            }
        }

        private static void ValidateLevelExitPrefab(ICollection<string> errors)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                FirstLevelBuilder.LevelExitPrefabPath);
            if (prefab == null)
            {
                errors.Add("LevelExit prefab is missing.");
                return;
            }

            if (prefab.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                prefab.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                errors.Add("LevelExit prefab must not contain 3D physics components.");
            }

            LevelExit[] exits = prefab.GetComponentsInChildren<LevelExit>(true);
            Collider2D[] triggers = prefab.GetComponentsInChildren<Collider2D>(true)
                .Where(collider => collider.isTrigger)
                .ToArray();
            if (exits.Length != 1 || triggers.Length != 1)
            {
                errors.Add("LevelExit prefab requires exactly one LevelExit and one 2D trigger.");
            }
            else if (exits[0].LevelSession != null || exits[0].Player != null)
            {
                errors.Add("LevelExit prefab must not reference scene Player or LevelSession objects.");
            }

            if (prefab.GetComponentsInChildren<WeightProvider>(true).Length > 0 ||
                prefab.GetComponentsInChildren<PlayerLifeController>(true).Length > 0 ||
                prefab.GetComponentsInChildren<CorpseController>(true).Length > 0 ||
                prefab.GetComponentsInChildren<NormalHazard>(true).Length > 0 ||
                prefab.GetComponentsInChildren<DoorController>(true).Length > 0 ||
                prefab.GetComponentsInChildren<PressurePlate>(true).Length > 0 ||
                prefab.GetComponentsInChildren<LevelSession>(true).Length > 0 ||
                prefab.GetComponentsInChildren<Camera>(true).Length > 0 ||
                prefab.GetComponentsInChildren<AudioListener>(true).Length > 0)
            {
                errors.Add("LevelExit prefab contains a forbidden gameplay or scene component.");
            }
        }

        private static void ValidateAllPrefabMissingScripts(ICollection<string> errors)
        {
            string[] prefabPaths =
            {
                PlayerPrototypeBuilder.PrefabPath,
                DeathPrototypeBuilder.HazardPrefabPath,
                CorpsePrototypeBuilder.PrefabPath,
                InteractionPrototypeBuilder.PressurePlatePrefabPath,
                InteractionPrototypeBuilder.DoorPrefabPath,
                FirstLevelBuilder.LevelExitPrefabPath
            };

            foreach (string path in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    ValidateMissingScriptsRecursively(prefab, errors);
                }
            }
        }

        private static void ValidateBuildSettings(ICollection<string> errors)
        {
            EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .ToArray();

            if (enabledScenes.Length == 0)
            {
                errors.Add("Build Settings contains no enabled scenes.");
            }

            if (enabledScenes.Length < 2 ||
                !string.Equals(
                    enabledScenes[0].path,
                    FirstLevelBuilder.ScenePath,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    enabledScenes[1].path,
                    BootstrapSceneBuilder.ScenePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    "Build Settings must enable Level_001 at index 0 and GreyboxBootstrap at index 1.");
            }
        }

        private static void ValidateFirstLevelScene(ICollection<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FirstLevelBuilder.ScenePath) == null)
            {
                errors.Add($"First level scene is missing: {FirstLevelBuilder.ScenePath}");
                return;
            }

            Scene existing = SceneManager.GetSceneByPath(FirstLevelBuilder.ScenePath);
            bool alreadyLoaded = existing.IsValid() && existing.isLoaded;
            Scene scene = existing;

            try
            {
                if (!alreadyLoaded)
                {
                    scene = EditorSceneManager.OpenScene(
                        FirstLevelBuilder.ScenePath,
                        OpenSceneMode.Additive);
                }

                ValidateFirstLevelContents(scene, errors);
            }
            catch (Exception exception)
            {
                errors.Add($"Level_001 could not be opened or inspected: {exception.Message}");
            }
            finally
            {
                if (!alreadyLoaded && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ValidateFirstLevelContents(
            Scene scene,
            ICollection<string> errors)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            GameObject[] levelRoots = roots
                .Where(root => string.Equals(
                    root.name,
                    FirstLevelBuilder.LevelRootName,
                    StringComparison.Ordinal))
                .ToArray();
            if (levelRoots.Length != 1)
            {
                errors.Add("Level_001 must contain exactly one LevelRoot.");
                return;
            }

            GameObject root = levelRoots[0];
            if (root.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                root.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                errors.Add("Level_001 must not contain 3D physics components.");
            }

            Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
            if (cameras.Length != 1 || !cameras[0].orthographic ||
                !cameras[0].CompareTag("MainCamera"))
            {
                errors.Add("Level_001 requires exactly one orthographic MainCamera.");
            }

            if (root.GetComponentsInChildren<AudioListener>(true).Length != 1)
            {
                errors.Add("Level_001 requires exactly one AudioListener.");
            }

            PlayerLifeController[] players =
                root.GetComponentsInChildren<PlayerLifeController>(true);
            LevelSession[] sessions = root.GetComponentsInChildren<LevelSession>(true);
            RespawnController[] respawns = root.GetComponentsInChildren<RespawnController>(true);
            CorpseFactory[] factories = root.GetComponentsInChildren<CorpseFactory>(true);
            NormalHazard[] hazards = root.GetComponentsInChildren<NormalHazard>(true);
            PressurePlate[] plates = root.GetComponentsInChildren<PressurePlate>(true);
            DoorController[] doors = root.GetComponentsInChildren<DoorController>(true);
            LevelExit[] exits = root.GetComponentsInChildren<LevelExit>(true);
            LevelStatusView[] statusViews = root.GetComponentsInChildren<LevelStatusView>(true);
            LevelRestartInput[] restartInputs = root.GetComponentsInChildren<LevelRestartInput>(true);

            ValidateExactCount(players, 1, "Player", errors);
            ValidateExactCount(sessions, 1, "LevelSession", errors);
            ValidateExactCount(respawns, 1, "RespawnController", errors);
            ValidateExactCount(factories, 1, "CorpseFactory", errors);
            ValidateExactCount(hazards, 1, "NormalHazard", errors);
            ValidateExactCount(plates, 1, "PressurePlate", errors);
            ValidateExactCount(doors, 1, "DoorController", errors);
            ValidateExactCount(exits, 1, "LevelExit", errors);
            ValidateExactCount(statusViews, 1, "LevelStatusView", errors);
            ValidateExactCount(restartInputs, 1, "LevelRestartInput", errors);

            Transform[] corpseRoots = root.transform.Cast<Transform>()
                .Where(child => string.Equals(
                    child.name,
                    FirstLevelBuilder.RuntimeCorpsesName,
                    StringComparison.Ordinal))
                .ToArray();
            if (corpseRoots.Length != 1)
            {
                errors.Add("Level_001 requires exactly one RuntimeCorpses parent.");
            }
            else if (corpseRoots[0].GetComponentsInChildren<CorpseController>(true).Length > 0)
            {
                errors.Add("Level_001 RuntimeCorpses must be empty in the saved scene.");
            }

            if (players.Length == 1 && respawns.Length == 1 &&
                (respawns[0].Player != players[0] || respawns[0].SpawnPoint == null))
            {
                errors.Add("Level_001 RespawnController has invalid Player or Spawn references.");
            }

            if (players.Length == 1 && sessions.Length == 1 && factories.Length == 1)
            {
                CorpseFactory factory = factories[0];
                if (factory.Player != players[0] ||
                    factory.LevelSession != sessions[0] ||
                    factory.NormalCorpsePrefab == null ||
                    factory.CorpseParent == null ||
                    corpseRoots.Length != 1 ||
                    factory.CorpseParent != corpseRoots[0])
                {
                    errors.Add("Level_001 CorpseFactory explicit references are invalid.");
                }
            }

            if (doors.Length == 1 && plates.Length == 1 &&
                doors[0].PressurePlate != plates[0])
            {
                errors.Add("Level_001 Door must reference its unique PressurePlate.");
            }

            if (exits.Length == 1 && players.Length == 1 && sessions.Length == 1 &&
                (exits[0].Player != players[0] ||
                 exits[0].LevelSession != sessions[0] ||
                 exits[0].Trigger == null ||
                 !exits[0].Trigger.isTrigger))
            {
                errors.Add("Level_001 LevelExit explicit references or trigger are invalid.");
            }

            if (statusViews.Length == 1 && sessions.Length == 1 &&
                statusViews[0].LevelSession != sessions[0])
            {
                errors.Add("LevelStatusView must reference the Level_001 LevelSession.");
            }

            if (restartInputs.Length == 1 && sessions.Length == 1 &&
                restartInputs[0].LevelSession != sessions[0])
            {
                errors.Add("LevelRestartInput must reference the Level_001 LevelSession.");
            }

            if (plates.Length == 1 && plates[0].ActivationThreshold <= 0f)
            {
                errors.Add("Level_001 PressurePlate threshold must be positive.");
            }

            if (players.Length == 1)
            {
                WeightProvider weight = players[0].GetComponent<WeightProvider>();
                if (weight == null || weight.Weight <= 0f)
                {
                    errors.Add("Level_001 Player must provide positive weight.");
                }
            }

            if (respawns.Length == 1 && respawns[0].SpawnPoint != null &&
                hazards.Length == 1)
            {
                Vector3 spawnPosition = respawns[0].SpawnPoint.position;
                bool overlapsHazard = hazards[0]
                    .GetComponentsInChildren<Collider2D>(true)
                    .Any(collider => collider.bounds.Contains(spawnPosition));
                if (overlapsHazard)
                {
                    errors.Add("Level_001 NormalHazard must not overlap PlayerSpawn.");
                }
            }

            if (respawns.Length == 1 && respawns[0].SpawnPoint != null &&
                doors.Length == 1 && exits.Length == 1 &&
                !(respawns[0].SpawnPoint.position.x < doors[0].transform.position.x &&
                  doors[0].transform.position.x < exits[0].transform.position.x))
            {
                errors.Add("Level_001 requires Spawn and Exit on opposite sides of the Door.");
            }

            if (doors.Length == 1 && doors[0].BlockingCollider != null)
            {
                Bounds doorBounds = doors[0].BlockingCollider.bounds;
                bool overheadBlocker = root.GetComponentsInChildren<Collider2D>(true)
                    .Where(collider => collider != doors[0].BlockingCollider && !collider.isTrigger)
                    .Any(collider =>
                        collider.bounds.min.x < doorBounds.max.x &&
                        collider.bounds.max.x > doorBounds.min.x &&
                        collider.bounds.min.y <= doorBounds.max.y + 0.01f &&
                        collider.bounds.max.y > doorBounds.max.y);
                if (!overheadBlocker)
                {
                    errors.Add("Level_001 requires a physical blocker above the Door.");
                }
            }

            foreach (GameObject sceneRoot in roots)
            {
                ValidateMissingScriptsRecursively(sceneRoot, errors);
            }
        }

        private static void ValidateExactCount<T>(
            T[] components,
            int expected,
            string label,
            ICollection<string> errors) where T : Component
        {
            if (components.Length != expected)
            {
                errors.Add($"Level_001 must contain exactly {expected} {label}.");
            }
        }

        private static void ValidateBootstrapScene(ICollection<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapSceneBuilder.ScenePath) == null)
            {
                errors.Add($"Bootstrap scene is missing: {BootstrapSceneBuilder.ScenePath}");
                return;
            }

            Scene existingScene = SceneManager.GetSceneByPath(BootstrapSceneBuilder.ScenePath);
            bool wasAlreadyLoaded = existingScene.IsValid() && existingScene.isLoaded;
            Scene scene = existingScene;

            try
            {
                if (!wasAlreadyLoaded)
                {
                    scene = EditorSceneManager.OpenScene(
                        BootstrapSceneBuilder.ScenePath,
                        OpenSceneMode.Additive);
                }

                ValidateSceneContents(scene, errors);
            }
            catch (Exception exception)
            {
                errors.Add(
                    $"Bootstrap scene could not be opened or inspected: {exception.Message}");
            }
            finally
            {
                if (!wasAlreadyLoaded && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ValidateSceneContents(Scene scene, ICollection<string> errors)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            GameObject levelRoot = roots.SingleOrDefault(root =>
                string.Equals(root.name, BootstrapSceneBuilder.LevelRootName, StringComparison.Ordinal));

            if (levelRoot == null)
            {
                errors.Add($"Bootstrap scene is missing root object '{BootstrapSceneBuilder.LevelRootName}'.");
                return;
            }

            Camera[] cameras = levelRoot.GetComponentsInChildren<Camera>(true);
            Camera[] mainCameras = cameras
                .Where(camera => camera.CompareTag("MainCamera"))
                .ToArray();

            if (mainCameras.Length != 1 || !mainCameras[0].orthographic)
            {
                errors.Add("Bootstrap scene must contain exactly one orthographic MainCamera.");
            }

            if (levelRoot.GetComponentsInChildren<AudioListener>(true).Length != 1)
            {
                errors.Add("Bootstrap scene must contain exactly one AudioListener.");
            }

            if (levelRoot.GetComponentsInChildren<Light>(true)
                .Any(light => light.type == LightType.Directional))
            {
                errors.Add("Bootstrap scene must not contain a Directional Light.");
            }

            if (levelRoot.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                levelRoot.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                errors.Add("Bootstrap scene must not contain 3D physics components.");
            }

            PlayerMotor2D[] activePlayers = roots
                .SelectMany(root => root.GetComponentsInChildren<PlayerMotor2D>(false))
                .ToArray();

            if (activePlayers.Length != 1)
            {
                errors.Add("Bootstrap scene must contain exactly one active player.");
            }

            LevelSession[] levelSessions = levelRoot.GetComponentsInChildren<LevelSession>(true);
            RespawnController[] respawnControllers =
                levelRoot.GetComponentsInChildren<RespawnController>(true);
            NormalHazard[] hazards = levelRoot.GetComponentsInChildren<NormalHazard>(true);
            CorpseFactory[] corpseFactories =
                levelRoot.GetComponentsInChildren<CorpseFactory>(true);
            PressurePlate[] pressurePlates =
                levelRoot.GetComponentsInChildren<PressurePlate>(true);
            DoorController[] doors =
                levelRoot.GetComponentsInChildren<DoorController>(true);

            if (levelSessions.Length != 1)
            {
                errors.Add("Bootstrap scene must contain exactly one LevelSession.");
            }

            if (respawnControllers.Length != 1)
            {
                errors.Add("Bootstrap scene must contain exactly one RespawnController.");
            }
            else if (respawnControllers[0].Player == null ||
                     respawnControllers[0].SpawnPoint == null)
            {
                errors.Add("RespawnController must have explicit Player and SpawnPoint references.");
            }

            if (hazards.Length < 1)
            {
                errors.Add("Bootstrap scene must contain at least one NormalHazard.");
            }

            if (corpseFactories.Length != 1)
            {
                errors.Add("Bootstrap scene must contain exactly one CorpseFactory.");
            }
            else
            {
                CorpseFactory factory = corpseFactories[0];
                if (factory.Player == null ||
                    factory.LevelSession == null ||
                    factory.NormalCorpsePrefab == null ||
                    factory.CorpseParent == null)
                {
                    errors.Add(
                        "CorpseFactory must have explicit Player, LevelSession, " +
                        "NormalCorpse Prefab, and CorpseParent references.");
                }
                else if (!string.Equals(
                    AssetDatabase.GetAssetPath(factory.NormalCorpsePrefab),
                    CorpsePrototypeBuilder.PrefabPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("CorpseFactory must reference the generated NormalCorpse prefab.");
                }
            }

            if (pressurePlates.Length != 1)
            {
                errors.Add("Bootstrap scene must contain exactly one PressurePlate.");
            }

            if (doors.Length != 1)
            {
                errors.Add("Bootstrap scene must contain exactly one DoorController.");
            }
            else if (pressurePlates.Length == 1 &&
                     doors[0].PressurePlate != pressurePlates[0])
            {
                errors.Add("Bootstrap DoorController must explicitly reference its PressurePlate.");
            }

            Transform[] runtimeCorpseRoots = levelRoot.transform.Cast<Transform>()
                .Where(child => string.Equals(
                    child.name,
                    "RuntimeCorpses",
                    StringComparison.Ordinal))
                .ToArray();
            if (runtimeCorpseRoots.Length != 1)
            {
                errors.Add("Bootstrap scene must contain exactly one RuntimeCorpses parent.");
            }
            else if (runtimeCorpseRoots[0]
                .GetComponentsInChildren<CorpseController>(true).Length > 0)
            {
                errors.Add("RuntimeCorpses must not contain pre-generated corpses.");
            }

            if (respawnControllers.Length == 1 && respawnControllers[0].SpawnPoint != null)
            {
                Vector3 spawnPosition = respawnControllers[0].SpawnPoint.position;
                bool hazardOverlapsSpawn = hazards
                    .SelectMany(hazard => hazard.GetComponentsInChildren<Collider2D>(true))
                    .Any(collider => collider.bounds.Contains(spawnPosition));
                if (hazardOverlapsSpawn)
                {
                    errors.Add("NormalHazard must not overlap PlayerSpawn.");
                }
            }

            Collider2D[] activeColliders = roots
                .SelectMany(root => root.GetComponentsInChildren<Collider2D>(false))
                .ToArray();

            bool hasGround = activeColliders.Any(collider =>
                !collider.isTrigger &&
                (activePlayers.Length != 1 ||
                 !collider.transform.IsChildOf(activePlayers[0].transform)));

            if (!hasGround)
            {
                errors.Add("Bootstrap scene must contain an active non-trigger 2D ground collider.");
            }

            foreach (GameObject root in roots)
            {
                ValidateMissingScriptsRecursively(root, errors);
            }
        }

        private static void ValidateMissingScriptsRecursively(
            GameObject gameObject,
            ICollection<string> errors)
        {
            int missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
            if (missingScriptCount > 0)
            {
                errors.Add(
                    $"GameObject '{GetHierarchyPath(gameObject.transform)}' has " +
                    $"{missingScriptCount} missing script reference(s).");
            }

            foreach (Transform child in gameObject.transform)
            {
                ValidateMissingScriptsRecursively(child.gameObject, errors);
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            Transform current = transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private static void ThrowValidationFailure(IEnumerable<string> errors)
        {
            string message = "Project validation failed:\n- " + string.Join("\n- ", errors);
            Debug.LogError(message);
            throw new BuildFailedException(message);
        }

        private static string GetProjectRootPath()
        {
            DirectoryInfo assetsDirectory = Directory.GetParent(Application.dataPath);
            if (assetsDirectory == null)
            {
                throw new BuildFailedException(
                    $"Could not determine project root from '{Application.dataPath}'.");
            }

            return assetsDirectory.FullName;
        }
    }
}
