using System;
using System.Collections;
using CorpseMechanism.Corpse;
using CorpseMechanism.Death;
using CorpseMechanism.Interaction;
using CorpseMechanism.Level;
using CorpseMechanism.Player;
using CorpseMechanism.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CorpseMechanism.Tests.PlayMode
{
    public sealed class FirstLevelPlayModeTests
    {
        private const string LevelSceneName = "Level_001";

        [UnityTest]
        public IEnumerator Level001_MainStateChainCompletesThroughRealSceneReferences()
        {
            yield return LoadLevel();
            SceneRig rig = ReadSceneRig();

            rig.Body.position = new Vector2(
                rig.Plate.DetectionTrigger.bounds.center.x,
                -1.3f);
            Physics2D.SyncTransforms();
            Assert.That(rig.Life.TryKill(rig.Hazard), Is.True);

            yield return WaitUntil(() => rig.Session.SpawnedCorpseCount == 1);
            yield return WaitUntil(() => rig.Life.State == PlayerLifeState.Alive);
            yield return WaitUntil(() => rig.Plate.IsPressed, 3f);

            Assert.That(rig.Session.SpawnedCorpses[0], Is.Not.Null);
            Assert.That(rig.Door.IsOpen, Is.True);
            Assert.That(rig.Door.BlockingCollider.enabled, Is.False);

            rig.Body.position = rig.Exit.Trigger.bounds.center;
            Physics2D.SyncTransforms();
            yield return WaitUntil(() => rig.Session.State == LevelSessionState.Completed);

            Assert.That(rig.Session.State, Is.EqualTo(LevelSessionState.Completed));
        }

        [UnityTest]
        public IEnumerator Level001_StructureBlocksBypassAndUsesExplicitReferences()
        {
            yield return LoadLevel();
            SceneRig rig = ReadSceneRig();

            Assert.That(rig.Door.PressurePlate, Is.SameAs(rig.Plate));
            Assert.That(rig.Exit.Player, Is.SameAs(rig.Life));
            Assert.That(rig.Exit.LevelSession, Is.SameAs(rig.Session));
            Assert.That(rig.Respawn.Player, Is.SameAs(rig.Life));
            Assert.That(rig.Factory.Player, Is.SameAs(rig.Life));
            Assert.That(rig.Factory.LevelSession, Is.SameAs(rig.Session));
            Assert.That(rig.Factory.CorpseParent.childCount, Is.Zero);
            Assert.That(rig.Door.IsOpen, Is.False);
            Assert.That(rig.Door.BlockingCollider.enabled, Is.True);
            Assert.That(rig.Respawn.SpawnPoint.position.x, Is.LessThan(rig.Door.transform.position.x));
            Assert.That(rig.Exit.transform.position.x, Is.GreaterThan(rig.Door.transform.position.x));

            Bounds doorBounds = rig.Door.BlockingCollider.bounds;
            Collider2D[] colliders = UnityEngine.Object.FindObjectsOfType<Collider2D>();
            bool hasOverheadBlocker = Array.Exists(colliders, collider =>
                collider != rig.Door.BlockingCollider &&
                !collider.isTrigger &&
                collider.bounds.min.x < doorBounds.max.x &&
                collider.bounds.max.x > doorBounds.min.x &&
                collider.bounds.min.y <= doorBounds.max.y + 0.01f &&
                collider.bounds.max.y > doorBounds.max.y);
            Assert.That(hasOverheadBlocker, Is.True);

            bool spawnInsideHazard = Array.Exists(
                rig.Hazard.GetComponentsInChildren<Collider2D>(),
                collider => collider.bounds.Contains(rig.Respawn.SpawnPoint.position));
            Assert.That(spawnInsideHazard, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Restart_ReloadsFreshPlayingLevelAndExitWorksAgain()
        {
            yield return LoadLevel();
            SceneRig oldRig = ReadSceneRig();
            oldRig.Body.position = new Vector2(
                oldRig.Plate.DetectionTrigger.bounds.center.x,
                -1.3f);
            Physics2D.SyncTransforms();
            oldRig.Life.TryKill(oldRig.Hazard);
            yield return WaitUntil(() => oldRig.Session.SpawnedCorpseCount == 1);
            yield return WaitUntil(() => oldRig.Life.State == PlayerLifeState.Alive);
            yield return WaitUntil(() => oldRig.Plate.IsPressed, 3f);
            Assert.That(oldRig.Session.TryComplete(), Is.True);

            LevelSession oldSession = oldRig.Session;
            oldRig.Session.RestartLevel();
            yield return WaitUntil(() => oldSession == null, 3f);
            SceneRig fresh = ReadSceneRig();

            Assert.That(fresh.Session.State, Is.EqualTo(LevelSessionState.Playing));
            Assert.That(fresh.Session.RemainingLives, Is.EqualTo(3));
            Assert.That(fresh.Session.SpawnedCorpseCount, Is.Zero);
            Assert.That(fresh.Factory.CorpseParent.childCount, Is.Zero);
            Assert.That(fresh.Plate.IsPressed, Is.False);
            Assert.That(fresh.Door.IsOpen, Is.False);
            Assert.That(fresh.Door.BlockingCollider.enabled, Is.True);
            Assert.That(fresh.Life.State, Is.EqualTo(PlayerLifeState.Alive));
            Assert.That(fresh.Exit.TryComplete(fresh.Life.GetComponent<Collider2D>()), Is.True);
            Assert.That(fresh.Session.State, Is.EqualTo(LevelSessionState.Completed));
        }

        private static IEnumerator LoadLevel()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(LevelSceneName, LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null, "Level_001 must be enabled in Build Settings.");
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(LevelSceneName));
        }

        private static SceneRig ReadSceneRig()
        {
            PlayerLifeController life = FindUnique<PlayerLifeController>();
            LevelSession session = FindUnique<LevelSession>();
            RespawnController respawn = FindUnique<RespawnController>();
            CorpseFactory factory = FindUnique<CorpseFactory>();
            NormalHazard hazard = FindUnique<NormalHazard>();
            PressurePlate plate = FindUnique<PressurePlate>();
            DoorController door = FindUnique<DoorController>();
            LevelExit levelExit = FindUnique<LevelExit>();
            FindUnique<LevelStatusView>();
            FindUnique<LevelRestartInput>();
            return new SceneRig(
                life.GetComponent<Rigidbody2D>(),
                life,
                session,
                respawn,
                factory,
                hazard,
                plate,
                door,
                levelExit);
        }

        private static T FindUnique<T>() where T : UnityEngine.Object
        {
            T[] objects = UnityEngine.Object.FindObjectsOfType<T>();
            Assert.That(objects.Length, Is.EqualTo(1), $"Expected exactly one active {typeof(T).Name}.");
            return objects[0];
        }

        private static IEnumerator WaitUntil(Func<bool> condition, float timeout = 1.5f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, "Condition was not reached before timeout.");
        }

        private readonly struct SceneRig
        {
            public SceneRig(
                Rigidbody2D body,
                PlayerLifeController life,
                LevelSession session,
                RespawnController respawn,
                CorpseFactory factory,
                NormalHazard hazard,
                PressurePlate plate,
                DoorController door,
                LevelExit levelExit)
            {
                Body = body;
                Life = life;
                Session = session;
                Respawn = respawn;
                Factory = factory;
                Hazard = hazard;
                Plate = plate;
                Door = door;
                Exit = levelExit;
            }

            public Rigidbody2D Body { get; }
            public PlayerLifeController Life { get; }
            public LevelSession Session { get; }
            public RespawnController Respawn { get; }
            public CorpseFactory Factory { get; }
            public NormalHazard Hazard { get; }
            public PressurePlate Plate { get; }
            public DoorController Door { get; }
            public LevelExit Exit { get; }
        }
    }
}
