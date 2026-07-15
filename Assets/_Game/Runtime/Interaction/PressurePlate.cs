using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorpseMechanism.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PressurePlate : MonoBehaviour
    {
        private const int OverlapCapacity = 32;

        [SerializeField]
        [Tooltip("Trigger collider defining this plate's local detection area.")]
        private Collider2D _detectionTrigger;

        [SerializeField]
        [Tooltip("Greybox renderer updated when the plate changes state.")]
        private SpriteRenderer _renderer;

        [SerializeField]
        [Range(0.1f, 5f)]
        [Tooltip("Total unique active gameplay weight required to press this plate. Level_001 baseline: 1.")]
        private float _activationThreshold = 1f;

        [SerializeField]
        private Color _releasedColor = new Color(0.9f, 0.7f, 0.12f, 1f);

        [SerializeField]
        private Color _pressedColor = new Color(0.25f, 0.85f, 0.35f, 1f);

        private readonly Collider2D[] _overlapResults = new Collider2D[OverlapCapacity];
        private readonly HashSet<WeightProvider> _uniqueProviders =
            new HashSet<WeightProvider>();
        private readonly List<IWeightedObject> _weightedObjects =
            new List<IWeightedObject>(OverlapCapacity);
        private WeightAccumulator _accumulator;

        public event Action<bool> PressedChanged;

        public float CurrentWeight => _accumulator != null ? _accumulator.CurrentWeight : 0f;

        public float ActivationThreshold => Mathf.Max(0.0001f, _activationThreshold);

        public bool IsPressed => _accumulator != null && _accumulator.IsPressed;

        public Collider2D DetectionTrigger => _detectionTrigger;

        private void Awake()
        {
            EnsureAccumulator();
        }

        private void OnEnable()
        {
            EnsureAccumulator();
            _accumulator.PressedChanged += HandlePressedChanged;
            RefreshWeights();
            ApplyVisual(IsPressed);
        }

        private void OnDisable()
        {
            if (_accumulator != null)
            {
                _accumulator.PressedChanged -= HandlePressedChanged;
                _weightedObjects.Clear();
                _accumulator.Evaluate(_weightedObjects);
            }

            _uniqueProviders.Clear();
            _weightedObjects.Clear();
        }

        private void FixedUpdate()
        {
            RefreshWeights();
        }

        private void OnValidate()
        {
            _activationThreshold = Mathf.Max(0.0001f, _activationThreshold);
        }

        public void Configure(
            Collider2D detectionTrigger,
            SpriteRenderer plateRenderer,
            float activationThreshold = 1f)
        {
            _detectionTrigger = detectionTrigger;
            _renderer = plateRenderer;
            _activationThreshold = Mathf.Max(0.0001f, activationThreshold);
            EnsureAccumulator();
            _accumulator.SetActivationThreshold(_activationThreshold);
            ApplyVisual(IsPressed);
        }

        public void RefreshWeights()
        {
            EnsureAccumulator();
            _uniqueProviders.Clear();
            _weightedObjects.Clear();

            if (_detectionTrigger == null || !_detectionTrigger.enabled || !isActiveAndEnabled)
            {
                _accumulator.Evaluate(_weightedObjects);
                return;
            }

            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = false,
                useTriggers = true
            };

            int overlapCount = _detectionTrigger.OverlapCollider(filter, _overlapResults);
            for (int index = 0; index < overlapCount; index++)
            {
                Collider2D overlap = _overlapResults[index];
                if (overlap == null || !overlap.enabled)
                {
                    continue;
                }

                WeightProvider provider = overlap.GetComponentInParent<WeightProvider>();
                if (provider != null && _uniqueProviders.Add(provider))
                {
                    _weightedObjects.Add(provider);
                }
            }

            _accumulator.Evaluate(_weightedObjects);
        }

        private void EnsureAccumulator()
        {
            if (_accumulator == null)
            {
                _accumulator = new WeightAccumulator(_activationThreshold);
            }
            else
            {
                _accumulator.SetActivationThreshold(_activationThreshold);
            }
        }

        private void HandlePressedChanged(bool pressed)
        {
            ApplyVisual(pressed);
            PressedChanged?.Invoke(pressed);
        }

        private void ApplyVisual(bool pressed)
        {
            if (_renderer != null)
            {
                _renderer.color = pressed ? _pressedColor : _releasedColor;
            }
        }
    }
}
