using UnityEngine;
using UnityEngine.UI;

namespace TheShadowWood.UI
{
    // Holds references only; MainMenuPresenter owns the behaviour.
    [DisallowMultipleComponent]
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;

        public Button StartButton => startButton;
        public Button QuitButton => quitButton;

        public void SetInteractable(bool interactable)
        {
            startButton.interactable = interactable;
            quitButton.interactable = interactable;
        }
    }
}
