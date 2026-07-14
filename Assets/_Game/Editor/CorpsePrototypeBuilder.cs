using System;
using System.IO;
using CorpseMechanism.Corpse;
using UnityEditor;
using UnityEngine;

namespace CorpseMechanism.Editor
{
    public static class CorpsePrototypeBuilder
    {
        public const string PrefabPath = "Assets/_Game/Prefabs/NormalCorpse.prefab";

        [MenuItem("Corpse Mechanism/Corpse/Create or Update Normal Corpse Prefab")]
        public static void CreateOrUpdateNormalCorpsePrefabFromMenu()
        {
            CreateOrUpdateNormalCorpsePrefab();
            Debug.Log($"Created or updated normal corpse prefab: {PrefabPath}");
        }

        public static GameObject CreateOrUpdateNormalCorpsePrefab()
        {
            EnsurePrefabDirectoryExists();

            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            bool loadedPrefabContents = existingPrefab != null;
            GameObject prefabRoot = loadedPrefabContents
                ? PrefabUtility.LoadPrefabContents(PrefabPath)
                : new GameObject("NormalCorpse");

            try
            {
                ConfigurePrefab(prefabRoot);
                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(
                    prefabRoot,
                    PrefabPath,
                    out bool savedSuccessfully);

                if (!savedSuccessfully || savedPrefab == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to save normal corpse prefab at '{PrefabPath}'.");
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

        private static void ConfigurePrefab(GameObject root)
        {
            if (root.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                root.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                throw new InvalidOperationException(
                    "Normal corpse prefab contains unsupported 3D physics components.");
            }

            root.name = "NormalCorpse";
            root.layer = 0;
            root.SetActive(true);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            SpriteRenderer renderer = GetOrAddSingleComponent<SpriteRenderer>(root);
            Rigidbody2D body = GetOrAddSingleComponent<Rigidbody2D>(root);
            BoxCollider2D collider = GetOrAddSingleComponent<BoxCollider2D>(root);
            CorpseController controller = GetOrAddSingleComponent<CorpseController>(root);

            renderer.sprite = LoadBuiltInGreyboxSprite();
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(1.4f, 0.55f);
            renderer.color = new Color(0.42f, 0.46f, 0.5f, 1f);

            body.bodyType = RigidbodyType2D.Dynamic;
            body.mass = 1.5f;
            body.gravityScale = 2.5f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;

            collider.size = new Vector2(1.4f, 0.55f);
            collider.offset = Vector2.zero;
            collider.isTrigger = false;

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(body);
            EditorUtility.SetDirty(collider);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(root);
        }

        private static T GetOrAddSingleComponent<T>(GameObject gameObject) where T : Component
        {
            T[] components = gameObject.GetComponents<T>();
            if (components.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Normal corpse prefab contains more than one {typeof(T).Name} component.");
            }

            return components.Length == 1 ? components[0] : gameObject.AddComponent<T>();
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
