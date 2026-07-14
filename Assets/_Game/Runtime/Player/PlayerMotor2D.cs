using UnityEngine;

namespace CorpseMechanism.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(GroundProbe2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Rigidbody2D controlled by this motor.")]
        private Rigidbody2D _body;

        [SerializeField]
        [Tooltip("Ground probe used to authorize a jump.")]
        private GroundProbe2D _groundProbe;

        [SerializeField]
        [Tooltip("MonoBehaviour implementing IPlayerInputSource.")]
        private MonoBehaviour _inputSourceComponent;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Maximum horizontal movement speed in world units per second.")]
        private float _moveSpeed = 5f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Vertical speed assigned when a grounded jump is accepted.")]
        private float _jumpSpeed = 7f;

        [SerializeField]
        [Tooltip("Whether movement input currently controls the Rigidbody2D.")]
        private bool _controlEnabled = true;

        private IPlayerInputSource _inputSource;

        public bool ControlEnabled => _controlEnabled;

        public float MoveSpeed => _moveSpeed;

        public float JumpSpeed => _jumpSpeed;

        private void Reset()
        {
            _body = GetComponent<Rigidbody2D>();
            _groundProbe = GetComponent<GroundProbe2D>();
            _inputSourceComponent = GetComponent<LegacyPlayerInputSource>();
        }

        private void Awake()
        {
            _body = _body != null ? _body : GetComponent<Rigidbody2D>();
            _groundProbe = _groundProbe != null ? _groundProbe : GetComponent<GroundProbe2D>();

            if (!ResolveInputSource() || _body == null || _groundProbe == null)
            {
                Debug.LogError(
                    "PlayerMotor2D requires Rigidbody2D, GroundProbe2D, and IPlayerInputSource references.",
                    this);
                enabled = false;
                return;
            }

            _body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            _inputSource.SetInputEnabled(_controlEnabled);
        }

        private void OnValidate()
        {
            _moveSpeed = Mathf.Max(0f, _moveSpeed);
            _jumpSpeed = Mathf.Max(0f, _jumpSpeed);

            if (_inputSourceComponent != null &&
                !(_inputSourceComponent is IPlayerInputSource))
            {
                Debug.LogError(
                    "PlayerMotor2D input source component must implement IPlayerInputSource.",
                    this);
            }
        }

        private void FixedUpdate()
        {
            if (_body == null || _groundProbe == null || !ResolveInputSource())
            {
                return;
            }

            bool isGrounded = _groundProbe.RefreshGroundedState();
            bool jumpPressed = _inputSource.ConsumeJumpPressed();
            Vector2 velocity = _body.velocity;

            if (!_controlEnabled)
            {
                velocity.x = 0f;
                _body.velocity = velocity;
                return;
            }

            velocity.x = Mathf.Clamp(_inputSource.Horizontal, -1f, 1f) * _moveSpeed;
            if (jumpPressed && isGrounded)
            {
                velocity.y = _jumpSpeed;
            }

            _body.velocity = velocity;
        }

        public void Configure(
            Rigidbody2D body,
            GroundProbe2D groundProbe,
            IPlayerInputSource inputSource,
            float moveSpeed = 5f,
            float jumpSpeed = 7f)
        {
            _body = body;
            _groundProbe = groundProbe;
            _inputSource = inputSource;
            _inputSourceComponent = inputSource as MonoBehaviour;
            _moveSpeed = Mathf.Max(0f, moveSpeed);
            _jumpSpeed = Mathf.Max(0f, jumpSpeed);
        }

        public void SetControlEnabled(bool enabledState)
        {
            _controlEnabled = enabledState;

            if (ResolveInputSource())
            {
                _inputSource.SetInputEnabled(enabledState);
            }

            if (!enabledState && _body != null)
            {
                Vector2 velocity = _body.velocity;
                velocity.x = 0f;
                _body.velocity = velocity;
            }
        }

        private bool ResolveInputSource()
        {
            if (_inputSource != null)
            {
                return true;
            }

            _inputSource = _inputSourceComponent as IPlayerInputSource;
            return _inputSource != null;
        }
    }
}
