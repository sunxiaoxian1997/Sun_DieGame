using CorpseMechanism.Death;
using CorpseMechanism.Level;
using CorpseMechanism.Player;
using NUnit.Framework;
using UnityEngine;

namespace CorpseMechanism.Tests.EditMode
{
    public sealed class DeathAndLivesRuleTests
    {
        private GameObject _playerObject;
        private GameObject _sessionObject;
        private PlayerLifeController _playerLife;
        private LevelSession _session;
        private FakeRespawnScheduler _respawn;
        private FakeSceneReloader _sceneReloader;
        private readonly FakeDamageSource _damageSource = new FakeDamageSource();

        [SetUp]
        public void SetUp()
        {
            _playerObject = new GameObject("RuleTestPlayer");
            _playerObject.SetActive(false);
            Rigidbody2D body = _playerObject.AddComponent<Rigidbody2D>();
            CapsuleCollider2D collider = _playerObject.AddComponent<CapsuleCollider2D>();
            SpriteRenderer renderer = _playerObject.AddComponent<SpriteRenderer>();
            GroundProbe2D probe = _playerObject.AddComponent<GroundProbe2D>();
            PlayerMotor2D motor = _playerObject.AddComponent<PlayerMotor2D>();
            _playerLife = _playerObject.AddComponent<PlayerLifeController>();

            FakePlayerInput input = new FakePlayerInput();
            probe.Configure(collider, 1 << 0, 0.08f);
            motor.Configure(body, probe, input);
            _playerLife.Configure(motor, body, collider, probe, renderer);
            _playerObject.SetActive(true);

            _sessionObject = new GameObject("RuleTestSession");
            _session = _sessionObject.AddComponent<LevelSession>();
            _respawn = new FakeRespawnScheduler();
            _sceneReloader = new FakeSceneReloader();
            _session.Configure(_playerLife, _respawn, 3, _sceneReloader);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sessionObject);
            Object.DestroyImmediate(_playerObject);
        }

        [Test]
        public void DeathContext_PreservesNormalTypeAndPosition()
        {
            DeathContext context = new DeathContext(DeathType.Normal, new Vector2(2.5f, -1.25f));

            Assert.That(context.DeathType, Is.EqualTo(DeathType.Normal));
            Assert.That(context.Position, Is.EqualTo(new Vector2(2.5f, -1.25f)));
        }

        [Test]
        public void Session_StartsWithThreeLives()
        {
            Assert.That(_session.InitialLives, Is.EqualTo(3));
            Assert.That(_session.RemainingLives, Is.EqualTo(3));
            Assert.That(_session.State, Is.EqualTo(LevelSessionState.Playing));
        }

        [Test]
        public void AcceptedDeath_SpendsOneLifeAndRequestsOneRespawn()
        {
            Assert.That(_playerLife.TryKill(_damageSource), Is.True);

            Assert.That(_session.RemainingLives, Is.EqualTo(2));
            Assert.That(_respawn.RequestCount, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedDeathWhileDead_IsRejectedWithoutSpendingAnotherLife()
        {
            int acceptedEvents = 0;
            _playerLife.DeathAccepted += _ => acceptedEvents++;

            Assert.That(_playerLife.TryKill(_damageSource), Is.True);
            Assert.That(_playerLife.TryKill(_damageSource), Is.False);

            Assert.That(acceptedEvents, Is.EqualTo(1));
            Assert.That(_session.RemainingLives, Is.EqualTo(2));
            Assert.That(_respawn.RequestCount, Is.EqualTo(1));
        }

        [Test]
        public void ThirdDeath_EntersFailedWithoutNegativeLivesOrRespawnRequest()
        {
            KillAndRestore();
            KillAndRestore();

            Assert.That(_playerLife.TryKill(_damageSource), Is.True);
            Assert.That(_session.RemainingLives, Is.Zero);
            Assert.That(_session.State, Is.EqualTo(LevelSessionState.Failed));
            Assert.That(_respawn.RequestCount, Is.EqualTo(2));

            Assert.That(_playerLife.TryKill(_damageSource), Is.False);
            Assert.That(_session.RemainingLives, Is.Zero);
            Assert.That(_respawn.RequestCount, Is.EqualTo(2));
        }

        [Test]
        public void FailedSession_DoesNotRequestRespawn()
        {
            _session.Configure(_playerLife, _respawn, 1, _sceneReloader);

            _playerLife.TryKill(_damageSource);

            Assert.That(_session.State, Is.EqualTo(LevelSessionState.Failed));
            Assert.That(_respawn.RequestCount, Is.Zero);
        }

        [Test]
        public void NewlyConfiguredSession_RestoresInitialLifeState()
        {
            _playerLife.TryKill(_damageSource);
            LevelSession replacement = new GameObject("ReplacementSession").AddComponent<LevelSession>();

            try
            {
                replacement.Configure(_playerLife, new FakeRespawnScheduler(), 3, _sceneReloader);
                Assert.That(replacement.RemainingLives, Is.EqualTo(3));
                Assert.That(replacement.State, Is.EqualTo(LevelSessionState.Playing));
            }
            finally
            {
                Object.DestroyImmediate(replacement.gameObject);
            }
        }

        [Test]
        public void RestartLevel_DelegatesToConfiguredSceneReloader()
        {
            _session.RestartLevel();

            Assert.That(_sceneReloader.ReloadCount, Is.EqualTo(1));
        }

        private void KillAndRestore()
        {
            Assert.That(_playerLife.TryKill(_damageSource), Is.True);
            Assert.That(_playerLife.RespawnAt(Vector2.zero), Is.True);
        }

        private sealed class FakeDamageSource : IDamageSource
        {
            public DeathType DeathType => DeathType.Normal;
        }

        private sealed class FakeRespawnScheduler : IRespawnScheduler
        {
            public int RequestCount { get; private set; }

            public bool RequestRespawn(DeathContext deathContext)
            {
                RequestCount++;
                return true;
            }
        }

        private sealed class FakeSceneReloader : IActiveSceneReloader
        {
            public int ReloadCount { get; private set; }

            public void ReloadActiveScene()
            {
                ReloadCount++;
            }
        }

        private sealed class FakePlayerInput : IPlayerInputSource
        {
            public float Horizontal => 0f;

            public bool ConsumeJumpPressed()
            {
                return false;
            }

            public void SetInputEnabled(bool enabled)
            {
            }
        }
    }
}
