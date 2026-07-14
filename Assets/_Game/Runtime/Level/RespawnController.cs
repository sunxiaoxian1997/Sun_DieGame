using System;
using System.Collections;
using CorpseMechanism.Death;
using CorpseMechanism.Player;
using UnityEngine;

namespace CorpseMechanism.Level
{
    [DisallowMultipleComponent]
    public sealed class RespawnController : MonoBehaviour, IRespawnScheduler
    {
        [SerializeField]
        [Tooltip("Existing player instance restored after an ordinary death.")]
        private PlayerLifeController _player;

        [SerializeField]
        [Tooltip("Safe transform used as the single respawn position.")]
        private Transform _spawnPoint;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds between accepted death and respawn.")]
        private float _respawnDelay = 0.65f;

        private Coroutine _respawnRoutine;

        public event Action PlayerRespawned;

        public bool IsRespawnPending => _respawnRoutine != null;

        public PlayerLifeController Player => _player;

        public Transform SpawnPoint => _spawnPoint;

        private void OnValidate()
        {
            _respawnDelay = Mathf.Max(0f, _respawnDelay);
        }

        private void OnDisable()
        {
            if (_respawnRoutine != null)
            {
                StopCoroutine(_respawnRoutine);
                _respawnRoutine = null;
            }
        }

        public void Configure(
            PlayerLifeController player,
            Transform spawnPoint,
            float respawnDelay = 0.65f)
        {
            _player = player;
            _spawnPoint = spawnPoint;
            _respawnDelay = Mathf.Max(0f, respawnDelay);
        }

        public bool RequestRespawn(DeathContext deathContext)
        {
            if (deathContext == null ||
                _respawnRoutine != null ||
                _player == null ||
                _spawnPoint == null ||
                !_player.isActiveAndEnabled ||
                _player.State != PlayerLifeState.Dead)
            {
                return false;
            }

            _respawnRoutine = StartCoroutine(RespawnAfterDelay());
            return true;
        }

        public bool RespawnImmediately()
        {
            if (_respawnRoutine != null)
            {
                StopCoroutine(_respawnRoutine);
                _respawnRoutine = null;
            }

            return CompleteRespawn();
        }

        private IEnumerator RespawnAfterDelay()
        {
            if (_respawnDelay > 0f)
            {
                yield return new WaitForSeconds(_respawnDelay);
            }

            _respawnRoutine = null;
            CompleteRespawn();
        }

        private bool CompleteRespawn()
        {
            if (_player == null || _spawnPoint == null || !_player.RespawnAt(_spawnPoint.position))
            {
                return false;
            }

            PlayerRespawned?.Invoke();
            return true;
        }
    }
}
