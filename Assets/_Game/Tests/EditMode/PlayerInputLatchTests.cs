using CorpseMechanism.Player;
using NUnit.Framework;

namespace CorpseMechanism.Tests.EditMode
{
    public sealed class PlayerInputLatchTests
    {
        [Test]
        public void Consume_ReturnsLatchedInputOnlyOnce()
        {
            InputButtonLatch latch = new InputButtonLatch();

            latch.Latch();

            Assert.That(latch.Consume(), Is.True);
            Assert.That(latch.Consume(), Is.False);
        }

        [Test]
        public void Clear_RemovesPendingInput()
        {
            InputButtonLatch latch = new InputButtonLatch();
            latch.Latch();

            latch.Clear();

            Assert.That(latch.HasPendingInput, Is.False);
            Assert.That(latch.Consume(), Is.False);
        }
    }
}
