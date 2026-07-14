using UnityEngine;

namespace CorpseMechanism.Death
{
    public sealed class DeathContext
    {
        public DeathContext(DeathType deathType, Vector2 position)
        {
            DeathType = deathType;
            Position = position;
        }

        public DeathType DeathType { get; }

        public Vector2 Position { get; }
    }
}
