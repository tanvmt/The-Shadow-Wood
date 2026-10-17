using UnityEngine;
using UnityEngine.UI;

namespace TheShadowWood.UI
{
    [DisallowMultipleComponent]
    public sealed class PauseScreen : UIScreen
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;

        public Button ResumeButton => resumeButton;
        public Button SettingsButton => settingsButton;
        public Button MainMenuButton => mainMenuButton;

        // Cancel must not resume the game while leaving to the menu.
        public override bool CloseOnCancel => base.CloseOnCancel && resumeButton.interactable;

        public void SetButtonsEnabled(bool enabled)
        {
            resumeButton.interactable = enabled;
            settingsButton.interactable = enabled;
            mainMenuButton.interactable = enabled;
        }
    }
}
