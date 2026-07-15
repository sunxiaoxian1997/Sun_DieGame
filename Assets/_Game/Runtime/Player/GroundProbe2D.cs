using UnityEngine;

namespace CorpseMechanism.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class GroundProbe2D : MonoBehaviour
    {
        private const int HitCapacity = 8;
        private const float MinimumGroundNormalY = 0.01f;

        [SerializeField]
        [Tooltip("The player's own collider used for the downward ground cast.")]
        private Collider2D _bodyCollider;

        [SerializeField]
        [Tooltip("Layers that are allowed to count as ground.")]
        private LayerMask _groundLayers = ~0;

        [SerializeField]
        [Range(0.001f, 0.5f)]
        [Tooltip("Ground-check cast distance below the player collider, in world units. Level_001 baseline: 0.08.")]
        private float _castDistance = 0.08f;

        [SerializeField]
        [Tooltip("Whether trigger colliders may count as ground.")]
        private bool _includeTriggers;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[HitCapacity];

        public bool IsGrounded { get; private set; }

        private void Reset()
        {
            _bodyCollider = GetComponent<Collider2D>();
        }

        private void Awake()
        {
            if (_bodyCollider == null)
            {
                _bodyCollider = GetComponent<Collider2D>();
            }

            if (_bodyCollider == null)
            {
                Debug.LogError("GroundProbe2D requires an explicit Collider2D reference.", this);
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            RefreshGroundedState();
        }

        private void OnValidate()
        {
            _castDistance = Mathf.Max(0.001f, _castDistance);
        }

        public void Configure(
            Collider2D bodyCollider,
            LayerMask groundLayers,
            float castDistance,
            bool includeTriggers = false)
        {
            _bodyCollider = bodyCollider;
            _groundLayers = groundLayers;
            _castDistance = Mathf.Max(0.001f, castDistance);
            _includeTriggers = includeTriggers;
        }

        public bool RefreshGroundedState()
        {
            if (_bodyCollider == null || !_bodyCollider.enabled)
            {
                IsGrounded = false;
                return false;
            }

            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _groundLayers,
                useTriggers = _includeTriggers
            };

            int hitCount = _bodyCollider.Cast(
                Vector2.down,
                filter,
                _hits,
                _castDistance);

            IsGrounded = false;
            Rigidbody2D ownBody = _bodyCollider.attachedRigidbody;

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = _hits[index];
                Collider2D hitCollider = hit.collider;

                bool belongsToOwnBody = hitCollider != null &&
                    (hitCollider == _bodyCollider ||
                     (ownBody != null && hitCollider.attachedRigidbody == ownBody));

                if (hitCollider == null ||
                    belongsToOwnBody ||
                    (!_includeTriggers && hitCollider.isTrigger) ||
                    hit.normal.y <= MinimumGroundNormalY)
                {
                    continue;
                }

                IsGrounded = true;
                break;
            }

            return IsGrounded;
        }

        private void OnDrawGizmosSelected()
        {
            Collider2D colliderToDraw = _bodyCollider != null
                ? _bodyCollider
                : GetComponent<Collider2D>();

            if (colliderToDraw == null)
            {
                return;
            }

            Bounds bounds = colliderToDraw.bounds;
            float distance = Mathf.Max(0.001f, _castDistance);
            Vector3 center = bounds.center + Vector3.down * (distance * 0.5f);
            Vector3 size = new Vector3(bounds.size.x, distance, 0f);

            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
