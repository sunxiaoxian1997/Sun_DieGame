using CorpseMechanism.Death;
using UnityEngine;

namespace CorpseMechanism.Corpse
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class CorpseController : MonoBehaviour
    {
        public bool IsInitialized { get; private set; }

        public DeathType DeathType { get; private set; }

        public Vector2 DeathPosition { get; private set; }

        public bool Initialize(DeathContext context)
        {
            if (context == null || IsInitialized)
            {
                return false;
            }

            DeathType = context.DeathType;
            DeathPosition = context.Position;
            IsInitialized = true;

            Rigidbody2D body = GetComponent<Rigidbody2D>();
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            return true;
        }
    }
}
