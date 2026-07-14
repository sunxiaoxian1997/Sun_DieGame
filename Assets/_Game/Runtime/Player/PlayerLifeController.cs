using System;
using CorpseMechanism.Death;
using UnityEngine;

namespace CorpseMechanism.Player
{
    public enum PlayerLifeState
    {
        Alive,
        Dead
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerMotor2D), typeof(GroundProbe2D))]
    public sealed class PlayerLifeController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Motor whose input is disabled while the player is dead.")]
        private PlayerMotor2D _motor;

        [SerializeField]
        [Tooltip("Rigidbody2D moved back to the spawn point during respawn.")]
        private Rigidbody2D _body;

        [SerializeField]
        [Tooltip("Player collider disabled while dead.")]
        private Collider2D _bodyCollider;

        [SerializeField]
        [Tooltip("Ground probe disabled while player physics is suspended.")]
        private GroundProbe2D _groundProbe;

        [SerializeField]
        [Tooltip("Greybox renderer hidden while the player is dead.")]
        private Renderer _renderer;

        public event Action<DeathContext> DeathAccepted;

        public event Action PlayerRespawned;

        public PlayerLifeState State { get; private set; } = PlayerLifeState.Alive;

        public bool IsAlive => State == PlayerLifeState.Alive;

        public DeathContext LastDeathContext { get; private set; }

        private void Reset()
        {
            _motor = GetComponent<PlayerMotor2D>();
            _body = GetComponent<Rigidbody2D>();
            _bodyCollider = GetComponent<Collider2D>();
            _groundProbe = GetComponent<GroundProbe2D>();
            _renderer = GetComponent<Renderer>();
        }

        private void Awake()
        {
            ResolveLocalReferences();
            if (!HasRequiredReferences())
            {
                Debug.LogError("PlayerLifeController is missing required player component references.", this);
                enabled = false;
            }
        }

        public void Configure(
            PlayerMotor2D motor,
            Rigidbody2D body,
            Collider2D bodyCollider,
            GroundProbe2D groundProbe,
            Renderer playerRenderer)
        {
            _motor = motor;
            _body = body;
            _bodyCollider = bodyCollider;
            _groundProbe = groundProbe;
            _renderer = playerRenderer;
        }

        public bool TryKill(IDamageSource source)
        {
            if (source == null || State != PlayerLifeState.Alive || !HasRequiredReferences())
            {
                return false;
            }

            Vector2 deathPosition = _body.position;
            LastDeathContext = new DeathContext(source.DeathType, deathPosition);
            State = PlayerLifeState.Dead;

            _motor.SetControlEnabled(false);
            _body.velocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _body.simulated = false;
            _bodyCollider.enabled = false;
            _groundProbe.enabled = false;
            _renderer.enabled = false;

            DeathAccepted?.Invoke(LastDeathContext);
            return true;
        }

        public bool RespawnAt(Vector2 spawnPosition)
        {
            if (State != PlayerLifeState.Dead || !HasRequiredReferences())
            {
                return false;
            }

            _body.simulated = false;
            transform.position = spawnPosition;
            _body.position = spawnPosition;
            _body.velocity = Vector2.zero;
            _body.angularVelocity = 0f;

            _renderer.enabled = true;
            _bodyCollider.enabled = true;
            _groundProbe.enabled = true;
            _body.simulated = true;
            Physics2D.SyncTransforms();

            State = PlayerLifeState.Alive;
            _motor.SetControlEnabled(true);
            PlayerRespawned?.Invoke();
            return true;
        }

        private void ResolveLocalReferences()
        {
            _motor = _motor != null ? _motor : GetComponent<PlayerMotor2D>();
            _body = _body != null ? _body : GetComponent<Rigidbody2D>();
            _bodyCollider = _bodyCollider != null ? _bodyCollider : GetComponent<Collider2D>();
            _groundProbe = _groundProbe != null ? _groundProbe : GetComponent<GroundProbe2D>();
            _renderer = _renderer != null ? _renderer : GetComponent<Renderer>();
        }

        private bool HasRequiredReferences()
        {
            return _motor != null &&
                _body != null &&
                _bodyCollider != null &&
                _groundProbe != null &&
                _renderer != null;
        }
    }
}
