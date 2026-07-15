using UnityEngine;

namespace CorpseMechanism.Interaction
{
    [DisallowMultipleComponent]
    public sealed class DoorController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Explicit pressure plate controlling this door in the scene.")]
        private PressurePlate _pressurePlate;

        [SerializeField]
        [Tooltip("2D collider that blocks player and corpse while closed.")]
        private Collider2D _blockingCollider;

        [SerializeField]
        [Tooltip("Visual transform moved relative to its stable closed position.")]
        private Transform _visualTransform;

        [SerializeField]
        private SpriteRenderer _renderer;

        [SerializeField]
        [Tooltip("Local-space offset applied from the closed position while open. Level_001 baseline: (0, 4, 0).")]
        private Vector3 _openLocalOffset = new Vector3(0f, 4f, 0f);

        [SerializeField]
        private Color _closedColor = new Color(0.25f, 0.45f, 0.85f, 1f);

        [SerializeField]
        private Color _openColor = new Color(0.3f, 0.85f, 0.9f, 0.45f);

        private Vector3 _closedLocalPosition;
        private bool _hasAppliedState;
        private bool _subscribed;

        public bool IsOpen { get; private set; }

        public PressurePlate PressurePlate => _pressurePlate;

        public Collider2D BlockingCollider => _blockingCollider;

        private void Awake()
        {
            CaptureClosedPosition();
        }

        private void OnEnable()
        {
            CaptureClosedPosition();
            Subscribe();
            SetOpen(_pressurePlate != null && _pressurePlate.IsPressed);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            PressurePlate pressurePlate,
            Collider2D blockingCollider,
            Transform visualTransform,
            SpriteRenderer doorRenderer,
            Vector3 openLocalOffset)
        {
            Unsubscribe();
            _pressurePlate = pressurePlate;
            _blockingCollider = blockingCollider;
            _visualTransform = visualTransform;
            _renderer = doorRenderer;
            _openLocalOffset = openLocalOffset;
            _closedLocalPosition = _visualTransform != null
                ? _visualTransform.localPosition
                : Vector3.zero;
            _hasAppliedState = false;
            Subscribe();
            SetOpen(_pressurePlate != null && _pressurePlate.IsPressed);
        }

        public bool SetOpen(bool open)
        {
            if (_hasAppliedState && IsOpen == open)
            {
                return false;
            }

            IsOpen = open;
            _hasAppliedState = true;

            if (_blockingCollider != null)
            {
                _blockingCollider.enabled = !open;
            }

            if (_visualTransform != null)
            {
                _visualTransform.localPosition =
                    _closedLocalPosition + (open ? _openLocalOffset : Vector3.zero);
            }

            if (_renderer != null)
            {
                _renderer.color = open ? _openColor : _closedColor;
            }

            return true;
        }

        private void CaptureClosedPosition()
        {
            if (_visualTransform != null && !_hasAppliedState)
            {
                _closedLocalPosition = _visualTransform.localPosition;
            }
        }

        private void HandlePressedChanged(bool pressed)
        {
            SetOpen(pressed);
        }

        private void Subscribe()
        {
            if (!_subscribed && isActiveAndEnabled && _pressurePlate != null)
            {
                _pressurePlate.PressedChanged += HandlePressedChanged;
                _subscribed = true;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribed && _pressurePlate != null)
            {
                _pressurePlate.PressedChanged -= HandlePressedChanged;
            }

            _subscribed = false;
        }
    }
}
