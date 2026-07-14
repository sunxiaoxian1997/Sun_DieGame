using CorpseMechanism.Death;

namespace CorpseMechanism.Level
{
    public interface IRespawnScheduler
    {
        bool RequestRespawn(DeathContext deathContext);
    }
}
