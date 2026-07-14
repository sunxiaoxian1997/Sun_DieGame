using CorpseMechanism.Player;
using UnityEngine;

namespace CorpseMechanism.Death
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class NormalHazard : MonoBehaviour, IDamageSource
    {
        public DeathType DeathType => DeathType.Normal;

        private void Reset()
        {
            Collider2D hazardCollider = GetComponent<Collider2D>();
            if (hazardCollider != null)
            {
                hazardCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryDamage(collision.collider);
        }

        private void TryDamage(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            PlayerLifeController playerLife = other.GetComponentInParent<PlayerLifeController>();
            if (playerLife != null)
            {
                playerLife.TryKill(this);
            }
        }
    }
}
