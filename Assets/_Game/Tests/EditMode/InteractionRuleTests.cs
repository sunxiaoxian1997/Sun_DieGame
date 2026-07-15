using System.Collections.Generic;
using CorpseMechanism.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace CorpseMechanism.Tests.EditMode
{
    public sealed class InteractionRuleTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in _objects)
            {
                if (gameObject != null)
                {
                    Object.DestroyImmediate(gameObject);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void WeightProvider_ExposesConfiguredPositiveWeight()
        {
            WeightProvider provider = CreateProvider(2.5f);

            Assert.That(provider.Weight, Is.EqualTo(2.5f));
            Assert.That(provider.IsWeightActive, Is.True);
        }

        [Test]
        public void WeightProvider_ClampsNegativeWeightToZero()
        {
            WeightProvider provider = CreateProvider(-3f);

            Assert.That(provider.Weight, Is.Zero);
        }

        [Test]
        public void DisabledWeightProvider_IsNotActiveWeight()
        {
            WeightProvider provider = CreateProvider(1f);

            provider.enabled = false;

            Assert.That(provider.IsWeightActive, Is.False);
        }

        [TestCase(0.5f, false)]
        [TestCase(1f, true)]
        [TestCase(1.5f, true)]
        public void Accumulator_ComparesTotalAgainstThreshold(float weight, bool expectedPressed)
        {
            WeightAccumulator accumulator = new WeightAccumulator(1f);

            accumulator.Evaluate(new IWeightedObject[] { new FakeWeight(weight) });

            Assert.That(accumulator.IsPressed, Is.EqualTo(expectedPressed));
            Assert.That(accumulator.CurrentWeight, Is.EqualTo(weight));
        }

        [Test]
        public void Accumulator_CountsSameObjectOnlyOnce()
        {
            WeightAccumulator accumulator = new WeightAccumulator(2f);
            FakeWeight weight = new FakeWeight(1f);

            accumulator.Evaluate(new IWeightedObject[] { weight, weight });

            Assert.That(accumulator.CurrentWeight, Is.EqualTo(1f));
            Assert.That(accumulator.IsPressed, Is.False);
        }

        [Test]
        public void Accumulator_IgnoresInactiveAndNegativeWeights()
        {
            WeightAccumulator accumulator = new WeightAccumulator(1f);

            accumulator.Evaluate(new IWeightedObject[]
            {
                new FakeWeight(3f, false),
                new FakeWeight(-2f)
            });

            Assert.That(accumulator.CurrentWeight, Is.Zero);
            Assert.That(accumulator.IsPressed, Is.False);
        }

        [Test]
        public void Accumulator_ReevaluationRemovesDepartedWeight()
        {
            WeightAccumulator accumulator = new WeightAccumulator(1f);
            accumulator.Evaluate(new IWeightedObject[] { new FakeWeight(1f) });

            accumulator.Evaluate(new IWeightedObject[0]);

            Assert.That(accumulator.CurrentWeight, Is.Zero);
            Assert.That(accumulator.IsPressed, Is.False);
        }

        [Test]
        public void PressedChanged_FiresOnlyForStateTransitions()
        {
            WeightAccumulator accumulator = new WeightAccumulator(1f);
            int eventCount = 0;
            accumulator.PressedChanged += _ => eventCount++;
            FakeWeight weight = new FakeWeight(1f);

            accumulator.Evaluate(new IWeightedObject[] { weight });
            accumulator.Evaluate(new IWeightedObject[] { weight });
            accumulator.Evaluate(new IWeightedObject[0]);
            accumulator.Evaluate(new IWeightedObject[0]);

            Assert.That(eventCount, Is.EqualTo(2));
        }

        [Test]
        public void RaisingThreshold_CanReleasePressedAccumulator()
        {
            WeightAccumulator accumulator = new WeightAccumulator(1f);
            accumulator.Evaluate(new IWeightedObject[] { new FakeWeight(1f) });

            accumulator.SetActivationThreshold(2f);

            Assert.That(accumulator.IsPressed, Is.False);
            Assert.That(accumulator.CurrentWeight, Is.EqualTo(1f));
        }

        [Test]
        public void Door_OpenDisablesBlockingColliderAndMovesFromStableClosedPosition()
        {
            GameObject doorObject = new GameObject("RuleDoor");
            BoxCollider2D blocker = doorObject.AddComponent<BoxCollider2D>();
            SpriteRenderer renderer = doorObject.AddComponent<SpriteRenderer>();
            DoorController door = doorObject.AddComponent<DoorController>();
            door.Configure(null, blocker, doorObject.transform, renderer, new Vector3(0f, 4f));
            _objects.Add(doorObject);

            bool changed = door.SetOpen(true);

            Assert.That(changed, Is.True);
            Assert.That(door.IsOpen, Is.True);
            Assert.That(blocker.enabled, Is.False);
            Assert.That(doorObject.transform.localPosition.y, Is.EqualTo(4f));
        }

        [Test]
        public void Door_RepeatedStateRequestIsIdempotentAndDoesNotDrift()
        {
            GameObject doorObject = new GameObject("IdempotentDoor");
            BoxCollider2D blocker = doorObject.AddComponent<BoxCollider2D>();
            DoorController door = doorObject.AddComponent<DoorController>();
            door.Configure(null, blocker, doorObject.transform, null, new Vector3(0f, 4f));
            _objects.Add(doorObject);

            door.SetOpen(true);
            bool changed = door.SetOpen(true);

            Assert.That(changed, Is.False);
            Assert.That(doorObject.transform.localPosition.y, Is.EqualTo(4f));
        }

        [Test]
        public void Door_CloseRestoresBlockingColliderAndClosedPosition()
        {
            GameObject doorObject = new GameObject("ClosingDoor");
            BoxCollider2D blocker = doorObject.AddComponent<BoxCollider2D>();
            DoorController door = doorObject.AddComponent<DoorController>();
            door.Configure(null, blocker, doorObject.transform, null, new Vector3(0f, 4f));
            _objects.Add(doorObject);

            door.SetOpen(true);
            door.SetOpen(false);

            Assert.That(door.IsOpen, Is.False);
            Assert.That(blocker.enabled, Is.True);
            Assert.That(doorObject.transform.localPosition.y, Is.Zero);
        }

        private WeightProvider CreateProvider(float weight)
        {
            GameObject gameObject = new GameObject("RuleWeight");
            WeightProvider provider = gameObject.AddComponent<WeightProvider>();
            provider.Configure(weight);
            _objects.Add(gameObject);
            return provider;
        }

        private sealed class FakeWeight : IWeightedObject
        {
            public FakeWeight(float weight, bool active = true)
            {
                Weight = weight;
                IsWeightActive = active;
            }

            public float Weight { get; }

            public bool IsWeightActive { get; }
        }
    }
}
