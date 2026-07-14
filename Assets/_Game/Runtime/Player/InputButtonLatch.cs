namespace CorpseMechanism.Player
{
    /// <summary>
    /// Preserves a button-down event until the physics loop consumes it once.
    /// </summary>
    public sealed class InputButtonLatch
    {
        private bool _isLatched;

        public bool HasPendingInput => _isLatched;

        public void Latch()
        {
            _isLatched = true;
        }

        public bool Consume()
        {
            bool wasLatched = _isLatched;
            _isLatched = false;
            return wasLatched;
        }

        public void Clear()
        {
            _isLatched = false;
        }
    }
}
