namespace CorpseMechanism.Player
{
    public interface IPlayerInputSource
    {
        float Horizontal { get; }

        bool ConsumeJumpPressed();

        void SetInputEnabled(bool enabled);
    }
}
