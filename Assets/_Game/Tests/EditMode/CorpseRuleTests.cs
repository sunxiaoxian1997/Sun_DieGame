using System.Collections.Generic;
using CorpseMechanism.Corpse;
using CorpseMechanism.Death;
using CorpseMechanism.Level;
using CorpseMechanism.Player;
using NUnit.Framework;
using UnityEngine;

namespace CorpseMechanism.Tests.EditMode
{
    public sealed class CorpseRuleTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in _objects)
            {
                if (gameObject != null)
                {
                    Object.DestroyImmediate(gameObject);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void CorpseController_InitializesFromNormalDeathContext()
        {
            CorpseController corpse = CreateCorpse();

            bool initialized = corpse.Initialize(
                new DeathContext(DeathType.Normal, new Vector2(2f, -1f)));

            Assert.That(initialized, Is.True);
            Assert.That(corpse.IsInitialized, Is.True);
        }

        [Test]
        public void InitializedCorpse_ReportsNormalDeathType()
        {
            CorpseController corpse = CreateCorpse();
            corpse.Initialize(new DeathContext(DeathType.Normal, Vector2.zero));

            Assert.That(corpse.DeathType, Is.EqualTo(DeathType.Normal));
        }

        [Test]
        public void InitializedCorpse_PreservesDeathPosition()
        {
            CorpseController corpse = CreateCorpse();
            Vector2 deathPosition = new Vector2(3.25f, -0.75f);
            corpse.Initialize(new DeathContext(DeathType.Normal, deathPosition));

            Assert.That(corpse.DeathPosition, Is.EqualTo(deathPosition));
        }

        [Test]
        public void RepeatedInitialization_IsRejectedWithoutOverwritingOriginalData()
        {
            CorpseController corpse = CreateCorpse();
            Vector2 originalPosition = new Vector2(1f, 2f);
            corpse.Initialize(new DeathContext(DeathType.Normal, originalPosition));

            bool secondInitialization = corpse.Initialize(
                new DeathContext(DeathType.Normal, new Vector2(9f, 9f)));

            Assert.That(secondInitialization, Is.False);
            Assert.That(corpse.DeathPosition, Is.EqualTo(originalPosition));
        }

        [Test]
        public void LevelSession_RegistersOneCorpse()
        {
            LevelSession session = CreateSession();

            Assert.That(session.RegisterCorpse(CreateCorpse()), Is.True);
            Assert.That(session.SpawnedCorpseCount, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateCorpseRegistration_DoesNotIncreaseCount()
        {
            LevelSession session = CreateSession();
            CorpseController corpse = CreateCorpse();

            Assert.That(session.RegisterCorpse(corpse), Is.True);
            Assert.That(session.RegisterCorpse(corpse), Is.False);
            Assert.That(session.SpawnedCorpseCount, Is.EqualTo(1));
        }

        [Test]
        public void NullCorpseRegistration_IsRejected()
        {
            LevelSession session = CreateSession();

            Assert.That(session.RegisterCorpse(null), Is.False);
            Assert.That(session.SpawnedCorpseCount, Is.Zero);
        }

        [Test]
        public void CorpseRegistration_DoesNotChangeRemainingLives()
        {
            LevelSession session = CreateSession();
            int livesBefore = session.RemainingLives;

            session.RegisterCorpse(CreateCorpse());

            Assert.That(session.RemainingLives, Is.EqualTo(livesBefore));
        }

        [Test]
        public void FailedSession_AcceptsCorpseFromFinalDeath()
        {
            PlayerLifeController life = CreatePlayerLife();
            LevelSession session = CreateSession(life, 1);
            life.TryKill(new FakeDamageSource());

            bool registered = session.RegisterCorpse(CreateCorpse());

            Assert.That(session.State, Is.EqualTo(LevelSessionState.Failed));
            Assert.That(registered, Is.True);
            Assert.That(session.SpawnedCorpseCount, Is.EqualTo(1));
        }

        [Test]
        public void NewLevelSession_HasNoRegisteredCorpses()
        {
            LevelSession first = CreateSession();
            first.RegisterCorpse(CreateCorpse());
            LevelSession replacement = CreateSession();

            Assert.That(first.SpawnedCorpseCount, Is.EqualTo(1));
            Assert.That(replacement.SpawnedCorpseCount, Is.Zero);
        }

        private CorpseController CreateCorpse()
        {
            GameObject corpseObject = new GameObject("RuleCorpse");
            corpseObject.AddComponent<Rigidbody2D>();
            corpseObject.AddComponent<BoxCollider2D>();
            CorpseController corpse = corpseObject.AddComponent<CorpseController>();
            _objects.Add(corpseObject);
            return corpse;
        }

        private LevelSession CreateSession(
            PlayerLifeController player = null,
            int initialLives = 3)
        {
            GameObject sessionObject = new GameObject("RuleSession");
            LevelSession session = sessionObject.AddComponent<LevelSession>();
            session.Configure(player, new FakeRespawnScheduler(), initialLives, new FakeReloader());
            _objects.Add(sessionObject);
            return session;
        }

        private PlayerLifeController CreatePlayerLife()
        {
            GameObject player = new GameObject("RulePlayer");
            player.SetActive(false);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            GroundProbe2D probe = player.AddComponent<GroundProbe2D>();
            PlayerMotor2D motor = player.AddComponent<PlayerMotor2D>();
            PlayerLifeController life = player.AddComponent<PlayerLifeController>();
            probe.Configure(collider, 1 << 0, 0.08f);
            motor.Configure(body, probe, new FakeInput());
            life.Configure(motor, body, collider, probe, renderer);
            player.SetActive(true);
            _objects.Add(player);
            return life;
        }

        private sealed class FakeDamageSource : IDamageSource
        {
            public DeathType DeathType => DeathType.Normal;
        }

        private sealed class FakeRespawnScheduler : IRespawnScheduler
        {
            public bool RequestRespawn(DeathContext deathContext)
            {
                return true;
            }
        }

        private sealed class FakeReloader : IActiveSceneReloader
        {
            public void ReloadActiveScene()
            {
            }
        }

        private sealed class FakeInput : IPlayerInputSource
        {
            public float Horizontal => 0f;
            public bool ConsumeJumpPressed() => false;
            public void SetInputEnabled(bool enabled) { }
        }
    }
}
