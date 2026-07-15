using System;
using System.Collections.Generic;
using CorpseMechanism.Corpse;
using CorpseMechanism.Death;
using CorpseMechanism.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorpseMechanism.Level
{
    public enum LevelSessionState
    {
        Playing,
        Failed,
        Completed
    }

    [DisallowMultipleComponent]
    public sealed class LevelSession : MonoBehaviour
    {
        [SerializeField]
        [Min(1)]
        [Tooltip("Lives available when this scene starts.")]
        private int _initialLives = 3;

        [SerializeField]
        [Tooltip("Active player whose accepted deaths spend lives.")]
        private PlayerLifeController _player;

        [SerializeField]
        [Tooltip("Controller responsible for delayed respawn timing and position.")]
        private RespawnController _respawnController;

        private IRespawnScheduler _respawnScheduler;
        private IActiveSceneReloader _sceneReloader;
        private bool _subscribed;
        private readonly List<CorpseController> _spawnedCorpses =
            new List<CorpseController>();

        public event Action<int> RemainingLivesChanged;

        public event Action LevelFailed;

        public event Action LevelCompleted;

        public int InitialLives => _initialLives;

        public int RemainingLives { get; private set; }

        public LevelSessionState State { get; private set; }

        public int SpawnedCorpseCount
        {
            get
            {
                RemoveDestroyedCorpseReferences();
                return _spawnedCorpses.Count;
            }
        }

        public IReadOnlyList<CorpseController> SpawnedCorpses
        {
            get
            {
                RemoveDestroyedCorpseReferences();
                return _spawnedCorpses;
            }
        }

        private void Awake()
        {
            _initialLives = Mathf.Max(1, _initialLives);
            RemainingLives = _initialLives;
            State = LevelSessionState.Playing;
            _respawnScheduler = _respawnController;
            _sceneReloader = new ActiveSceneReloader();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            PlayerLifeController player,
            IRespawnScheduler respawnScheduler,
            int initialLives = 3,
            IActiveSceneReloader sceneReloader = null)
        {
            Unsubscribe();
            _player = player;
            _respawnController = respawnScheduler as RespawnController;
            _respawnScheduler = respawnScheduler;
            _sceneReloader = sceneReloader ?? new ActiveSceneReloader();
            _initialLives = Mathf.Max(1, initialLives);
            RemainingLives = _initialLives;
            State = LevelSessionState.Playing;
            _spawnedCorpses.Clear();
            Subscribe();
        }

        public bool RegisterCorpse(CorpseController corpse)
        {
            RemoveDestroyedCorpseReferences();
            if (corpse == null || _spawnedCorpses.Contains(corpse))
            {
                return false;
            }

            _spawnedCorpses.Add(corpse);
            return true;
        }

        public void RestartLevel()
        {
            (_sceneReloader ?? (_sceneReloader = new ActiveSceneReloader()))
                .ReloadActiveScene();
        }

        public bool TryComplete()
        {
            if (State != LevelSessionState.Playing)
            {
                return false;
            }

            State = LevelSessionState.Completed;
            Debug.Log("Level completed.", this);
            LevelCompleted?.Invoke();
            return true;
        }

        private void HandleDeathAccepted(DeathContext deathContext)
        {
            if (State != LevelSessionState.Playing || RemainingLives <= 0)
            {
                return;
            }

            RemainingLives = Mathf.Max(0, RemainingLives - 1);
            RemainingLivesChanged?.Invoke(RemainingLives);

            if (RemainingLives == 0)
            {
                State = LevelSessionState.Failed;
                Debug.Log("Level failed: no remaining lives.", this);
                LevelFailed?.Invoke();
                return;
            }

            _respawnScheduler?.RequestRespawn(deathContext);
        }

        private void Subscribe()
        {
            if (!_subscribed && _player != null)
            {
                _player.DeathAccepted += HandleDeathAccepted;
                _subscribed = true;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribed && _player != null)
            {
                _player.DeathAccepted -= HandleDeathAccepted;
            }

            _subscribed = false;
        }

        private void RemoveDestroyedCorpseReferences()
        {
            _spawnedCorpses.RemoveAll(corpse => corpse == null);
        }

        private sealed class ActiveSceneReloader : IActiveSceneReloader
        {
            public void ReloadActiveScene()
            {
                Scene activeScene = SceneManager.GetActiveScene();
                if (activeScene.buildIndex < 0)
                {
                    Debug.LogError("Cannot restart the active scene because it is not in Build Settings.");
                    return;
                }

                SceneManager.LoadSceneAsync(activeScene.buildIndex);
            }
        }
    }
}
