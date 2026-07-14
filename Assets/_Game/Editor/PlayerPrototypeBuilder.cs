using System;
using System.IO;
using CorpseMechanism.Player;
using UnityEditor;
using UnityEngine;

namespace CorpseMechanism.Editor
{
    public static class PlayerPrototypeBuilder
    {
        public const string PrefabPath = "Assets/_Game/Prefabs/Player.prefab";

        private const string MenuPath = "Corpse Mechanism/Player/Create or Update Player Prefab";

        [MenuItem(MenuPath)]
        public static void CreateOrUpdatePlayerPrefabFromMenu()
        {
            CreateOrUpdatePlayerPrefab();
            Debug.Log($"Created or updated player prefab: {PrefabPath}");
        }

        public static GameObject CreateOrUpdatePlayerPrefab()
        {
            EnsurePrefabDirectoryExists();

            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            bool loadedPrefabContents = existingPrefab != null;
            GameObject prefabRoot = loadedPrefabContents
                ? PrefabUtility.LoadPrefabContents(PrefabPath)
                : new GameObject("Player");

            try
            {
                ConfigurePrefabRoot(prefabRoot);

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(
                    prefabRoot,
                    PrefabPath,
                    out bool savedSuccessfully);

                if (!savedSuccessfully || savedPrefab == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to save player prefab at '{PrefabPath}'.");
                }
            }
            finally
            {
                if (loadedPrefabContents)
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(prefabRoot);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        private static void ConfigurePrefabRoot(GameObject root)
        {
            if (root.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                root.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                throw new InvalidOperationException(
                    "Player prefab contains unsupported 3D physics components.");
            }

            root.name = "Player";
            root.layer = 0;
            root.SetActive(true);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            SpriteRenderer renderer = GetOrAddSingleComponent<SpriteRenderer>(root);
            Rigidbody2D body = GetOrAddSingleComponent<Rigidbody2D>(root);
            CapsuleCollider2D bodyCollider = GetOrAddSingleComponent<CapsuleCollider2D>(root);
            LegacyPlayerInputSource input = GetOrAddSingleComponent<LegacyPlayerInputSource>(root);
            GroundProbe2D groundProbe = GetOrAddSingleComponent<GroundProbe2D>(root);
            PlayerMotor2D motor = GetOrAddSingleComponent<PlayerMotor2D>(root);
            PlayerLifeController lifeController =
                GetOrAddSingleComponent<PlayerLifeController>(root);

            renderer.sprite = LoadBuiltInGreyboxSprite();
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(0.8f, 1.6f);
            renderer.color = new Color(0.25f, 0.78f, 0.9f, 1f);

            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 2.5f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            bodyCollider.direction = CapsuleDirection2D.Vertical;
            bodyCollider.size = new Vector2(0.8f, 1.6f);
            bodyCollider.offset = Vector2.zero;
            bodyCollider.isTrigger = false;

            groundProbe.Configure(bodyCollider, 1 << 0, 0.08f);
            motor.Configure(body, groundProbe, input, 5f, 7f);
            motor.SetControlEnabled(true);
            lifeController.Configure(motor, body, bodyCollider, groundProbe, renderer);

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(body);
            EditorUtility.SetDirty(bodyCollider);
            EditorUtility.SetDirty(input);
            EditorUtility.SetDirty(groundProbe);
            EditorUtility.SetDirty(motor);
            EditorUtility.SetDirty(lifeController);
            EditorUtility.SetDirty(root);
        }

        private static T GetOrAddSingleComponent<T>(GameObject gameObject) where T : Component
        {
            T[] components = gameObject.GetComponents<T>();
            if (components.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Player prefab contains more than one {typeof(T).Name} component.");
            }

            return components.Length == 1
                ? components[0]
                : gameObject.AddComponent<T>();
        }

        private static Sprite LoadBuiltInGreyboxSprite()
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    "Unity built-in greybox sprite 'UI/Skin/UISprite.psd' is unavailable.");
            }

            return sprite;
        }

        private static void EnsurePrefabDirectoryExists()
        {
            string absoluteDirectory = Path.GetFullPath(
                Path.Combine(Application.dataPath, "_Game", "Prefabs"));
            Directory.CreateDirectory(absoluteDirectory);
            AssetDatabase.Refresh();
        }
    }
}
