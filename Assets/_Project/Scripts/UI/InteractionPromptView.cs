using TheShadowWood.Core.Input;
using TheShadowWood.Interaction;
using TMPro;
using UnityEngine;
using VContainer;

namespace TheShadowWood.UI
{
    // One shared world-space label that follows the focused interactable.
    [DisallowMultipleComponent]
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        [Tooltip("Gap above the renderer bounds when the target has no prompt anchor.")]
        [SerializeField, Min(0f)] private float heightOffset = 0.15f;

        private IPlayerInteractor _interactor;
        private IInputService _input;
        private Camera _camera;

        private InteractableBehaviour _target;
        private Renderer[] _targetRenderers;
        private string _keyLabel;
        private bool _visible;

        [Inject]
        public void Construct(IPlayerInteractor interactor, IInputService input, Camera camera)
        {
            _interactor = interactor;
            _input = input;
            _camera = camera;

            _interactor.FocusChanged += HandleFocusChanged;
            _input.ContextChanged += HandleContextChanged;
            Refresh();
        }

        private void Awake()
        {
            if (_interactor == null)
            {
                SetVisible(false);
            }
        }

        private void OnDestroy()
        {
            if (_interactor != null)
            {
                _interactor.FocusChanged -= HandleFocusChanged;
                _input.ContextChanged -= HandleContextChanged;
            }
        }

        private void LateUpdate()
        {
            if (!_visible)
            {
                return;
            }

            if (_target == null)
            {
                SetVisible(false);
                return;
            }

            // Device switch changes the key label without a focus change.
            if (!ReferenceEquals(_keyLabel, _input.InteractBindingLabel))
            {
                Refresh();
                return;
            }

            FollowTarget();
        }

        private void FollowTarget()
        {
            Transform self = transform;
            self.position = GetPromptPosition();
            if (_camera != null)
            {
                self.rotation = _camera.transform.rotation;
            }
        }

        private void HandleFocusChanged(InteractionFocusState focus)
        {
            Refresh();
        }

        private void HandleContextChanged(InputContext context)
        {
            Refresh();
        }

        private void Refresh()
        {
            InteractionFocusState focus = _interactor.CurrentFocus;
            if (!ReferenceEquals(_target, focus.Target))
            {
                _target = focus.Target;
                _targetRenderers = _target != null ? _target.GetComponentsInChildren<Renderer>() : null;
            }

            _keyLabel = _input.InteractBindingLabel;
            string text = _input.Context == InputContext.Gameplay
                ? InteractionPromptText.Build(focus, _keyLabel)
                : null;

            if (text != null)
            {
                label.text = text;
            }

            SetVisible(text != null);
            if (_visible)
            {
                FollowTarget();
            }
        }

        private Vector3 GetPromptPosition()
        {
            Transform anchor = _target.PromptAnchor;
            if (anchor != null)
            {
                return anchor.position;
            }

            bool hasBounds = false;
            Bounds bounds = default;
            for (int i = 0; i < _targetRenderers.Length; i++)
            {
                Renderer targetRenderer = _targetRenderers[i];
                if (targetRenderer == null || !targetRenderer.enabled)
                {
                    continue;
                }

                if (hasBounds)
                {
                    bounds.Encapsulate(targetRenderer.bounds);
                }
                else
                {
                    bounds = targetRenderer.bounds;
                    hasBounds = true;
                }
            }

            if (!hasBounds)
            {
                return _target.transform.position + Vector3.up * heightOffset;
            }

            return new Vector3(bounds.center.x, bounds.max.y + heightOffset, bounds.center.z);
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            if (label != null)
            {
                label.enabled = visible;
            }
        }
    }
}
