using System.Collections;
using System.Collections.Generic;
using CorpseMechanism.Corpse;
using CorpseMechanism.Death;
using CorpseMechanism.Interaction;
using CorpseMechanism.Level;
using CorpseMechanism.Player;
using CorpseMechanism.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CorpseMechanism.Tests.PlayMode
{
    public sealed class LevelCompletionPlayModeTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject gameObject in _objects)
            {
                if (gameObject != null)
                {
                    Object.Destroy(gameObject);
                }
            }

            _objects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AliveConfiguredPlayer_EnteringExitCompletesOnce()
        {
            CompletionRig rig = CreateRig();
            int events = 0;
            rig.Session.LevelCompleted += () => events++;

            rig.PlayerObject.transform.position = rig.Exit.transform.position;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(rig.Session.State, Is.EqualTo(LevelSessionState.Completed));
            Assert.That(events, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CorpseCollider_CannotCompleteLevel()
        {
            CompletionRig rig = CreateRig();
            GameObject corpseObject = new GameObject("ExitTestCorpse");
            corpseObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            BoxCollider2D collider = corpseObject.AddComponent<BoxCollider2D>();
            corpseObject.AddComponent<CorpseController>();
            corpseObject.AddComponent<WeightProvider>();
            _objects.Add(corpseObject);

            bool completed = rig.Exit.TryComplete(collider);
            yield return null;

            Assert.That(completed, Is.False);
            Assert.That(rig.Session.State, Is.EqualTo(LevelSessionState.Playing));
        }

        [UnityTest]
        public IEnumerator UnconfiguredWeightOrPlainCollider_CannotCompleteLevel()
        {
            CompletionRig rig = CreateRig();
            GameObject other = new GameObject("ExitTestOtherWeight");
            BoxCollider2D collider = other.AddComponent<BoxCollider2D>();
            other.AddComponent<WeightProvider>();
            _objects.Add(other);

            bool completed = rig.Exit.TryComplete(collider);
            yield return null;

            Assert.That(completed, Is.False);
            Assert.That(rig.Session.State, Is.EqualTo(LevelSessionState.Playing));
        }

        [UnityTest]
        public IEnumerator DeadConfiguredPlayer_CannotCompleteLevel()
        {
            CompletionRig rig = CreateRig();
            rig.Life.TryKill(new FakeDamageSource());

            bool completed = rig.Exit.TryComplete(rig.PlayerCollider);
            yield return null;

            Assert.That(completed, Is.False);
            Assert.That(rig.Session.State, Is.EqualTo(LevelSessionState.Playing));
        }

        [UnityTest]
        public IEnumerator FailedSession_CannotCompleteLevel()
        {
            CompletionRig rig = CreateRig(1);
            rig.Life.TryKill(new FakeDamageSource());

            bool completed = rig.Exit.TryComplete(rig.PlayerCollider);
            yield return null;

            Assert.That(completed, Is.False);
            Assert.That(rig.Session.State, Is.EqualTo(LevelSessionState.Failed));
        }

        [UnityTest]
        public IEnumerator CompletedSession_IgnoresLaterDeathForLivesAndRespawn()
        {
            CompletionRig rig = CreateRig();
            Assert.That(rig.Exit.TryComplete(rig.PlayerCollider), Is.True);
            int livesBefore = rig.Session.RemainingLives;

            Assert.That(rig.Life.TryKill(new FakeDamageSource()), Is.True);
            yield return null;

            Assert.That(rig.Session.State, Is.EqualTo(LevelSessionState.Completed));
            Assert.That(rig.Session.RemainingLives, Is.EqualTo(livesBefore));
            Assert.That(rig.Respawn.RequestCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator RepeatedExitCallbacks_DoNotRepeatCompletionEvent()
        {
            CompletionRig rig = CreateRig();
            int events = 0;
            rig.Session.LevelCompleted += () => events++;

            bool first = rig.Exit.TryComplete(rig.PlayerCollider);
            bool second = rig.Exit.TryComplete(rig.PlayerCollider);
            yield return null;

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(events, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator StatusView_SynchronizesLivesFailedAndCompleted()
        {
            CompletionRig playing = CreateRig();
            LevelStatusView playingView = CreateStatusView(playing.Session);
            Assert.That(playingView.DisplayText, Does.Contain("Lives: 3"));
            Assert.That(playingView.DisplayText, Does.Contain("State: Playing"));

            playing.Life.TryKill(new FakeDamageSource());
            Assert.That(playingView.DisplayText, Does.Contain("Lives: 2"));

            CompletionRig failed = CreateRig(1);
            LevelStatusView failedView = CreateStatusView(failed.Session);
            failed.Life.TryKill(new FakeDamageSource());
            Assert.That(failedView.DisplayText, Does.Contain("State: Failed"));
            Assert.That(failedView.DisplayText, Does.Contain("Press R to Restart"));

            CompletionRig completed = CreateRig();
            LevelStatusView completedView = CreateStatusView(completed.Session);
            completed.Session.TryComplete();
            yield return null;

            Assert.That(completedView.DisplayText, Does.Contain("State: Completed"));
            Assert.That(completedView.DisplayText, Does.Contain("Press R to Restart"));
        }

        [UnityTest]
        public IEnumerator RestartInput_PublicRequestDelegatesOnce()
        {
            FakeReloader reloader = new FakeReloader();
            CompletionRig rig = CreateRig(3, reloader);
            GameObject inputObject = new GameObject("ExitTestRestartInput");
            LevelRestartInput restartInput = inputObject.AddComponent<LevelRestartInput>();
            restartInput.Configure(rig.Session);
            _objects.Add(inputObject);

            bool requested = restartInput.RequestRestart();
            yield return null;

            Assert.That(requested, Is.True);
            Assert.That(reloader.ReloadCount, Is.EqualTo(1));
        }

        private CompletionRig CreateRig(
            int initialLives = 3,
            IActiveSceneReloader reloader = null)
        {
            GameObject player = new GameObject("ExitTestPlayer");
            player.SetActive(false);
            player.transform.position = new Vector2(5f, 0f);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
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

            FakeRespawnScheduler respawn = new FakeRespawnScheduler();
            GameObject sessionObject = new GameObject("ExitTestSession");
            LevelSession session = sessionObject.AddComponent<LevelSession>();
            session.Configure(life, respawn, initialLives, reloader ?? new FakeReloader());
            _objects.Add(sessionObject);

            GameObject exitObject = new GameObject("ExitTestTrigger");
            BoxCollider2D trigger = exitObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(2f, 2f);
            LevelExit levelExit = exitObject.AddComponent<LevelExit>();
            levelExit.Configure(trigger, session, life);
            _objects.Add(exitObject);
            Physics2D.SyncTransforms();

            return new CompletionRig(
                player,
                collider,
                life,
                session,
                respawn,
                levelExit);
        }

        private LevelStatusView CreateStatusView(LevelSession session)
        {
            GameObject viewObject = new GameObject("ExitTestStatusView");
            LevelStatusView view = viewObject.AddComponent<LevelStatusView>();
            view.Configure(session);
            _objects.Add(viewObject);
            return view;
        }

        private readonly struct CompletionRig
        {
            public CompletionRig(
                GameObject playerObject,
                Collider2D playerCollider,
                PlayerLifeController life,
                LevelSession session,
                FakeRespawnScheduler respawn,
                LevelExit levelExit)
            {
                PlayerObject = playerObject;
                PlayerCollider = playerCollider;
                Life = life;
                Session = session;
                Respawn = respawn;
                Exit = levelExit;
            }

            public GameObject PlayerObject { get; }
            public Collider2D PlayerCollider { get; }
            public PlayerLifeController Life { get; }
            public LevelSession Session { get; }
            public FakeRespawnScheduler Respawn { get; }
            public LevelExit Exit { get; }
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
