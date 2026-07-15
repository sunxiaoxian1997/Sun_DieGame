using System.Collections.Generic;
using CorpseMechanism.Death;
using CorpseMechanism.Level;
using CorpseMechanism.Player;
using NUnit.Framework;
using UnityEngine;

namespace CorpseMechanism.Tests.EditMode
{
    public sealed class LevelCompletionRuleTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly FakeDamageSource _damageSource = new FakeDamageSource();

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
        public void NewSession_StartsPlaying()
        {
            SessionRig rig = CreateSession();

            Assert.That(rig.Session.State, Is.EqualTo(LevelSessionState.Playing));
        }

        [Test]
        public void PlayingSession_FirstTryCompleteSucceeds()
        {
            SessionRig rig = CreateSession();

            Assert.That(rig.Session.TryComplete(), Is.True);
        }

        [Test]
        public void SuccessfulCompletion_EntersCompletedState()
        {
            SessionRig rig = CreateSession();

            rig.Session.TryComplete();

            Assert.That(rig.Session.State, Is.EqualTo(LevelSessionState.Completed));
        }

        [Test]
        public void RepeatedCompletion_IsRejectedAndEventFiresOnce()
        {
            SessionRig rig = CreateSession();
            int completedEvents = 0;
            rig.Session.LevelCompleted += () => completedEvents++;

            bool first = rig.Session.TryComplete();
            bool second = rig.Session.TryComplete();

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(completedEvents, Is.EqualTo(1));
        }

        [Test]
        public void FailedSession_CannotComplete()
        {
            SessionRig rig = CreateSession(1);
            rig.Player.TryKill(_damageSource);

            Assert.That(rig.Session.State, Is.EqualTo(LevelSessionState.Failed));
            Assert.That(rig.Session.TryComplete(), Is.False);
            Assert.That(rig.Session.State, Is.EqualTo(LevelSessionState.Failed));
        }

        [Test]
        public void CompletedSession_CannotBecomeFailedOrSpendLife()
        {
            SessionRig rig = CreateSession(3);
            rig.Session.TryComplete();
            int livesBefore = rig.Session.RemainingLives;

            Assert.That(rig.Player.TryKill(_damageSource), Is.True);

            Assert.That(rig.Session.State, Is.EqualTo(LevelSessionState.Completed));
            Assert.That(rig.Session.RemainingLives, Is.EqualTo(livesBefore));
            Assert.That(rig.Respawn.RequestCount, Is.Zero);
        }

        [Test]
        public void FailedAndCompletedStatesRemainMutuallyExclusive()
        {
            SessionRig failed = CreateSession(1);
            failed.Player.TryKill(_damageSource);
            SessionRig completed = CreateSession();
            completed.Session.TryComplete();

            Assert.That(failed.Session.State, Is.Not.EqualTo(LevelSessionState.Completed));
            Assert.That(completed.Session.State, Is.Not.EqualTo(LevelSessionState.Failed));
        }

        [Test]
        public void Restart_IsAvailableWhilePlaying()
        {
            SessionRig rig = CreateSession();

            rig.Session.RestartLevel();

            Assert.That(rig.Reloader.ReloadCount, Is.EqualTo(1));
        }

        [Test]
        public void Restart_IsAvailableWhileFailed()
        {
            SessionRig rig = CreateSession(1);
            rig.Player.TryKill(_damageSource);

            rig.Session.RestartLevel();

            Assert.That(rig.Reloader.ReloadCount, Is.EqualTo(1));
        }

        [Test]
        public void Restart_IsAvailableWhileCompleted()
        {
            SessionRig rig = CreateSession();
            rig.Session.TryComplete();

            rig.Session.RestartLevel();

            Assert.That(rig.Reloader.ReloadCount, Is.EqualTo(1));
        }

        [Test]
        public void ReplacementSession_StartsWithThreeLivesNoCorpsesAndPlaying()
        {
            SessionRig first = CreateSession();
            first.Session.TryComplete();
            SessionRig replacement = CreateSession();

            Assert.That(replacement.Session.InitialLives, Is.EqualTo(3));
            Assert.That(replacement.Session.RemainingLives, Is.EqualTo(3));
            Assert.That(replacement.Session.SpawnedCorpseCount, Is.Zero);
            Assert.That(replacement.Session.State, Is.EqualTo(LevelSessionState.Playing));
        }

        private SessionRig CreateSession(int initialLives = 3)
        {
            GameObject playerObject = new GameObject("CompletionRulePlayer");
            playerObject.SetActive(false);
            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
            CapsuleCollider2D collider = playerObject.AddComponent<CapsuleCollider2D>();
            SpriteRenderer renderer = playerObject.AddComponent<SpriteRenderer>();
            GroundProbe2D probe = playerObject.AddComponent<GroundProbe2D>();
            PlayerMotor2D motor = playerObject.AddComponent<PlayerMotor2D>();
            PlayerLifeController life = playerObject.AddComponent<PlayerLifeController>();
            probe.Configure(collider, 1 << 0, 0.08f);
            motor.Configure(body, probe, new FakeInput());
            life.Configure(motor, body, collider, probe, renderer);
            playerObject.SetActive(true);
            _objects.Add(playerObject);

            GameObject sessionObject = new GameObject("CompletionRuleSession");
            LevelSession session = sessionObject.AddComponent<LevelSession>();
            FakeRespawnScheduler respawn = new FakeRespawnScheduler();
            FakeReloader reloader = new FakeReloader();
            session.Configure(life, respawn, initialLives, reloader);
            _objects.Add(sessionObject);
            return new SessionRig(life, session, respawn, reloader);
        }

        private readonly struct SessionRig
        {
            public SessionRig(
                PlayerLifeController player,
                LevelSession session,
                FakeRespawnScheduler respawn,
                FakeReloader reloader)
            {
                Player = player;
                Session = session;
                Respawn = respawn;
                Reloader = reloader;
            }

            public PlayerLifeController Player { get; }
            public LevelSession Session { get; }
            public FakeRespawnScheduler Respawn { get; }
            public FakeReloader Reloader { get; }
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
            public int RequestCount { get; private set; }

            public bool RequestRespawn(DeathContext deathContext)
            {
                RequestCount++;
                return true;
            }
        }

        private sealed class FakeReloader : IActiveSceneReloader
        {
            public int ReloadCount { get; private set; }

            public void ReloadActiveScene()
            {
                ReloadCount++;
            }
        }
    }
}
