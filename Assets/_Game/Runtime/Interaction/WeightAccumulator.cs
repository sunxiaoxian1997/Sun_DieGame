using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorpseMechanism.Interaction
{
    public sealed class WeightAccumulator
    {
        private const float MinimumThreshold = 0.0001f;
        private readonly HashSet<IWeightedObject> _uniqueObjects =
            new HashSet<IWeightedObject>();
        private float _activationThreshold;

        public WeightAccumulator(float activationThreshold = 1f)
        {
            _activationThreshold = Mathf.Max(MinimumThreshold, activationThreshold);
        }

        public event Action<bool> PressedChanged;

        public float CurrentWeight { get; private set; }

        public float ActivationThreshold => _activationThreshold;

        public bool IsPressed { get; private set; }

        public void SetActivationThreshold(float activationThreshold)
        {
            _activationThreshold = Mathf.Max(MinimumThreshold, activationThreshold);
            EvaluateCurrentState();
        }

        public void Evaluate(IList<IWeightedObject> weightedObjects)
        {
            _uniqueObjects.Clear();
            float total = 0f;

            if (weightedObjects != null)
            {
                for (int index = 0; index < weightedObjects.Count; index++)
                {
                    IWeightedObject weightedObject = weightedObjects[index];
                    if (weightedObject == null ||
                        !_uniqueObjects.Add(weightedObject) ||
                        !weightedObject.IsWeightActive)
                    {
                        continue;
                    }

                    total += Mathf.Max(0f, weightedObject.Weight);
                }
            }

            CurrentWeight = Mathf.Max(0f, total);
            EvaluateCurrentState();
        }

        private void EvaluateCurrentState()
        {
            bool pressed = CurrentWeight + Mathf.Epsilon >= _activationThreshold;
            if (pressed == IsPressed)
            {
                return;
            }

            IsPressed = pressed;
            PressedChanged?.Invoke(IsPressed);
        }
    }
}
