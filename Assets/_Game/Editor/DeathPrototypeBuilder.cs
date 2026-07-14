using System;
using System.IO;
using CorpseMechanism.Death;
using UnityEditor;
using UnityEngine;

namespace CorpseMechanism.Editor
{
    public static class DeathPrototypeBuilder
    {
        public const string HazardPrefabPath = "Assets/_Game/Prefabs/NormalHazard.prefab";

        [MenuItem("Corpse Mechanism/Death/Create or Update Normal Hazard Prefab")]
        public static void CreateOrUpdateNormalHazardPrefabFromMenu()
        {
            CreateOrUpdateNormalHazardPrefab();
            Debug.Log($"Created or updated normal hazard prefab: {HazardPrefabPath}");
        }

        public static GameObject CreateOrUpdateNormalHazardPrefab()
        {
            EnsurePrefabDirectoryExists();

            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HazardPrefabPath);
            bool loadedPrefabContents = existingPrefab != null;
            GameObject prefabRoot = loadedPrefabContents
                ? PrefabUtility.LoadPrefabContents(HazardPrefabPath)
                : new GameObject("NormalHazard");

            try
            {
                ConfigurePrefab(prefabRoot);
                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(
                    prefabRoot,
                    HazardPrefabPath,
                    out bool savedSuccessfully);

                if (!savedSuccessfully || savedPrefab == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to save normal hazard prefab at '{HazardPrefabPath}'.");
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
            return AssetDatabase.LoadAssetAtPath<GameObject>(HazardPrefabPath);
        }

        private static void ConfigurePrefab(GameObject root)
        {
            if (root.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                root.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                throw new InvalidOperationException(
                    "Normal hazard prefab contains unsupported 3D physics components.");
            }

            root.name = "NormalHazard";
            root.layer = 0;
            root.SetActive(true);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            SpriteRenderer renderer = GetOrAddSingleComponent<SpriteRenderer>(root);
            BoxCollider2D collider = GetOrAddSingleComponent<BoxCollider2D>(root);
            NormalHazard hazard = GetOrAddSingleComponent<NormalHazard>(root);

            renderer.sprite = LoadBuiltInGreyboxSprite();
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(1.2f, 1.2f);
            renderer.color = new Color(0.88f, 0.18f, 0.2f, 1f);

            collider.size = new Vector2(1.2f, 1.2f);
            collider.offset = Vector2.zero;
            collider.isTrigger = true;

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(collider);
            EditorUtility.SetDirty(hazard);
            EditorUtility.SetDirty(root);
        }

        private static T GetOrAddSingleComponent<T>(GameObject gameObject) where T : Component
        {
            T[] components = gameObject.GetComponents<T>();
            if (components.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Normal hazard prefab contains more than one {typeof(T).Name} component.");
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
