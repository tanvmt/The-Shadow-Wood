using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheShadowWood.UI
{
    // Visibility via CanvasGroup; open and close through IUIScreenService, not directly.
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreen : MonoBehaviour, IUIScreen
    {
        [SerializeField] private bool closeOnCancel = true;

        [Tooltip("Selected when the screen becomes interactive, for gamepad navigation.")]
        [SerializeField] private Selectable firstSelected;

        private CanvasGroup _canvasGroup;
        private bool _shown;

        public virtual bool CloseOnCancel => closeOnCancel;
        public bool IsShown => _shown;

        private CanvasGroup Group
        {
            get
            {
                if (_canvasGroup == null && !TryGetComponent(out _canvasGroup))
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }

                return _canvasGroup;
            }
        }

        protected virtual void Awake()
        {
            // A presenter may open the screen before Awake runs.
            if (!_shown)
            {
                ApplyVisible(false);
            }
        }

        public void Show()
        {
            _shown = true;
            ApplyVisible(true);
            OnShown();
        }

        public void Hide()
        {
            _shown = false;
            ApplyVisible(false);
            OnHidden();
        }

        public void SetInteractive(bool interactive)
        {
            CanvasGroup group = Group;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;

            if (interactive && firstSelected != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
            }
        }

        protected virtual void OnShown()
        {
        }

        protected virtual void OnHidden()
        {
        }

        private void ApplyVisible(bool visible)
        {
            CanvasGroup group = Group;
            group.alpha = visible ? 1f : 0f;
            if (!visible)
            {
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }
    }
}
