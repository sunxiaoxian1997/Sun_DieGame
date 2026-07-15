using System;
using System.IO;
using CorpseMechanism.Interaction;
using UnityEditor;
using UnityEngine;

namespace CorpseMechanism.Editor
{
    public static class InteractionPrototypeBuilder
    {
        public const string PressurePlatePrefabPath = "Assets/_Game/Prefabs/PressurePlate.prefab";
        public const string DoorPrefabPath = "Assets/_Game/Prefabs/Door.prefab";

        [MenuItem("Corpse Mechanism/Interaction/Create or Update Pressure Plate Prefab")]
        public static void CreateOrUpdatePressurePlatePrefabFromMenu()
        {
            CreateOrUpdatePressurePlatePrefab();
        }

        [MenuItem("Corpse Mechanism/Interaction/Create or Update Door Prefab")]
        public static void CreateOrUpdateDoorPrefabFromMenu()
        {
            CreateOrUpdateDoorPrefab();
        }

        public static GameObject CreateOrUpdatePressurePlatePrefab()
        {
            EnsurePrefabDirectoryExists();
            return CreateOrUpdatePrefab(PressurePlatePrefabPath, "PressurePlate", ConfigurePressurePlate);
        }

        public static GameObject CreateOrUpdateDoorPrefab()
        {
            EnsurePrefabDirectoryExists();
            return CreateOrUpdatePrefab(DoorPrefabPath, "Door", ConfigureDoor);
        }

        private static GameObject CreateOrUpdatePrefab(
            string path,
            string rootName,
            Action<GameObject> configure)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            bool loadedPrefabContents = existingPrefab != null;
            GameObject root = loadedPrefabContents
                ? PrefabUtility.LoadPrefabContents(path)
                : new GameObject(rootName);

            try
            {
                configure(root);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    path,
                    out bool savedSuccessfully);
                if (!savedSuccessfully || saved == null)
                {
                    throw new InvalidOperationException($"Failed to save interaction prefab at '{path}'.");
                }
            }
            finally
            {
                if (loadedPrefabContents)
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
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void ConfigurePressurePlate(GameObject root)
        {
            Reject3DPhysics(root, "PressurePlate");
            ConfigureRoot(root, "PressurePlate");

            SpriteRenderer renderer = GetOrAddSingleComponent<SpriteRenderer>(root);
            BoxCollider2D trigger = GetOrAddSingleComponent<BoxCollider2D>(root);
            PressurePlate plate = GetOrAddSingleComponent<PressurePlate>(root);

            renderer.sprite = LoadBuiltInSprite();
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(2f, 0.25f);
            renderer.color = new Color(0.9f, 0.7f, 0.12f, 1f);

            trigger.isTrigger = true;
            trigger.size = new Vector2(2f, 0.8f);
            trigger.offset = new Vector2(0f, 0.3f);
            plate.Configure(trigger, renderer, 1f);

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(trigger);
            EditorUtility.SetDirty(plate);
            EditorUtility.SetDirty(root);
        }

        private static void ConfigureDoor(GameObject root)
        {
            Reject3DPhysics(root, "Door");
            ConfigureRoot(root, "Door");

            SpriteRenderer renderer = GetOrAddSingleComponent<SpriteRenderer>(root);
            BoxCollider2D blocker = GetOrAddSingleComponent<BoxCollider2D>(root);
            DoorController door = GetOrAddSingleComponent<DoorController>(root);

            renderer.sprite = LoadBuiltInSprite();
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(0.8f, 3.5f);
            renderer.color = new Color(0.25f, 0.45f, 0.85f, 1f);

            blocker.isTrigger = false;
            blocker.size = new Vector2(0.8f, 3.5f);
            blocker.offset = Vector2.zero;
            door.Configure(null, blocker, root.transform, renderer, new Vector3(0f, 4f, 0f));

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(blocker);
            EditorUtility.SetDirty(door);
            EditorUtility.SetDirty(root);
        }

        private static void ConfigureRoot(GameObject root, string name)
        {
            root.name = name;
            root.layer = 0;
            root.SetActive(true);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
        }

        private static void Reject3DPhysics(GameObject root, string label)
        {
            if (root.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                root.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                throw new InvalidOperationException($"{label} prefab contains unsupported 3D physics.");
            }
        }

        private static T GetOrAddSingleComponent<T>(GameObject root) where T : Component
        {
            T[] components = root.GetComponents<T>();
            if (components.Length > 1)
            {
                throw new InvalidOperationException(
                    $"'{root.name}' contains more than one {typeof(T).Name} component.");
            }

            return components.Length == 1 ? components[0] : root.AddComponent<T>();
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

        private static void EnsurePrefabDirectoryExists()
        {
            Directory.CreateDirectory(Path.GetFullPath(
                Path.Combine(Application.dataPath, "_Game", "Prefabs")));
            AssetDatabase.Refresh();
        }
    }
}
