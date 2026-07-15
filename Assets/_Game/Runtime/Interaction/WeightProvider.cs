using UnityEngine;

namespace CorpseMechanism.Interaction
{
    [DisallowMultipleComponent]
    public sealed class WeightProvider : MonoBehaviour, IWeightedObject
    {
        [SerializeField]
        [Min(0f)]
        [Tooltip("Weight contributed while this component and GameObject are active.")]
        private float _weight = 1f;

        public float Weight => Mathf.Max(0f, _weight);

        public bool IsWeightActive => isActiveAndEnabled && gameObject.activeInHierarchy;

        private void OnValidate()
        {
            _weight = Mathf.Max(0f, _weight);
        }

        public void Configure(float weight)
        {
            _weight = Mathf.Max(0f, weight);
        }
    }
}
