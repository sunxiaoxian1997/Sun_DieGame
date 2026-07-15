using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CorpseMechanism.Corpse;
using CorpseMechanism.Death;
using CorpseMechanism.Level;
using CorpseMechanism.Interaction;
using CorpseMechanism.Player;
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
        private const string JumpStepName = "JumpStep";
        private const string PlayerSpawnName = "PlayerSpawn";
        private const string LevelSystemsName = "LevelSystems";
        private const string NormalHazardName = "NormalHazard";
        private const string RuntimeCorpsesName = "RuntimeCorpses";
        private const string MenuRoot = "Corpse Mechanism/Infrastructure/";

        [MenuItem(MenuRoot + "Create or Update Bootstrap Scene")]
        public static void CreateOrUpdateBootstrapScene()
        {
            EnsureSceneDirectoryExists();
            GameObject playerPrefab = PlayerPrototypeBuilder.CreateOrUpdatePlayerPrefab();
            GameObject hazardPrefab = DeathPrototypeBuilder.CreateOrUpdateNormalHazardPrefab();
            GameObject corpsePrefab = CorpsePrototypeBuilder.CreateOrUpdateNormalCorpsePrefab();
            GameObject pressurePlatePrefab =
                InteractionPrototypeBuilder.CreateOrUpdatePressurePlatePrefab();
            GameObject doorPrefab = InteractionPrototypeBuilder.CreateOrUpdateDoorPrefab();

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene generatedScene = OpenOrCreateGeneratedScene(
                previousActiveScene,
                out bool closeGeneratedSceneAfterSave);

            try
            {
                SceneManager.SetActiveScene(generatedScene);
                CreateOrUpdateSceneContents(
                    generatedScene,
                    playerPrefab,
                    hazardPrefab,
                    corpsePrefab,
                    pressurePlatePrefab,
                    doorPrefab);

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
                if (closeGeneratedSceneAfterSave && generatedScene.IsValid() && generatedScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(generatedScene, true);
                }

                if (closeGeneratedSceneAfterSave && previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }
        }

        private static Scene OpenOrCreateGeneratedScene(
            Scene activeScene,
            out bool closeAfterSave)
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
                    "Bootstrap scene generation was cancelled to preserve unsaved scene changes.");
            }

            Scene loadedScene = SceneManager.GetSceneByPath(ScenePath);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                closeAfterSave = false;
                return loadedScene;
            }

            bool canOpenAdditively = activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.path);
            closeAfterSave = canOpenAdditively;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                return EditorSceneManager.OpenScene(
                    ScenePath,
                    canOpenAdditively ? OpenSceneMode.Additive : OpenSceneMode.Single);
            }

            return EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                canOpenAdditively ? NewSceneMode.Additive : NewSceneMode.Single);
        }

        private static void CreateOrUpdateSceneContents(
            Scene scene,
            GameObject playerPrefab,
            GameObject hazardPrefab,
            GameObject corpsePrefab,
            GameObject pressurePlatePrefab,
            GameObject doorPrefab)
        {
            GameObject levelRoot = GetOrCreateUniqueRoot(scene, LevelRootName);

            GameObject cameraObject = GetOrCreateUniqueChild(levelRoot.transform, CameraName);
            Camera camera = GetOrAddComponent<Camera>(cameraObject);
            GetOrAddComponent<AudioListener>(cameraObject);

            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            cameraObject.transform.localRotation = Quaternion.identity;
            cameraObject.transform.localScale = Vector3.one;
            cameraObject.tag = "MainCamera";

            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.17f, 1f);

            GameObject ground = GetOrCreateUniqueChild(levelRoot.transform, GroundName);
            SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(ground);
            BoxCollider2D groundCollider = GetOrAddComponent<BoxCollider2D>(ground);

            ground.transform.localPosition = new Vector3(0f, -3.5f, 0f);
            ground.transform.localRotation = Quaternion.identity;
            ground.transform.localScale = Vector3.one;

            renderer.sprite = LoadBuiltInSprite();
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(16f, 1f);
            renderer.color = new Color(0.45f, 0.48f, 0.52f, 1f);
            groundCollider.size = new Vector2(16f, 1f);
            groundCollider.offset = Vector2.zero;
            groundCollider.isTrigger = false;

            GameObject jumpStep = GetOrCreateUniqueChild(levelRoot.transform, JumpStepName);
            SpriteRenderer stepRenderer = GetOrAddComponent<SpriteRenderer>(jumpStep);
            BoxCollider2D stepCollider = GetOrAddComponent<BoxCollider2D>(jumpStep);

            jumpStep.transform.localPosition = new Vector3(2f, -2.7f, 0f);
            jumpStep.transform.localRotation = Quaternion.identity;
            jumpStep.transform.localScale = Vector3.one;
            stepRenderer.sprite = LoadBuiltInSprite();
            stepRenderer.drawMode = SpriteDrawMode.Sliced;
            stepRenderer.size = new Vector2(2f, 0.6f);
            stepRenderer.color = new Color(0.58f, 0.6f, 0.64f, 1f);
            stepCollider.size = new Vector2(2f, 0.6f);
            stepCollider.offset = Vector2.zero;
            stepCollider.isTrigger = false;

            GameObject playerSpawn = GetOrCreateUniqueChild(levelRoot.transform, PlayerSpawnName);
            playerSpawn.transform.localPosition = new Vector3(-4f, -2.1f, 0f);
            playerSpawn.transform.localRotation = Quaternion.identity;
            playerSpawn.transform.localScale = Vector3.one;

            GameObject player = GetOrCreatePlayerInstance(scene, levelRoot.transform, playerPrefab);
            player.transform.position = playerSpawn.transform.position;
            player.transform.rotation = Quaternion.identity;
            player.transform.localScale = Vector3.one;
            player.SetActive(true);

            GameObject hazard = GetOrCreateHazardInstance(scene, levelRoot.transform, hazardPrefab);
            hazard.transform.localPosition = new Vector3(5f, -2.2f, 0f);
            hazard.transform.localRotation = Quaternion.identity;
            hazard.transform.localScale = Vector3.one;
            hazard.SetActive(true);

            GameObject levelSystems = GetOrCreateUniqueChild(levelRoot.transform, LevelSystemsName);
            levelSystems.transform.localPosition = Vector3.zero;
            levelSystems.transform.localRotation = Quaternion.identity;
            levelSystems.transform.localScale = Vector3.one;

            LevelSession levelSession = GetOrAddComponent<LevelSession>(levelSystems);
            RespawnController respawnController = GetOrAddComponent<RespawnController>(levelSystems);
            CorpseFactory corpseFactory = GetOrAddComponent<CorpseFactory>(levelSystems);
            PlayerLifeController playerLife = player.GetComponent<PlayerLifeController>();
            if (playerLife == null)
            {
                throw new InvalidOperationException(
                    "Generated Player prefab must contain PlayerLifeController.");
            }

            respawnController.Configure(playerLife, playerSpawn.transform, 0.65f);
            levelSession.Configure(playerLife, respawnController, 3);

            GameObject runtimeCorpses =
                GetOrCreateUniqueChild(levelRoot.transform, RuntimeCorpsesName);
            runtimeCorpses.transform.localPosition = Vector3.zero;
            runtimeCorpses.transform.localRotation = Quaternion.identity;
            runtimeCorpses.transform.localScale = Vector3.one;

            if (runtimeCorpses.GetComponentsInChildren<CorpseController>(true).Length > 0)
            {
                throw new InvalidOperationException(
                    "RuntimeCorpses must be empty in the saved bootstrap scene.");
            }

            CorpseController corpseController = corpsePrefab.GetComponent<CorpseController>();
            if (corpseController == null)
            {
                throw new InvalidOperationException(
                    "Generated NormalCorpse prefab must contain CorpseController.");
            }

            corpseFactory.Configure(
                playerLife,
                corpseController,
                levelSession,
                runtimeCorpses.transform,
                Vector2.zero);
            EditorUtility.SetDirty(respawnController);
            EditorUtility.SetDirty(levelSession);
            EditorUtility.SetDirty(corpseFactory);

            GameObject pressurePlateObject = GetOrCreatePrefabInstance<PressurePlate>(
                scene,
                levelRoot.transform,
                pressurePlatePrefab,
                InteractionPrototypeBuilder.PressurePlatePrefabPath,
                "PressurePlate");
            pressurePlateObject.transform.localPosition = new Vector3(5f, -3f, 0f);
            pressurePlateObject.transform.localRotation = Quaternion.identity;
            pressurePlateObject.transform.localScale = Vector3.one;
            pressurePlateObject.SetActive(true);

            PressurePlate pressurePlate = pressurePlateObject.GetComponent<PressurePlate>();
            GameObject doorObject = GetOrCreatePrefabInstance<DoorController>(
                scene,
                levelRoot.transform,
                doorPrefab,
                InteractionPrototypeBuilder.DoorPrefabPath,
                "Door");
            doorObject.transform.localPosition = new Vector3(7f, -1.25f, 0f);
            doorObject.transform.localRotation = Quaternion.identity;
            doorObject.transform.localScale = Vector3.one;
            doorObject.SetActive(true);

            DoorController door = doorObject.GetComponent<DoorController>();
            BoxCollider2D doorCollider = doorObject.GetComponent<BoxCollider2D>();
            SpriteRenderer doorRenderer = doorObject.GetComponent<SpriteRenderer>();
            door.Configure(
                pressurePlate,
                doorCollider,
                doorObject.transform,
                doorRenderer,
                new Vector3(0f, 4f, 0f));
            EditorUtility.SetDirty(door);
        }

        private static GameObject GetOrCreatePrefabInstance<T>(
            Scene scene,
            Transform levelRoot,
            GameObject prefab,
            string expectedPrefabPath,
            string displayName) where T : Component
        {
            T[] components = levelRoot.GetComponentsInChildren<T>(true);
            if (components.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Bootstrap scene contains more than one {displayName}.");
            }

            if (components.Length == 1)
            {
                GameObject existing = components[0].gameObject;
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(existing);
                string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
                if (!string.Equals(sourcePath, expectedPrefabPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Bootstrap {displayName} must use the generated prefab.");
                }

                return existing;
            }

            GameObject created = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (created == null)
            {
                throw new InvalidOperationException($"Failed to instantiate {displayName} prefab.");
            }

            created.name = displayName;
            created.transform.SetParent(levelRoot, true);
            return created;
        }

        private static GameObject GetOrCreateHazardInstance(
            Scene scene,
            Transform levelRoot,
            GameObject hazardPrefab)
        {
            NormalHazard[] hazards = levelRoot.GetComponentsInChildren<NormalHazard>(true);
            if (hazards.Length > 1)
            {
                throw new InvalidOperationException(
                    "Bootstrap scene contains more than one normal hazard.");
            }

            if (hazards.Length == 1)
            {
                GameObject existingHazard = hazards[0].gameObject;
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(existingHazard);
                string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
                if (!string.Equals(
                    sourcePath,
                    DeathPrototypeBuilder.HazardPrefabPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Bootstrap hazard must be an instance of the generated NormalHazard prefab.");
                }

                return existingHazard;
            }

            GameObject created = PrefabUtility.InstantiatePrefab(hazardPrefab, scene) as GameObject;
            if (created == null)
            {
                throw new InvalidOperationException("Failed to instantiate the NormalHazard prefab.");
            }

            created.name = NormalHazardName;
            created.transform.SetParent(levelRoot, true);
            return created;
        }

        private static GameObject GetOrCreatePlayerInstance(
            Scene scene,
            Transform levelRoot,
            GameObject playerPrefab)
        {
            PlayerMotor2D[] motors = levelRoot.GetComponentsInChildren<PlayerMotor2D>(true);
            if (motors.Length > 1)
            {
                throw new InvalidOperationException(
                    "Bootstrap scene contains more than one player motor.");
            }

            if (motors.Length == 1)
            {
                GameObject existingPlayer = motors[0].gameObject;
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(existingPlayer);
                string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;

                if (!string.Equals(
                    sourcePath,
                    PlayerPrototypeBuilder.PrefabPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Bootstrap player must be an instance of the generated Player prefab.");
                }

                return existingPlayer;
            }

            GameObject created = PrefabUtility.InstantiatePrefab(playerPrefab, scene) as GameObject;
            if (created == null)
            {
                throw new InvalidOperationException("Failed to instantiate the Player prefab.");
            }

            created.transform.SetParent(levelRoot, true);
            return created;
        }

        private static GameObject GetOrCreateUniqueRoot(Scene scene, string objectName)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .Where(root => string.Equals(root.name, objectName, StringComparison.Ordinal))
                .ToArray();

            if (matches.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Scene contains more than one root object named '{objectName}'.");
            }

            if (matches.Length == 1)
            {
                return matches[0];
            }

            GameObject created = new GameObject(objectName);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static GameObject GetOrCreateUniqueChild(Transform parent, string objectName)
        {
            GameObject[] matches = parent.Cast<Transform>()
                .Where(child => string.Equals(child.name, objectName, StringComparison.Ordinal))
                .Select(child => child.gameObject)
                .ToArray();

            if (matches.Length > 1)
            {
                throw new InvalidOperationException(
                    $"'{parent.name}' contains more than one child named '{objectName}'.");
            }

            if (matches.Length == 1)
            {
                return matches[0];
            }

            GameObject created = new GameObject(objectName);
            created.transform.SetParent(parent, false);
            return created;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
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
