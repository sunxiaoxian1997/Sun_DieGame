using NUnit.Framework;

namespace CorpseMechanism.Tests.EditMode
{
    public sealed class InfrastructureSmokeTests
    {
        [Test]
        public void RuntimeAssembly_IsReachableThroughExpectedBoundary()
        {
            string actualAssemblyName = typeof(RuntimeAssemblyInfo).Assembly.GetName().Name;

            Assert.That(actualAssemblyName, Is.EqualTo(RuntimeAssemblyInfo.AssemblyName));
        }
    }
}
