using CorpseMechanism.Death;
using CorpseMechanism.Level;
using CorpseMechanism.Player;
using UnityEngine;

namespace CorpseMechanism.Corpse
{
    [DisallowMultipleComponent]
    public sealed class CorpseFactory : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Active player whose accepted deaths create normal corpses.")]
        private PlayerLifeController _player;

        [SerializeField]
        [Tooltip("Dedicated normal corpse prefab; never use the Player prefab here.")]
        private CorpseController _normalCorpsePrefab;

        [SerializeField]
        [Tooltip("Level session that owns the runtime corpse registry.")]
        private LevelSession _levelSession;

        [SerializeField]
        [Tooltip("Parent for runtime-created corpse instances.")]
        private Transform _corpseParent;

        [SerializeField]
        [Tooltip("Small world-space alignment offset applied after reading DeathContext.Position.")]
        private Vector2 _spawnOffset;

        private bool _subscribed;

        public PlayerLifeController Player => _player;

        public CorpseController NormalCorpsePrefab => _normalCorpsePrefab;

        public LevelSession LevelSession => _levelSession;

        public Transform CorpseParent => _corpseParent;

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
            CorpseController normalCorpsePrefab,
            LevelSession levelSession,
            Transform corpseParent,
            Vector2 spawnOffset = default)
        {
            Unsubscribe();
            _player = player;
            _normalCorpsePrefab = normalCorpsePrefab;
            _levelSession = levelSession;
            _corpseParent = corpseParent;
            _spawnOffset = spawnOffset;
            Subscribe();
        }

        private void HandleDeathAccepted(DeathContext context)
        {
            if (context == null ||
                _normalCorpsePrefab == null ||
                _levelSession == null ||
                _corpseParent == null)
            {
                Debug.LogError(
                    "CorpseFactory cannot create a corpse because a required reference is missing.",
                    this);
                return;
            }

            Vector2 spawnPosition = context.Position + _spawnOffset;
            CorpseController corpse = Instantiate(
                _normalCorpsePrefab,
                spawnPosition,
                Quaternion.identity,
                _corpseParent);

            if (corpse == null || !corpse.Initialize(context))
            {
                if (corpse != null)
                {
                    Destroy(corpse.gameObject);
                }

                Debug.LogError("CorpseFactory failed to initialize a normal corpse.", this);
                return;
            }

            if (!_levelSession.RegisterCorpse(corpse))
            {
                Destroy(corpse.gameObject);
                Debug.LogError("LevelSession rejected a newly created normal corpse.", this);
            }
        }

        private void Subscribe()
        {
            if (!_subscribed && isActiveAndEnabled && _player != null)
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
    }
}
