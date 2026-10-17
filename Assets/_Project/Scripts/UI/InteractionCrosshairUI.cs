using TheShadowWood.Core.Input;
using TheShadowWood.Interaction;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace TheShadowWood.UI
{
    [DisallowMultipleComponent]
    public sealed class InteractionCrosshairUI : MonoBehaviour
    {
        [SerializeField] private Graphic crosshairGraphic;

        [Header("Colors")]
        [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color unavailableColor = new Color(0.65f, 0.65f, 0.65f, 0.75f);

        [Header("Scale")]
        [SerializeField, Min(0.01f)] private float idleScale = 1f;
        [SerializeField, Min(0.01f)] private float focusedScale = 1.35f;
        [SerializeField, Min(0f)] private float transitionSpeed = 14f;
        [SerializeField, Min(0f)] private float successPulseScale = 0.35f;
        [SerializeField, Min(0.01f)] private float successPulseDuration = 0.12f;

        private IPlayerInteractor _interactor;
        private IInputService _input;

        private RectTransform _rectTransform;
        private Vector3 _baseScale;
        private Color _targetColor;
        private float _targetScale;
        private float _pulseTimer;

        [Inject]
        public void Construct(IPlayerInteractor interactor, IInputService input)
        {
            _interactor = interactor;
            _input = input;

            _interactor.FocusChanged += ApplyFocusState;
            _interactor.InteractionCompleted += HandleInteractionCompleted;
            _input.ContextChanged += ApplyContext;

            ApplyFocusState(_interactor.CurrentFocus);
            ApplyContext(_input.Context);
        }

        private void Awake()
        {
            if (crosshairGraphic != null)
            {
                _rectTransform = crosshairGraphic.rectTransform;
                _baseScale = _rectTransform.localScale;
            }

            if (_interactor == null)
            {
                ApplyFocusState(InteractionFocusState.None);
            }
        }

        private void OnDestroy()
        {
            if (_interactor == null)
            {
                return;
            }

            _interactor.FocusChanged -= ApplyFocusState;
            _interactor.InteractionCompleted -= HandleInteractionCompleted;
            _input.ContextChanged -= ApplyContext;
        }

        private void Update()
        {
            if (crosshairGraphic == null || _rectTransform == null)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            float blend = transitionSpeed <= 0f
                ? 1f
                : 1f - Mathf.Exp(-transitionSpeed * deltaTime);

            crosshairGraphic.color = Color.Lerp(crosshairGraphic.color, _targetColor, blend);

            float pulse = 0f;
            if (_pulseTimer > 0f)
            {
                _pulseTimer = Mathf.Max(0f, _pulseTimer - deltaTime);
                pulse = Mathf.Sin((_pulseTimer / successPulseDuration) * Mathf.PI) * successPulseScale;
            }

            Vector3 desiredScale = _baseScale * (_targetScale + pulse);
            _rectTransform.localScale = Vector3.Lerp(_rectTransform.localScale, desiredScale, blend);
        }

        private void ApplyContext(InputContext context)
        {
            if (crosshairGraphic != null)
            {
                crosshairGraphic.enabled = context == InputContext.Gameplay;
            }
        }

        private void ApplyFocusState(InteractionFocusState state)
        {
            if (!state.HasTarget)
            {
                _targetColor = idleColor;
                _targetScale = idleScale;
                return;
            }

            _targetColor = state.CanInteract ? availableColor : unavailableColor;
            _targetScale = focusedScale;
        }

        private void HandleInteractionCompleted(InteractionResult result)
        {
            if (result.Succeeded)
            {
                _pulseTimer = successPulseDuration;
            }
        }

        private void OnValidate()
        {
            idleScale = Mathf.Max(0.01f, idleScale);
            focusedScale = Mathf.Max(0.01f, focusedScale);
            transitionSpeed = Mathf.Max(0f, transitionSpeed);
            successPulseScale = Mathf.Max(0f, successPulseScale);
            successPulseDuration = Mathf.Max(0.01f, successPulseDuration);
        }
    }
}
