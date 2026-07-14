using System;
using System.Collections;
using System.Collections.Generic;
using CorpseMechanism.Death;
using CorpseMechanism.Level;
using CorpseMechanism.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CorpseMechanism.Tests.PlayMode
{
    public sealed class DeathRespawnPlayModeTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject createdObject in _createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.Destroy(createdObject);
                }
            }

            _createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator HazardTrigger_AcceptsOneDeathAndSpendsOneLife()
        {
            DeathRig rig = CreateRig(3, 0.2f);
            int deathEvents = 0;
            rig.Life.DeathAccepted += _ => deathEvents++;
            CreateHazard(rig.Body.position);

            yield return WaitUntil(() => rig.Life.State == PlayerLifeState.Dead);

            Assert.That(deathEvents, Is.EqualTo(1));
            Assert.That(rig.Session.RemainingLives, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator SustainedHazardOverlap_DoesNotSpendAgainOrDuplicateRespawn()
        {
            DeathRig rig = CreateRig(3, 0.2f);
            int respawnEvents = 0;
            rig.Respawn.PlayerRespawned += () => respawnEvents++;
            CreateHazard(rig.Body.position);

            yield return WaitUntil(() => rig.Life.State == PlayerLifeState.Dead);
            yield return new WaitForSeconds(0.08f);

            Assert.That(rig.Session.RemainingLives, Is.EqualTo(2));
            Assert.That(rig.Respawn.IsRespawnPending, Is.True);
            Assert.That(respawnEvents, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Respawn_RestoresSamePlayerAtConfiguredSpawn()
        {
            DeathRig rig = CreateRig(3, 0.02f);
            PlayerLifeController originalPlayer = rig.Life;
            CreateHazard(rig.Body.position);

            yield return WaitUntil(() => rig.Life.State == PlayerLifeState.Dead);
            yield return WaitUntil(() => rig.Life.State == PlayerLifeState.Alive);

            Assert.That(rig.Respawn.Player, Is.SameAs(originalPlayer));
            Assert.That(
                Vector2.Distance(rig.Body.position, rig.Spawn.position),
                Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator Respawn_RestoresControlAndHorizontalMovement()
        {
            DeathRig rig = CreateRig(3, 0.02f);
            CreateHazard(rig.Body.position);

            yield return WaitUntil(() => rig.Life.State == PlayerLifeState.Dead);
            Assert.That(rig.Motor.ControlEnabled, Is.False);
            yield return WaitUntil(() => rig.Life.State == PlayerLifeState.Alive);

            rig.Input.SetHorizontal(1f);
            yield return new WaitForFixedUpdate();

            Assert.That(rig.Motor.ControlEnabled, Is.True);
            Assert.That(rig.Body.velocity.x, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator Respawn_ClearsLinearAndAngularVelocity()
        {
            DeathRig rig = CreateRig(3, 0.02f);
            rig.Body.velocity = new Vector2(4f, 6f);
            rig.Body.angularVelocity = 20f;

            Assert.That(rig.Life.TryKill(new FakeDamageSource()), Is.True);
            yield return WaitUntil(() => rig.Life.State == PlayerLifeState.Alive);

            Assert.That(rig.Body.velocity.sqrMagnitude, Is.LessThan(0.0001f));
            Assert.That(rig.Body.angularVelocity, Is.EqualTo(0f).Within(0.001f));
            Assert.That(
                Vector2.Distance(rig.Body.position, rig.Spawn.position),
                Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator LastLife_EntersFailedAndNeverRespawns()
        {
            DeathRig rig = CreateRig(1, 0.01f);
            CreateHazard(rig.Body.position);

            yield return WaitUntil(() => rig.Session.State == LevelSessionState.Failed);
            yield return new WaitForSeconds(0.05f);

            Assert.That(rig.Session.RemainingLives, Is.Zero);
            Assert.That(rig.Life.State, Is.EqualTo(PlayerLifeState.Dead));
            Assert.That(rig.Motor.ControlEnabled, Is.False);
            Assert.That(rig.Respawn.IsRespawnPending, Is.False);
        }

        [UnityTest]
        public IEnumerator RepeatedTryKill_IsIdempotent()
        {
            DeathRig rig = CreateRig(3, 0.2f);
            FakeDamageSource source = new FakeDamageSource();
            int deathEvents = 0;
            rig.Life.DeathAccepted += _ => deathEvents++;

            bool firstAccepted = rig.Life.TryKill(source);
            bool secondAccepted = rig.Life.TryKill(source);
            yield return null;

            Assert.That(firstAccepted, Is.True);
            Assert.That(secondAccepted, Is.False);
            Assert.That(deathEvents, Is.EqualTo(1));
            Assert.That(rig.Session.RemainingLives, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Restart_RequestsActiveSceneReloadThroughPublicApi()
        {
            FakeSceneReloader reloader = new FakeSceneReloader();
            DeathRig rig = CreateRig(3, 0.02f, reloader);

            rig.Session.RestartLevel();
            yield return null;

            Assert.That(reloader.ReloadCount, Is.EqualTo(1));
        }

        private DeathRig CreateRig(
            int initialLives,
            float respawnDelay,
            IActiveSceneReloader sceneReloader = null)
        {
            GameObject spawnObject = new GameObject("TestSpawn");
            spawnObject.transform.position = new Vector3(-4f, 0f, 0f);
            _createdObjects.Add(spawnObject);

            GameObject player = new GameObject("DeathTestPlayer");
            player.SetActive(false);
            player.transform.position = Vector3.zero;
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.8f, 1.6f);
            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            GroundProbe2D probe = player.AddComponent<GroundProbe2D>();
            TestPlayerInput input = new TestPlayerInput();
            PlayerMotor2D motor = player.AddComponent<PlayerMotor2D>();
            PlayerLifeController life = player.AddComponent<PlayerLifeController>();
            probe.Configure(collider, 1 << 0, 0.08f);
            motor.Configure(body, probe, input, 5f, 7f);
            life.Configure(motor, body, collider, probe, renderer);
            player.SetActive(true);
            _createdObjects.Add(player);

            GameObject systems = new GameObject("DeathTestSystems");
            RespawnController respawn = systems.AddComponent<RespawnController>();
            LevelSession session = systems.AddComponent<LevelSession>();
            respawn.Configure(life, spawnObject.transform, respawnDelay);
            session.Configure(life, respawn, initialLives, sceneReloader);
            _createdObjects.Add(systems);

            Physics2D.SyncTransforms();
            return new DeathRig(
                body,
                life,
                motor,
                input,
                respawn,
                session,
                spawnObject.transform);
        }

        private void CreateHazard(Vector2 position)
        {
            GameObject hazardObject = new GameObject("TestNormalHazard");
            hazardObject.transform.position = position;
            BoxCollider2D collider = hazardObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(2f, 2f);
            collider.isTrigger = true;
            hazardObject.AddComponent<NormalHazard>();
            _createdObjects.Add(hazardObject);
            Physics2D.SyncTransforms();
        }

        private static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds = 1f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, "Condition was not reached before the test timeout.");
        }

        private readonly struct DeathRig
        {
            public DeathRig(
                Rigidbody2D body,
                PlayerLifeController life,
                PlayerMotor2D motor,
                TestPlayerInput input,
                RespawnController respawn,
                LevelSession session,
                Transform spawn)
            {
                Body = body;
                Life = life;
                Motor = motor;
                Input = input;
                Respawn = respawn;
                Session = session;
                Spawn = spawn;
            }

            public Rigidbody2D Body { get; }
            public PlayerLifeController Life { get; }
            public PlayerMotor2D Motor { get; }
            public TestPlayerInput Input { get; }
            public RespawnController Respawn { get; }
            public LevelSession Session { get; }
            public Transform Spawn { get; }
        }

        private sealed class FakeDamageSource : IDamageSource
        {
            public DeathType DeathType => DeathType.Normal;
        }

        private sealed class FakeSceneReloader : IActiveSceneReloader
        {
            public int ReloadCount { get; private set; }

            public void ReloadActiveScene()
            {
                ReloadCount++;
            }
        }

        private sealed class TestPlayerInput : IPlayerInputSource
        {
            private readonly InputButtonLatch _jump = new InputButtonLatch();
            private bool _enabled = true;

            public float Horizontal { get; private set; }

            public bool ConsumeJumpPressed()
            {
                return _enabled && _jump.Consume();
            }

            public void SetInputEnabled(bool enabled)
            {
                _enabled = enabled;
                Horizontal = 0f;
                if (!enabled)
                {
                    _jump.Clear();
                }
            }

            public void SetHorizontal(float value)
            {
                Horizontal = _enabled ? Mathf.Clamp(value, -1f, 1f) : 0f;
            }
        }
    }
}
