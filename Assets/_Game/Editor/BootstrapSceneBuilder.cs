using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorpseMechanism.Editor
{
    public static class BootstrapSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/GreyboxBootstrap.unity";
        public const string LevelRootName = "LevelRoot";

        private const string CameraName = "BootstrapCamera";
        private const string GroundName = "GreyboxGround";
        private const string MenuRoot = "Corpse Mechanism/Infrastructure/";

        [MenuItem(MenuRoot + "Create or Update Bootstrap Scene")]
        public static void CreateOrUpdateBootstrapScene()
        {
            EnsureSceneDirectoryExists();

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene generatedScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(generatedScene);
                CreateSceneContents(generatedScene);

                if (!EditorSceneManager.SaveScene(generatedScene, ScenePath))
                {
                    throw new InvalidOperationException(
                        $"Failed to save bootstrap scene at '{ScenePath}'.");
                }

                EnsureSceneIsEnabledInBuildSettings();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"Created bootstrap scene and enabled it in Build Settings: {ScenePath}");
            }
            finally
            {
                if (generatedScene.IsValid() && generatedScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(generatedScene, true);
                }

                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }
        }

        private static void CreateSceneContents(Scene scene)
        {
            GameObject levelRoot = new GameObject(LevelRootName);
            SceneManager.MoveGameObjectToScene(levelRoot, scene);

            GameObject cameraObject = new GameObject(
                CameraName,
                typeof(Camera),
                typeof(AudioListener));
            cameraObject.transform.SetParent(levelRoot.transform, false);
            cameraObject.transform.position = new Vector3(0f, 1f, -10f);
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.17f, 1f);

            GameObject ground = new GameObject(GroundName, typeof(SpriteRenderer));
            ground.transform.SetParent(levelRoot.transform, false);
            ground.transform.localPosition = new Vector3(0f, -3.5f, 0f);
            ground.transform.localScale = new Vector3(16f, 1f, 1f);

            SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
            renderer.sprite = LoadBuiltInSprite();
            renderer.color = new Color(0.45f, 0.48f, 0.52f, 1f);
        }

        private static Sprite LoadBuiltInSprite()
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    "Unity built-in greybox sprite 'UI/Skin/UISprite.psd' is unavailable.");
            }

            return sprite;
        }

        private static void EnsureSceneDirectoryExists()
        {
            string absoluteSceneDirectory = Path.GetFullPath(
                Path.Combine(Application.dataPath, "_Game", "Scenes"));
            Directory.CreateDirectory(absoluteSceneDirectory);
            AssetDatabase.Refresh();
        }

        private static void EnsureSceneIsEnabledInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(scene => !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                .ToList();

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
