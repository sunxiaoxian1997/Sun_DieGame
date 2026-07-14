using UnityEngine;

namespace CorpseMechanism.Player
{
    [DisallowMultipleComponent]
    public sealed class LegacyPlayerInputSource : MonoBehaviour, IPlayerInputSource
    {
        private readonly InputButtonLatch _jumpPressed = new InputButtonLatch();
        private bool _inputEnabled = true;

        public float Horizontal { get; private set; }

        private void Update()
        {
            if (!_inputEnabled)
            {
                return;
            }

            Horizontal = Input.GetAxisRaw("Horizontal");
            if (Input.GetButtonDown("Jump"))
            {
                _jumpPressed.Latch();
            }
        }

        private void OnDisable()
        {
            ClearInput();
        }

        public bool ConsumeJumpPressed()
        {
            if (!_inputEnabled)
            {
                _jumpPressed.Clear();
                return false;
            }

            return _jumpPressed.Consume();
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                ClearInput();
            }
        }

        private void ClearInput()
        {
            Horizontal = 0f;
            _jumpPressed.Clear();
        }
    }
}
