using TheShadowWood.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace TheShadowWood.UI
{
    /// <summary>
    /// Presentation-only view for the centre dot. It reacts to PlayerInteractor events
    /// and never performs physics queries itself.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionCrosshairUI : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerInteractor interactor;
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

        private RectTransform _rectTransform;
        private Vector3 _baseScale;
        private Color _targetColor;
        private float _targetScale;
        private float _pulseTimer;

        private void Awake()
        {
            if (crosshairGraphic != null)
            {
                _rectTransform = crosshairGraphic.rectTransform;
                _baseScale = _rectTransform.localScale;
            }

            ApplyFocusState(InteractionFocusState.None);
        }

        private void OnEnable()
        {
            if (interactor == null)
            {
                Debug.LogError($"{nameof(InteractionCrosshairUI)} on {name} requires a PlayerInteractor reference.", this);
                return;
            }

            interactor.FocusChanged += ApplyFocusState;
            interactor.InteractionCompleted += HandleInteractionCompleted;
            ApplyFocusState(interactor.CurrentFocus);
        }

        private void OnDisable()
        {
            if (interactor == null)
            {
                return;
            }

            interactor.FocusChanged -= ApplyFocusState;
            interactor.InteractionCompleted -= HandleInteractionCompleted;
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
