using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TheShadowWood.UI
{
    // Open and close through IUIScreenService, not directly. Can be left active or inactive in the scene.
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreen : MonoBehaviour, IUIScreen
    {
        [SerializeField] private bool closeOnCancel = true;

        [Tooltip("Selected when the screen becomes interactive, for gamepad navigation.")]
        [SerializeField] private Selectable firstSelected;

        private CanvasGroup _canvasGroup;
        private bool _shown;

        public event Action Shown;
        public event Action Hidden;

        public virtual bool CloseOnCancel => closeOnCancel;
        public bool IsShown => _shown;

        private CanvasGroup Group
        {
            get
            {
                if (_canvasGroup == null)
                {
                    if (!TryGetComponent(out _canvasGroup))
                    {
                        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                    }

                    // A screen placed under another screen must not inherit its locked state.
                    _canvasGroup.ignoreParentGroups = true;
                }

                return _canvasGroup;
            }
        }

        protected virtual void Awake()
        {
            // A presenter may open the screen before Awake runs.
            if (!_shown)
            {
                gameObject.SetActive(false);
            }
        }

        public void Show()
        {
            _shown = true;
            gameObject.SetActive(true);
            Shown?.Invoke();
        }

        public void Hide()
        {
            _shown = false;
            SetInteractive(false);
            gameObject.SetActive(false);
            Hidden?.Invoke();
        }

        public void SetInteractive(bool interactive)
        {
            CanvasGroup group = Group;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;

            EventSystem eventSystem = EventSystem.current;
            if (!interactive || eventSystem == null)
            {
                return;
            }

            // Preselect only for gamepad; with a mouse a selected button looks like a stuck hover.
            bool preselect = firstSelected != null && Gamepad.current != null;
            eventSystem.SetSelectedGameObject(preselect ? firstSelected.gameObject : null);
        }
    }
}
