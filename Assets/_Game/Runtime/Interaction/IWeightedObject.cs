namespace CorpseMechanism.Interaction
{
    public interface IWeightedObject
    {
        float Weight { get; }

        bool IsWeightActive { get; }
    }
}
