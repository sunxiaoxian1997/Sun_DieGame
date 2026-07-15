using System;
using System.Collections;
using System.Collections.Generic;
using CorpseMechanism.Corpse;
using CorpseMechanism.Death;
using CorpseMechanism.Interaction;
using CorpseMechanism.Level;
using CorpseMechanism.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CorpseMechanism.Tests.PlayMode
{
    public sealed class PressureDoorPlayModeTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject gameObject in _objects)
            {
                if (gameObject != null)
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
            }

            _objects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerWeight_ActivatesPlate()
        {
            PressurePlate plate = CreatePlate(Vector2.zero, 1f);
            CreateWeight("PlayerWeight", Vector2.zero, 1f);

            yield return RefreshAfterPhysics(plate);

            Assert.That(plate.IsPressed, Is.True);
            Assert.That(plate.CurrentWeight, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator WeightLeavingPlate_ReleasesIt()
        {
            PressurePlate plate = CreatePlate(Vector2.zero, 1f);
            WeightProvider weight = CreateWeight("LeavingWeight", Vector2.zero, 1f);
            yield return RefreshAfterPhysics(plate);

            weight.transform.position = new Vector2(10f, 0f);
            yield return RefreshAfterPhysics(plate);

            Assert.That(plate.IsPressed, Is.False);
            Assert.That(plate.CurrentWeight, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CorpseWeight_ActivatesPlateWithoutPlayerDependency()
        {
            PressurePlate plate = CreatePlate(Vector2.zero, 1f);
            GameObject corpseObject = new GameObject("WeightedCorpse");
            corpseObject.transform.position = Vector2.zero;
            corpseObject.AddComponent<Rigidbody2D>().gravityScale = 0f;
            corpseObject.AddComponent<BoxCollider2D>();
            corpseObject.AddComponent<CorpseController>();
            WeightProvider weight = corpseObject.AddComponent<WeightProvider>();
            weight.Configure(1f);
            _objects.Add(corpseObject);

            yield return RefreshAfterPhysics(plate);

            Assert.That(plate.IsPressed, Is.True);
        }

        [UnityTest]
        public IEnumerator MultipleCollidersOnOneProvider_CountOnlyOnce()
        {
            PressurePlate plate = CreatePlate(Vector2.zero, 2f);
            GameObject root = new GameObject("MultiColliderWeight");
            root.transform.position = Vector2.zero;
            WeightProvider provider = root.AddComponent<WeightProvider>();
            provider.Configure(1f);
            root.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.5f);
            GameObject child = new GameObject("SecondCollider");
            child.transform.SetParent(root.transform, false);
            child.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.5f);
            _objects.Add(root);

            yield return RefreshAfterPhysics(plate);

            Assert.That(plate.CurrentWeight, Is.EqualTo(1f));
            Assert.That(plate.IsPressed, Is.False);
        }

        [UnityTest]
        public IEnumerator OneColliderLeavingWhileAnotherRemains_KeepsWeight()
        {
            PressurePlate plate = CreatePlate(Vector2.zero, 1f);
            GameObject root = new GameObject("PartialOverlapWeight");
            root.transform.position = Vector2.zero;
            WeightProvider provider = root.AddComponent<WeightProvider>();
            provider.Configure(1f);
            root.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.5f);
            GameObject child = new GameObject("MovingCollider");
            child.transform.SetParent(root.transform, false);
            child.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.5f);
            _objects.Add(root);
            yield return RefreshAfterPhysics(plate);

            child.transform.position = new Vector2(10f, 0f);
            yield return RefreshAfterPhysics(plate);

            Assert.That(plate.CurrentWeight, Is.EqualTo(1f));
            Assert.That(plate.IsPressed, Is.True);
        }

        [UnityTest]
        public IEnumerator DisabledWeightProvider_ReleasesPlate()
        {
            PressurePlate plate = CreatePlate(Vector2.zero, 1f);
            WeightProvider weight = CreateWeight("DisabledWeight", Vector2.zero, 1f);
            yield return RefreshAfterPhysics(plate);

            weight.enabled = false;
            yield return RefreshAfterPhysics(plate);

            Assert.That(plate.IsPressed, Is.False);
        }

        [UnityTest]
        public IEnumerator DestroyedWeight_ReleasesPlateWithoutException()
        {
            PressurePlate plate = CreatePlate(Vector2.zero, 1f);
            WeightProvider weight = CreateWeight("DestroyedWeight", Vector2.zero, 1f);
            yield return RefreshAfterPhysics(plate);

            UnityEngine.Object.Destroy(weight.gameObject);
            yield return null;
            yield return RefreshAfterPhysics(plate);

            Assert.That(plate.IsPressed, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CombinedWeights_CanMeetThreshold()
        {
            PressurePlate plate = CreatePlate(Vector2.zero, 2f);
            CreateWeight("CombinedWeightA", new Vector2(-0.3f, 0f), 1f);
            CreateWeight("CombinedWeightB", new Vector2(0.3f, 0f), 1f);

            yield return RefreshAfterPhysics(plate);

            Assert.That(plate.CurrentWeight, Is.EqualTo(2f));
            Assert.That(plate.IsPressed, Is.True);
        }

        [UnityTest]
        public IEnumerator Door_FollowsPlateAndTogglesPhysicalBlocker()
        {
            PressurePlate plate = CreatePlate(Vector2.zero, 1f);
            DoorController door = CreateDoor(plate, new Vector2(5f, 0f));
            WeightProvider weight = CreateWeight("DoorWeight", Vector2.zero, 1f);

            yield return RefreshAfterPhysics(plate);
            Assert.That(door.IsOpen, Is.True);
            Assert.That(door.BlockingCollider.enabled, Is.False);

            weight.transform.position = new Vector2(10f, 0f);
            yield return RefreshAfterPhysics(plate);
            Assert.That(door.IsOpen, Is.False);
            Assert.That(door.BlockingCollider.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator EnabledDoor_SynchronizesAlreadyPressedPlateState()
        {
            PressurePlate plate = CreatePlate(Vector2.zero, 1f);
            CreateWeight("InitialStateWeight", Vector2.zero, 1f);
            yield return RefreshAfterPhysics(plate);

            DoorController door = CreateDoor(plate, new Vector2(5f, 0f), false);
            door.gameObject.SetActive(true);
            yield return null;

            Assert.That(door.IsOpen, Is.True);
            Assert.That(door.BlockingCollider.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator NormalDeath_CorpsePersistsOnPlateAfterPlayerRespawns()
        {
            PressurePlate plate = CreatePlate(Vector2.zero, 1f);
            DeathCorpseRig rig = CreateDeathCorpseRig();
            rig.Body.position = Vector2.zero;
            Physics2D.SyncTransforms();

            Assert.That(rig.Life.TryKill(new FakeDamageSource()), Is.True);
            yield return WaitUntil(() => rig.Session.SpawnedCorpseCount == 1);
            yield return WaitUntil(() => rig.Life.State == PlayerLifeState.Alive);
            yield return RefreshAfterPhysics(plate);

            Assert.That(rig.Session.SpawnedCorpses[0], Is.Not.Null);
            Assert.That(plate.IsPressed, Is.True);
            Assert.That(Vector2.Distance(rig.Body.position, rig.Spawn.position), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator FreshInteractionLifecycle_StartsReleasedAndClosed()
        {
            GameObject oldRoot = new GameObject("OldInteractionLifecycle");
            _objects.Add(oldRoot);
            PressurePlate oldPlate = CreatePlate(Vector2.zero, 1f, oldRoot.transform);
            DoorController oldDoor = CreateDoor(oldPlate, new Vector2(5f, 0f), true, oldRoot.transform);
            CreateWeight("OldLifecycleWeight", Vector2.zero, 1f, oldRoot.transform);
            yield return RefreshAfterPhysics(oldPlate);
            Assert.That(oldDoor.IsOpen, Is.True);

            UnityEngine.Object.Destroy(oldRoot);
            yield return null;
            PressurePlate newPlate = CreatePlate(new Vector2(20f, 0f), 1f);
            DoorController newDoor = CreateDoor(newPlate, new Vector2(25f, 0f));
            yield return RefreshAfterPhysics(newPlate);

            Assert.That(newPlate.IsPressed, Is.False);
            Assert.That(newDoor.IsOpen, Is.False);
            Assert.That(newDoor.BlockingCollider.enabled, Is.True);
        }

        private PressurePlate CreatePlate(
            Vector2 position,
            float threshold,
            Transform parent = null)
        {
            GameObject plateObject = new GameObject("TestPressurePlate");
            plateObject.transform.SetParent(parent, false);
            plateObject.transform.position = position;
            SpriteRenderer renderer = plateObject.AddComponent<SpriteRenderer>();
            BoxCollider2D trigger = plateObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(2f, 1f);
            PressurePlate plate = plateObject.AddComponent<PressurePlate>();
            plate.Configure(trigger, renderer, threshold);
            if (parent == null)
            {
                _objects.Add(plateObject);
            }

            return plate;
        }

        private WeightProvider CreateWeight(
            string name,
            Vector2 position,
            float weight,
            Transform parent = null)
        {
            GameObject weightObject = new GameObject(name);
            weightObject.transform.SetParent(parent, false);
            weightObject.transform.position = position;
            weightObject.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.5f);
            WeightProvider provider = weightObject.AddComponent<WeightProvider>();
            provider.Configure(weight);
            if (parent == null)
            {
                _objects.Add(weightObject);
            }

            return provider;
        }

        private DoorController CreateDoor(
            PressurePlate plate,
            Vector2 position,
            bool active = true,
            Transform parent = null)
        {
            GameObject doorObject = new GameObject("TestDoor");
            doorObject.SetActive(false);
            doorObject.transform.SetParent(parent, false);
            doorObject.transform.position = position;
            BoxCollider2D blocker = doorObject.AddComponent<BoxCollider2D>();
            SpriteRenderer renderer = doorObject.AddComponent<SpriteRenderer>();
            DoorController door = doorObject.AddComponent<DoorController>();
            door.Configure(plate, blocker, doorObject.transform, renderer, new Vector3(0f, 4f));
            doorObject.SetActive(active);
            if (parent == null)
            {
                _objects.Add(doorObject);
            }

            return door;
        }

        private DeathCorpseRig CreateDeathCorpseRig()
        {
            GameObject spawnObject = new GameObject("InteractionRespawnPoint");
            spawnObject.transform.position = new Vector2(-4f, 0f);
            _objects.Add(spawnObject);

            GameObject player = new GameObject("InteractionPlayer");
            player.SetActive(false);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            GroundProbe2D probe = player.AddComponent<GroundProbe2D>();
            PlayerMotor2D motor = player.AddComponent<PlayerMotor2D>();
            PlayerLifeController life = player.AddComponent<PlayerLifeController>();
            player.AddComponent<WeightProvider>().Configure(1f);
            probe.Configure(collider, 1 << 0, 0.08f);
            motor.Configure(body, probe, new FakeInput());
            life.Configure(motor, body, collider, probe, renderer);
            player.SetActive(true);
            _objects.Add(player);

            GameObject corpseTemplateObject = new GameObject("InteractionCorpseTemplate");
            corpseTemplateObject.transform.position = new Vector2(100f, 100f);
            corpseTemplateObject.AddComponent<Rigidbody2D>().gravityScale = 0f;
            corpseTemplateObject.AddComponent<BoxCollider2D>();
            CorpseController corpsePrefab = corpseTemplateObject.AddComponent<CorpseController>();
            corpseTemplateObject.AddComponent<WeightProvider>().Configure(1f);
            _objects.Add(corpseTemplateObject);

            GameObject runtimeRoot = new GameObject("InteractionRuntimeCorpses");
            _objects.Add(runtimeRoot);
            GameObject systems = new GameObject("InteractionSystems");
            RespawnController respawn = systems.AddComponent<RespawnController>();
            LevelSession session = systems.AddComponent<LevelSession>();
            CorpseFactory factory = systems.AddComponent<CorpseFactory>();
            respawn.Configure(life, spawnObject.transform, 0.02f);
            session.Configure(life, respawn, 3, new NoOpReloader());
            factory.Configure(life, corpsePrefab, session, runtimeRoot.transform);
            _objects.Add(systems);

            return new DeathCorpseRig(body, life, session, spawnObject.transform);
        }

        private static IEnumerator RefreshAfterPhysics(PressurePlate plate)
        {
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            plate.RefreshWeights();
        }

        private static IEnumerator WaitUntil(Func<bool> condition, float timeout = 1f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, "Condition was not reached before timeout.");
        }

        private readonly struct DeathCorpseRig
        {
            public DeathCorpseRig(
                Rigidbody2D body,
                PlayerLifeController life,
                LevelSession session,
                Transform spawn)
            {
                Body = body;
                Life = life;
                Session = session;
                Spawn = spawn;
            }

            public Rigidbody2D Body { get; }
            public PlayerLifeController Life { get; }
            public LevelSession Session { get; }
            public Transform Spawn { get; }
        }

        private sealed class FakeDamageSource : IDamageSource
        {
            public DeathType DeathType => DeathType.Normal;
        }

        private sealed class FakeInput : IPlayerInputSource
        {
            public float Horizontal => 0f;
            public bool ConsumeJumpPressed() => false;
            public void SetInputEnabled(bool enabled) { }
        }

        private sealed class NoOpReloader : IActiveSceneReloader
        {
            public void ReloadActiveScene() { }
        }
    }
}
