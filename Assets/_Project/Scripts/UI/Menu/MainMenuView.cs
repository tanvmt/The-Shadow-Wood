using UnityEngine;
using UnityEngine.UI;

namespace TheShadowWood.UI
{
    // Holds references only; MainMenuPresenter owns the behaviour.
    [DisallowMultipleComponent]
    public sealed class MainMenuView : UIScreen
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;

        public Button StartButton => startButton;
        public Button QuitButton => quitButton;

        // The root menu has nowhere to go back to.
        public override bool CloseOnCancel => false;

        public void SetInteractable(bool interactable)
        {
            startButton.interactable = interactable;
            quitButton.interactable = interactable;
        }
    }
}
