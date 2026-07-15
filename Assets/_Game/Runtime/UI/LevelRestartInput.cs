using CorpseMechanism.Level;
using UnityEngine;

namespace CorpseMechanism.UI
{
    [DisallowMultipleComponent]
    public sealed class LevelRestartInput : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Explicit session restarted when R is pressed.")]
        private LevelSession _levelSession;

        public LevelSession LevelSession => _levelSession;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RequestRestart();
            }
        }

        public void Configure(LevelSession levelSession)
        {
            _levelSession = levelSession;
        }

        public bool RequestRestart()
        {
            if (_levelSession == null)
            {
                return false;
            }

            _levelSession.RestartLevel();
            return true;
        }
    }
}
