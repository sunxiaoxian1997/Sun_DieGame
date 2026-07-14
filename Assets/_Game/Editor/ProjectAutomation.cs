using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            ValidateBootstrapScene(errors);

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

            int bootstrapEntries = enabledScenes.Count(scene =>
                string.Equals(
                    scene.path,
                    BootstrapSceneBuilder.ScenePath,
                    StringComparison.OrdinalIgnoreCase));

            if (bootstrapEntries != 1)
            {
                errors.Add(
                    "Build Settings must contain exactly one enabled bootstrap scene entry: " +
                    BootstrapSceneBuilder.ScenePath);
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
