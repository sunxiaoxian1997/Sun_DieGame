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
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorpseMechanism.Editor
{
    public static class FirstLevelBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Level_001.unity";
        public const string LevelExitPrefabPath = "Assets/_Game/Prefabs/LevelExit.prefab";
        public const string LevelRootName = "LevelRoot";
        public const string RuntimeCorpsesName = "RuntimeCorpses";

        private const string MenuPath = "Corpse Mechanism/Level/Create or Update Level 001";
        private const string CameraName = "LevelCamera";
        private const string PlayerSpawnName = "PlayerSpawn";
        private const string LevelSystemsName = "LevelSystems";

        [MenuItem(MenuPath)]
        public static void CreateOrUpdateLevel001()
        {
            EnsureDirectoriesExist();

            GameObject playerPrefab = PlayerPrototypeBuilder.CreateOrUpdatePlayerPrefab();
            GameObject hazardPrefab = DeathPrototypeBuilder.CreateOrUpdateNormalHazardPrefab();
            GameObject corpsePrefab = CorpsePrototypeBuilder.CreateOrUpdateNormalCorpsePrefab();
            GameObject platePrefab = InteractionPrototypeBuilder.CreateOrUpdatePressurePlatePrefab();
            GameObject doorPrefab = InteractionPrototypeBuilder.CreateOrUpdateDoorPrefab();
            GameObject exitPrefab = CreateOrUpdateLevelExitPrefab();

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene levelScene = OpenOrCreateScene(previousActiveScene, out bool closeAfterSave);

            try
            {
                SceneManager.SetActiveScene(levelScene);
                ConfigureScene(
                    levelScene,
                    playerPrefab,
                    hazardPrefab,
                    corpsePrefab,
                    platePrefab,
                    doorPrefab,
                    exitPrefab);

                if (!EditorSceneManager.SaveScene(levelScene, ScenePath))
                {
                    throw new InvalidOperationException($"Failed to save first level at '{ScenePath}'.");
                }

                EnsureBuildSettingsOrder();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Created or updated first greybox level: {ScenePath}");
            }
            finally
            {
                if (closeAfterSave && levelScene.IsValid() && levelScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(levelScene, true);
                }

                if (closeAfterSave && previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }
        }

        public static GameObject CreateOrUpdateLevelExitPrefab()
        {
            EnsureDirectoriesExist();
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(LevelExitPrefabPath);
            bool loadedContents = existing != null;
            GameObject root = loadedContents
                ? PrefabUtility.LoadPrefabContents(LevelExitPrefabPath)
                : new GameObject("LevelExit");

            try
            {
                Reject3DPhysics(root, "LevelExit");
                root.name = "LevelExit";
                root.layer = 0;
                root.SetActive(true);
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;

                SpriteRenderer renderer = GetOrAddSingleComponent<SpriteRenderer>(root);
                BoxCollider2D trigger = GetOrAddSingleComponent<BoxCollider2D>(root);
                LevelExit levelExit = GetOrAddSingleComponent<LevelExit>(root);

                renderer.sprite = LoadBuiltInSprite();
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = new Vector2(1.2f, 2.5f);
                renderer.color = new Color(0.28f, 0.9f, 0.66f, 0.8f);
                trigger.size = new Vector2(1.2f, 2.5f);
                trigger.offset = Vector2.zero;
                trigger.isTrigger = true;
                levelExit.Configure(trigger, null, null);

                EditorUtility.SetDirty(renderer);
                EditorUtility.SetDirty(trigger);
                EditorUtility.SetDirty(levelExit);
                EditorUtility.SetDirty(root);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    LevelExitPrefabPath,
                    out bool success);
                if (!success || saved == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to save LevelExit prefab at '{LevelExitPrefabPath}'.");
                }
            }
            finally
            {
                if (loadedContents)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<GameObject>(LevelExitPrefabPath);
        }

        private static Scene OpenOrCreateScene(Scene activeScene, out bool closeAfterSave)
        {
            if (Application.isBatchMode)
            {
                closeAfterSave = false;
                return AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                    ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                    : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new OperationCanceledException(
                    "Level 001 generation was cancelled to preserve unsaved scene changes.");
            }

            Scene loaded = SceneManager.GetSceneByPath(ScenePath);
            if (loaded.IsValid() && loaded.isLoaded)
            {
                closeAfterSave = false;
                return loaded;
            }

            bool additive = activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.path);
            closeAfterSave = additive;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                return EditorSceneManager.OpenScene(
                    ScenePath,
                    additive ? OpenSceneMode.Additive : OpenSceneMode.Single);
            }

            return EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                additive ? NewSceneMode.Additive : NewSceneMode.Single);
        }

        private static void ConfigureScene(
            Scene scene,
            GameObject playerPrefab,
            GameObject hazardPrefab,
            GameObject corpsePrefab,
            GameObject platePrefab,
            GameObject doorPrefab,
            GameObject exitPrefab)
        {
            GameObject root = GetOrCreateUniqueRoot(scene, LevelRootName);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            ConfigureCamera(root.transform);
            ConfigureBlock(root.transform, "Ground", new Vector2(0f, -3.5f), new Vector2(18f, 1f));
            ConfigureBlock(root.transform, "LeftBoundary", new Vector2(-8.75f, 0f), new Vector2(0.5f, 7f));
            ConfigureBlock(root.transform, "RightBoundary", new Vector2(8.75f, 0f), new Vector2(0.5f, 7f));
            ConfigureBlock(root.transform, "Ceiling", new Vector2(0f, 4.75f), new Vector2(18f, 0.5f));
            ConfigureBlock(root.transform, "DoorHeader", new Vector2(3f, 1f), new Vector2(2f, 1f));
            ConfigureBlock(root.transform, "PlateStopLeft", new Vector2(-2.85f, -2.75f), new Vector2(0.4f, 0.5f));
            ConfigureBlock(root.transform, "PlateStopRight", new Vector2(-0.15f, -2.75f), new Vector2(0.4f, 0.5f));

            GameObject spawn = GetOrCreateUniqueChild(root.transform, PlayerSpawnName);
            SetLocalTransform(spawn.transform, new Vector3(-7f, -2.1f, 0f));

            GameObject player = GetOrCreatePrefabInstance<PlayerLifeController>(
                scene,
                root.transform,
                playerPrefab,
                PlayerPrototypeBuilder.PrefabPath,
                "Player");
            SetLocalTransform(player.transform, spawn.transform.localPosition);
            player.SetActive(true);
            PlayerLifeController playerLife = player.GetComponent<PlayerLifeController>();

            GameObject hazard = GetOrCreatePrefabInstance<NormalHazard>(
                scene,
                root.transform,
                hazardPrefab,
                DeathPrototypeBuilder.HazardPrefabPath,
                "NormalHazard");
            SetLocalTransform(hazard.transform, new Vector3(-1.5f, -0.2f, 0f));
            hazard.SetActive(true);

            GameObject plateObject = GetOrCreatePrefabInstance<PressurePlate>(
                scene,
                root.transform,
                platePrefab,
                InteractionPrototypeBuilder.PressurePlatePrefabPath,
                "PressurePlate");
            SetLocalTransform(plateObject.transform, new Vector3(-1.5f, -3f, 0f));
            plateObject.SetActive(true);
            PressurePlate plate = plateObject.GetComponent<PressurePlate>();

            GameObject doorObject = GetOrCreatePrefabInstance<DoorController>(
                scene,
                root.transform,
                doorPrefab,
                InteractionPrototypeBuilder.DoorPrefabPath,
                "Door");
            SetLocalTransform(doorObject.transform, new Vector3(3f, -1.25f, 0f));
            doorObject.SetActive(true);
            DoorController door = doorObject.GetComponent<DoorController>();
            door.Configure(
                plate,
                doorObject.GetComponent<BoxCollider2D>(),
                doorObject.transform,
                doorObject.GetComponent<SpriteRenderer>(),
                new Vector3(0f, 4f, 0f));

            GameObject systems = GetOrCreateUniqueChild(root.transform, LevelSystemsName);
            SetLocalTransform(systems.transform, Vector3.zero);
            LevelSession session = GetOrAddSingleComponent<LevelSession>(systems);
            RespawnController respawn = GetOrAddSingleComponent<RespawnController>(systems);
            CorpseFactory factory = GetOrAddSingleComponent<CorpseFactory>(systems);
            LevelStatusView statusView = GetOrAddSingleComponent<LevelStatusView>(systems);
            LevelRestartInput restartInput = GetOrAddSingleComponent<LevelRestartInput>(systems);

            respawn.Configure(playerLife, spawn.transform, 0.65f);
            session.Configure(playerLife, respawn, 3);

            GameObject runtimeCorpses = GetOrCreateUniqueChild(root.transform, RuntimeCorpsesName);
            SetLocalTransform(runtimeCorpses.transform, Vector3.zero);
            if (runtimeCorpses.GetComponentsInChildren<CorpseController>(true).Length > 0)
            {
                throw new InvalidOperationException(
                    "RuntimeCorpses must be empty in the saved Level_001 scene.");
            }

            CorpseController corpse = corpsePrefab.GetComponent<CorpseController>();
            if (corpse == null)
            {
                throw new InvalidOperationException("NormalCorpse prefab is missing CorpseController.");
            }

            factory.Configure(playerLife, corpse, session, runtimeCorpses.transform);
            statusView.Configure(session);
            restartInput.Configure(session);

            GameObject exitObject = GetOrCreatePrefabInstance<LevelExit>(
                scene,
                root.transform,
                exitPrefab,
                LevelExitPrefabPath,
                "LevelExit");
            SetLocalTransform(exitObject.transform, new Vector3(6.5f, -1.75f, 0f));
            exitObject.SetActive(true);
            LevelExit levelExit = exitObject.GetComponent<LevelExit>();
            levelExit.Configure(exitObject.GetComponent<Collider2D>(), session, playerLife);

            EditorUtility.SetDirty(respawn);
            EditorUtility.SetDirty(session);
            EditorUtility.SetDirty(factory);
            EditorUtility.SetDirty(statusView);
            EditorUtility.SetDirty(restartInput);
            EditorUtility.SetDirty(door);
            EditorUtility.SetDirty(levelExit);
            EditorUtility.SetDirty(root);
        }

        private static void ConfigureCamera(Transform parent)
        {
            GameObject cameraObject = GetOrCreateUniqueChild(parent, CameraName);
            SetLocalTransform(cameraObject.transform, new Vector3(0f, 0f, -10f));
            cameraObject.tag = "MainCamera";
            Camera camera = GetOrAddSingleComponent<Camera>(cameraObject);
            GetOrAddSingleComponent<AudioListener>(cameraObject);
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.11f, 0.14f, 1f);
            EditorUtility.SetDirty(camera);
        }

        private static void ConfigureBlock(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            GameObject block = GetOrCreateUniqueChild(parent, name);
            SetLocalTransform(block.transform, new Vector3(position.x, position.y, 0f));
            SpriteRenderer renderer = GetOrAddSingleComponent<SpriteRenderer>(block);
            BoxCollider2D collider = GetOrAddSingleComponent<BoxCollider2D>(block);
            renderer.sprite = LoadBuiltInSprite();
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = new Color(0.43f, 0.47f, 0.52f, 1f);
            collider.size = size;
            collider.offset = Vector2.zero;
            collider.isTrigger = false;
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(collider);
        }

        private static GameObject GetOrCreatePrefabInstance<T>(
            Scene scene,
            Transform parent,
            GameObject prefab,
            string expectedPath,
            string displayName) where T : Component
        {
            T[] matches = parent.GetComponentsInChildren<T>(true);
            if (matches.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Level_001 contains more than one {displayName}.");
            }

            if (matches.Length == 1)
            {
                GameObject existing = matches[0].gameObject;
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(existing);
                string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
                if (!string.Equals(sourcePath, expectedPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Level_001 {displayName} must use prefab '{expectedPath}'.");
                }

                return existing;
            }

            GameObject created = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (created == null)
            {
                throw new InvalidOperationException($"Failed to instantiate {displayName} prefab.");
            }

            created.name = displayName;
            created.transform.SetParent(parent, true);
            return created;
        }

        private static GameObject GetOrCreateUniqueRoot(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .Where(root => string.Equals(root.name, name, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"Scene contains more than one root '{name}'.");
            }

            if (matches.Length == 1)
            {
                return matches[0];
            }

            GameObject created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static GameObject GetOrCreateUniqueChild(Transform parent, string name)
        {
            GameObject[] matches = parent.Cast<Transform>()
                .Where(child => string.Equals(child.name, name, StringComparison.Ordinal))
                .Select(child => child.gameObject)
                .ToArray();
            if (matches.Length > 1)
            {
                throw new InvalidOperationException(
                    $"'{parent.name}' contains more than one child '{name}'.");
            }

            if (matches.Length == 1)
            {
                return matches[0];
            }

            GameObject created = new GameObject(name);
            created.transform.SetParent(parent, false);
            return created;
        }

        private static T GetOrAddSingleComponent<T>(GameObject gameObject) where T : Component
        {
            T[] components = gameObject.GetComponents<T>();
            if (components.Length > 1)
            {
                throw new InvalidOperationException(
                    $"'{gameObject.name}' contains more than one {typeof(T).Name}.");
            }

            return components.Length == 1 ? components[0] : gameObject.AddComponent<T>();
        }

        private static void SetLocalTransform(Transform transform, Vector3 position)
        {
            transform.localPosition = position;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private static void EnsureBuildSettingsOrder()
        {
            List<EditorBuildSettingsScene> remaining = EditorBuildSettings.scenes
                .Where(scene =>
                    !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(
                        scene.path,
                        BootstrapSceneBuilder.ScenePath,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            remaining.Insert(
                0,
                new EditorBuildSettingsScene(BootstrapSceneBuilder.ScenePath, true));
            remaining.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = remaining.ToArray();
        }

        private static void Reject3DPhysics(GameObject root, string label)
        {
            if (root.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                root.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                throw new InvalidOperationException($"{label} contains unsupported 3D physics.");
            }
        }

        private static Sprite LoadBuiltInSprite()
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (sprite == null)
            {
                throw new InvalidOperationException("Unity built-in greybox sprite is unavailable.");
            }

            return sprite;
        }

        private static void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(Path.GetFullPath(
                Path.Combine(Application.dataPath, "_Game", "Prefabs")));
            Directory.CreateDirectory(Path.GetFullPath(
                Path.Combine(Application.dataPath, "_Game", "Scenes")));
            AssetDatabase.Refresh();
        }
    }
}
