using CorpseMechanism.Player;
using UnityEngine;

namespace CorpseMechanism.Level
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class LevelExit : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Trigger collider defining the level completion area.")]
        private Collider2D _trigger;

        [SerializeField]
        [Tooltip("Current level session completed by the configured player.")]
        private LevelSession _levelSession;

        [SerializeField]
        [Tooltip("Only this active player may complete the level.")]
        private PlayerLifeController _player;

        public Collider2D Trigger => _trigger;

        public LevelSession LevelSession => _levelSession;

        public PlayerLifeController Player => _player;

        private void Reset()
        {
            _trigger = GetComponent<Collider2D>();
            if (_trigger != null)
            {
                _trigger.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryComplete(other);
        }

        public void Configure(
            Collider2D trigger,
            LevelSession levelSession,
            PlayerLifeController player)
        {
            _trigger = trigger;
            _levelSession = levelSession;
            _player = player;

            if (_trigger != null)
            {
                _trigger.isTrigger = true;
            }
        }

        public bool TryComplete(Collider2D other)
        {
            if (other == null ||
                _trigger == null ||
                !_trigger.enabled ||
                _levelSession == null ||
                _levelSession.State != LevelSessionState.Playing ||
                _player == null ||
                !_player.isActiveAndEnabled ||
                !_player.gameObject.activeInHierarchy ||
                _player.State != PlayerLifeState.Alive)
            {
                return false;
            }

            PlayerLifeController enteringPlayer =
                other.GetComponentInParent<PlayerLifeController>();
            return enteringPlayer == _player && _levelSession.TryComplete();
        }
    }
}
