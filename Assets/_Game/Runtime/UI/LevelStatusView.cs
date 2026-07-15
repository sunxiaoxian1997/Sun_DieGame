using CorpseMechanism.Level;
using UnityEngine;

namespace CorpseMechanism.UI
{
    [DisallowMultipleComponent]
    public sealed class LevelStatusView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Explicit session whose lives and state are displayed.")]
        private LevelSession _levelSession;

        [SerializeField]
        private Rect _screenRect = new Rect(16f, 16f, 620f, 110f);

        private bool _subscribed;

        public LevelSession LevelSession => _levelSession;

        public string DisplayText { get; private set; } = string.Empty;

        private void OnEnable()
        {
            Subscribe();
            RefreshDisplay();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnGUI()
        {
            GUI.Label(_screenRect, DisplayText);
        }

        public void Configure(LevelSession levelSession)
        {
            Unsubscribe();
            _levelSession = levelSession;
            Subscribe();
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            DisplayText = _levelSession == null
                ? "Lives: --\nState: Unconfigured"
                : FormatStatus(_levelSession.RemainingLives, _levelSession.State);
        }

        public static string FormatStatus(int remainingLives, LevelSessionState state)
        {
            string text =
                $"Lives: {Mathf.Max(0, remainingLives)}\n" +
                $"State: {state}\n" +
                "Controls: A/D or Arrow Keys, Space Jump, R Restart";

            if (state == LevelSessionState.Failed || state == LevelSessionState.Completed)
            {
                text += "\nPress R to Restart";
            }

            return text;
        }

        private void Subscribe()
        {
            if (_subscribed || !isActiveAndEnabled || _levelSession == null)
            {
                return;
            }

            _levelSession.RemainingLivesChanged += HandleRemainingLivesChanged;
            _levelSession.LevelFailed += HandleStateChanged;
            _levelSession.LevelCompleted += HandleStateChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && _levelSession != null)
            {
                _levelSession.RemainingLivesChanged -= HandleRemainingLivesChanged;
                _levelSession.LevelFailed -= HandleStateChanged;
                _levelSession.LevelCompleted -= HandleStateChanged;
            }

            _subscribed = false;
        }

        private void HandleRemainingLivesChanged(int remainingLives)
        {
            RefreshDisplay();
        }

        private void HandleStateChanged()
        {
            RefreshDisplay();
        }
    }
}
