using System;
using System.Collections;
using System.Collections.Generic;
using CorpseMechanism.Corpse;
using CorpseMechanism.Death;
using CorpseMechanism.Level;
using CorpseMechanism.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CorpseMechanism.Tests.PlayMode
{
    public sealed class CorpsePlayModeTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly FakeDamageSource _damageSource = new FakeDamageSource();

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
        public IEnumerator AcceptedDeath_CreatesAndRegistersExactlyOneCorpse()
        {
            CorpseRig rig = CreateRig(3, 0.2f);

            Assert.That(rig.Life.TryKill(_damageSource), Is.True);
            yield return WaitUntil(() => rig.Session.SpawnedCorpseCount == 1);

            Assert.That(rig.Session.SpawnedCorpseCount, Is.EqualTo(1));
            Assert.That(
                rig.RuntimeParent.GetComponentsInChildren<CorpseController>().Length,
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RepeatedDeathRequest_DoesNotCreateAnotherCorpse()
        {
            CorpseRig rig = CreateRig(3, 0.2f);

            Assert.That(rig.Life.TryKill(_damageSource), Is.True);
            Assert.That(rig.Life.TryKill(_damageSource), Is.False);
            yield return null;

            Assert.That(rig.Session.SpawnedCorpseCount, Is.EqualTo(1));
            Assert.That(rig.Session.RemainingLives, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Corpse_UsesDeathContextPositionNotRespawnPosition()
        {
            CorpseRig rig = CreateRig(3, 0.02f);
            Vector2 deathPosition = new Vector2(2.5f, 1.25f);
            rig.Body.position = deathPosition;
            Physics2D.SyncTransforms();

            rig.Life.TryKill(_damageSource);
            yield return WaitUntil(() => rig.Life.State == PlayerLifeState.Alive);

            CorpseController corpse = rig.Session.SpawnedCorpses[0];
            Assert.That(Vector2.Distance(corpse.transform.position, deathPosition), Is.LessThan(0.01f));
            Assert.That(Vector2.Distance(rig.Body.position, rig.Spawn.position), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator Corpse_PersistsAcrossRespawnAndPlayerInstanceIsReused()
        {
            CorpseRig rig = CreateRig(3, 0.02f);
            PlayerLifeController originalPlayer = rig.Life;
            rig.Life.TryKill(_damageSource);
            yield return WaitUntil(() => rig.Session.SpawnedCorpseCount == 1);
            CorpseController originalCorpse = rig.Session.SpawnedCorpses[0];
            yield return WaitUntil(() => rig.Life.State == PlayerLifeState.Alive);

            Assert.That(rig.Life, Is.SameAs(originalPlayer));
            Assert.That(rig.Session.SpawnedCorpses[0], Is.SameAs(originalCorpse));
            Assert.That(originalCorpse, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ThreeDeaths_CreateThreeCorpsesAndFinalDeathFails()
        {
            CorpseRig rig = CreateRig(3, 0.01f);

            for (int death = 1; death <= 3; death++)
            {
                rig.Body.position = new Vector2(death, 0f);
                Physics2D.SyncTransforms();
                Assert.That(rig.Life.TryKill(_damageSource), Is.True);
                int expectedCount = death;
                yield return WaitUntil(() => rig.Session.SpawnedCorpseCount == expectedCount);

                if (death < 3)
                {
                    yield return WaitUntil(() => rig.Life.State == PlayerLifeState.Alive);
                }
            }

            Assert.That(rig.Session.SpawnedCorpseCount, Is.EqualTo(3));
            Assert.That(rig.Session.RemainingLives, Is.Zero);
            Assert.That(rig.Session.State, Is.EqualTo(LevelSessionState.Failed));
            Assert.That(rig.Life.State, Is.EqualTo(PlayerLifeState.Dead));
            Assert.That(rig.Life.TryKill(_damageSource), Is.False);
            yield return null;
            Assert.That(rig.Session.SpawnedCorpseCount, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator GeneratedCorpse_HasNoActivePlayerComponents()
        {
            CorpseRig rig = CreateRig(3, 0.2f);
            rig.Life.TryKill(_damageSource);
            yield return WaitUntil(() => rig.Session.SpawnedCorpseCount == 1);

            GameObject corpse = rig.Session.SpawnedCorpses[0].gameObject;
            Assert.That(corpse.GetComponent<LegacyPlayerInputSource>(), Is.Null);
            Assert.That(corpse.GetComponent<PlayerMotor2D>(), Is.Null);
            Assert.That(corpse.GetComponent<PlayerLifeController>(), Is.Null);
            Assert.That(corpse.GetComponent<GroundProbe2D>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator NormalHazard_IgnoresCorpseOverlap()
        {
            CorpseRig rig = CreateRig(3, 0.02f);
            int acceptedDeaths = 0;
            rig.Life.DeathAccepted += _ => acceptedDeaths++;
            rig.Life.TryKill(_damageSource);
            yield return WaitUntil(() => rig.Life.State == PlayerLifeState.Alive);
            CorpseController corpse = rig.Session.SpawnedCorpses[0];

            GameObject hazardObject = new GameObject("CorpseOverlapHazard");
            hazardObject.transform.position = corpse.transform.position;
            BoxCollider2D hazardCollider = hazardObject.AddComponent<BoxCollider2D>();
            hazardCollider.size = new Vector2(2f, 2f);
            hazardCollider.isTrigger = true;
            hazardObject.AddComponent<NormalHazard>();
            _objects.Add(hazardObject);
            Physics2D.SyncTransforms();

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(acceptedDeaths, Is.EqualTo(1));
            Assert.That(rig.Session.RemainingLives, Is.EqualTo(2));
            Assert.That(rig.Session.SpawnedCorpseCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Player_CanSettleAndGroundOnCorpse()
        {
            CorpseRig rig = CreateRig(3, 0.2f, 2.5f);
            CreateGround();
            CorpseController corpse = UnityEngine.Object.Instantiate(
                rig.CorpsePrefab,
                new Vector2(0f, 0.25f),
                Quaternion.identity,
                rig.RuntimeParent);
            corpse.Initialize(new DeathContext(DeathType.Normal, corpse.transform.position));
            rig.Session.RegisterCorpse(corpse);
            rig.Body.position = new Vector2(0f, 2f);
            Physics2D.SyncTransforms();

            yield return WaitUntil(
                () => rig.Probe.RefreshGroundedState() && Mathf.Abs(rig.Body.velocity.y) < 0.2f,
                2f);

            Collider2D corpseCollider = corpse.GetComponent<Collider2D>();
            Collider2D playerCollider = rig.Life.GetComponent<Collider2D>();
            Assert.That(playerCollider.bounds.min.y, Is.GreaterThan(corpseCollider.bounds.max.y - 0.08f));
            Assert.That(rig.Probe.IsGrounded, Is.True);
        }

        [UnityTest]
        public IEnumerator RestartLifecycle_DestroysRuntimeCorpsesAndNewSessionStartsEmpty()
        {
            CorpseRig rig = CreateRig(3, 0.2f);
            rig.Session.Configure(
                rig.Life,
                rig.Respawn,
                3,
                new DestroyRootReloader(rig.RuntimeParent.gameObject));
            rig.Life.TryKill(_damageSource);
            yield return WaitUntil(() => rig.Session.SpawnedCorpseCount == 1);
            CorpseController oldCorpse = rig.Session.SpawnedCorpses[0];

            rig.Session.RestartLevel();
            yield return null;

            GameObject replacementObject = new GameObject("ReplacementLevelSession");
            LevelSession replacement = replacementObject.AddComponent<LevelSession>();
            replacement.Configure(null, new FakeRespawnScheduler(), 3, new NoOpReloader());
            _objects.Add(replacementObject);

            Assert.That(oldCorpse == null, Is.True);
            Assert.That(replacement.SpawnedCorpseCount, Is.Zero);
        }

        private CorpseRig CreateRig(
            int initialLives,
            float respawnDelay,
            float playerGravity = 0f)
        {
            GameObject spawnObject = new GameObject("CorpseTestSpawn");
            spawnObject.transform.position = new Vector3(-4f, 0f, 0f);
            _objects.Add(spawnObject);

            GameObject player = new GameObject("CorpseTestPlayer");
            player.SetActive(false);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = playerGravity;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.8f, 1.6f);
            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            GroundProbe2D probe = player.AddComponent<GroundProbe2D>();
            FakeInput input = new FakeInput();
            PlayerMotor2D motor = player.AddComponent<PlayerMotor2D>();
            PlayerLifeController life = player.AddComponent<PlayerLifeController>();
            probe.Configure(collider, 1 << 0, 0.1f);
            motor.Configure(body, probe, input);
            life.Configure(motor, body, collider, probe, renderer);
            player.SetActive(true);
            _objects.Add(player);

            GameObject corpseTemplateObject = new GameObject("CorpseTemplate");
            corpseTemplateObject.transform.position = new Vector3(100f, 100f, 0f);
            Rigidbody2D corpseBody = corpseTemplateObject.AddComponent<Rigidbody2D>();
            corpseBody.gravityScale = 2.5f;
            corpseBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            BoxCollider2D corpseCollider = corpseTemplateObject.AddComponent<BoxCollider2D>();
            corpseCollider.size = new Vector2(1.4f, 0.55f);
            corpseTemplateObject.AddComponent<SpriteRenderer>();
            CorpseController corpsePrefab = corpseTemplateObject.AddComponent<CorpseController>();
            _objects.Add(corpseTemplateObject);

            GameObject runtimeParentObject = new GameObject("RuntimeCorpseTestParent");
            _objects.Add(runtimeParentObject);

            GameObject systems = new GameObject("CorpseTestSystems");
            RespawnController respawn = systems.AddComponent<RespawnController>();
            LevelSession session = systems.AddComponent<LevelSession>();
            CorpseFactory factory = systems.AddComponent<CorpseFactory>();
            respawn.Configure(life, spawnObject.transform, respawnDelay);
            session.Configure(life, respawn, initialLives, new NoOpReloader());
            factory.Configure(life, corpsePrefab, session, runtimeParentObject.transform);
            _objects.Add(systems);

            Physics2D.SyncTransforms();
            return new CorpseRig(
                body,
                probe,
                life,
                respawn,
                session,
                corpsePrefab,
                runtimeParentObject.transform,
                spawnObject.transform);
        }

        private void CreateGround()
        {
            GameObject ground = new GameObject("CorpseStandingGround");
            ground.transform.position = new Vector3(0f, -1f, 0f);
            BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(10f, 1f);
            _objects.Add(ground);
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

        private readonly struct CorpseRig
        {
            public CorpseRig(
                Rigidbody2D body,
                GroundProbe2D probe,
                PlayerLifeController life,
                RespawnController respawn,
                LevelSession session,
                CorpseController corpsePrefab,
                Transform runtimeParent,
                Transform spawn)
            {
                Body = body;
                Probe = probe;
                Life = life;
                Respawn = respawn;
                Session = session;
                CorpsePrefab = corpsePrefab;
                RuntimeParent = runtimeParent;
                Spawn = spawn;
            }

            public Rigidbody2D Body { get; }
            public GroundProbe2D Probe { get; }
            public PlayerLifeController Life { get; }
            public RespawnController Respawn { get; }
            public LevelSession Session { get; }
            public CorpseController CorpsePrefab { get; }
            public Transform RuntimeParent { get; }
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

        private sealed class FakeRespawnScheduler : IRespawnScheduler
        {
            public bool RequestRespawn(DeathContext deathContext) => true;
        }

        private sealed class NoOpReloader : IActiveSceneReloader
        {
            public void ReloadActiveScene() { }
        }

        private sealed class DestroyRootReloader : IActiveSceneReloader
        {
            private readonly GameObject _root;

            public DestroyRootReloader(GameObject root)
            {
                _root = root;
            }

            public void ReloadActiveScene()
            {
                UnityEngine.Object.Destroy(_root);
            }
        }
    }
}
