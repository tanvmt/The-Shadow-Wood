using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TheShadowWood.Interaction
{
    /// <summary>
    /// Performs an occlusion-aware raycast from the centre of the gameplay camera and
    /// owns the focused interactable. It has no dependency on UI, inventory, or outline packages.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private Camera interactionCamera;
        [SerializeField, Min(0.1f)] private float interactionDistance = 3f;
        [Tooltip("Include both interactables and geometry that should block interaction.")]
        [SerializeField] private LayerMask visibilityLayers = Physics.DefaultRaycastLayers;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField, Range(4, 64)] private int hitBufferSize = 16;

        [Header("Debug")]
        [SerializeField] private bool drawDebugRay;

        private RaycastHit[] _hitBuffer;
        private InteractableBehaviour _focusedTarget;
        private InteractionContext _focusedContext;
        private bool _focusedTargetAvailable;

        public event Action<InteractionFocusState> FocusChanged;
        public event Action<InteractionResult> InteractionCompleted;

        public InteractionFocusState CurrentFocus =>
            new InteractionFocusState(_focusedTarget, _focusedTargetAvailable);

        public Camera InteractionCamera => interactionCamera;
        public float InteractionDistance => interactionDistance;

        private void Awake()
        {
            EnsureHitBuffer();
            ResolveCamera();
        }

        private void OnEnable()
        {
            ResolveCamera();
        }

        private void Update()
        {
            RefreshFocus();
        }

#if ENABLE_INPUT_SYSTEM
        public void OnInteract(InputValue value)
        {
            if (value.isPressed)
            {
                TryInteract();
            }
        }
#endif

        public InteractionResult TryInteract()
        {
            RefreshFocus();

            if (_focusedTarget == null)
            {
                return InteractionResult.Rejected("There is nothing to interact with.");
            }

            if (!_focusedTargetAvailable)
            {
                InteractionResult unavailable = InteractionResult.Rejected("Interaction is currently unavailable.");
                InteractionCompleted?.Invoke(unavailable);
                return unavailable;
            }

            InteractionResult result = _focusedTarget.Interact(_focusedContext);
            InteractionCompleted?.Invoke(result);
            RefreshFocus();
            return result;
        }

        public void RefreshFocus()
        {
            EnsureHitBuffer();

            if (interactionCamera == null)
            {
                ResolveCamera();
                if (interactionCamera == null)
                {
                    ClearFocus();
                    return;
                }
            }

            Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            bool hasHit = TryGetNearestVisibleHit(ray, out RaycastHit nearestHit);

            InteractableBehaviour nextTarget = null;
            InteractionContext nextContext = default;
            bool nextTargetAvailable = false;

            if (hasHit)
            {
                nextTarget = nearestHit.collider.GetComponentInParent<InteractableBehaviour>();
                nextContext = new InteractionContext(gameObject, interactionCamera, nearestHit);
                nextTargetAvailable = nextTarget != null && nextTarget.CanInteract(nextContext);
            }

            SetFocus(nextTarget, nextContext, nextTargetAvailable);

            if (drawDebugRay)
            {
                Color color = nextTarget == null
                    ? Color.red
                    : nextTargetAvailable ? Color.green : Color.yellow;
                Debug.DrawRay(ray.origin, ray.direction * interactionDistance, color);
            }
        }

        private bool TryGetNearestVisibleHit(Ray ray, out RaycastHit nearestHit)
        {
            nearestHit = default;
            float nearestDistance = float.PositiveInfinity;

            int hitCount = Physics.RaycastNonAlloc(
                ray,
                _hitBuffer,
                interactionDistance,
                visibilityLayers,
                triggerInteraction);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = _hitBuffer[i].collider;
                if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (_hitBuffer[i].distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = _hitBuffer[i].distance;
                nearestHit = _hitBuffer[i];
            }

            return nearestDistance < float.PositiveInfinity;
        }

        private void SetFocus(
            InteractableBehaviour nextTarget,
            InteractionContext nextContext,
            bool nextTargetAvailable)
        {
            if (_focusedTarget == nextTarget)
            {
                _focusedContext = nextContext;

                if (_focusedTargetAvailable != nextTargetAvailable)
                {
                    _focusedTargetAvailable = nextTargetAvailable;
                    FocusChanged?.Invoke(CurrentFocus);
                }

                return;
            }

            if (_focusedTarget != null)
            {
                _focusedTarget.SetFocused(false, _focusedContext);
            }

            _focusedTarget = nextTarget;
            _focusedContext = nextContext;
            _focusedTargetAvailable = nextTargetAvailable;

            if (_focusedTarget != null)
            {
                _focusedTarget.SetFocused(true, _focusedContext);
            }

            FocusChanged?.Invoke(CurrentFocus);
        }

        private void ClearFocus()
        {
            SetFocus(null, default, false);
        }

        private void ResolveCamera()
        {
            if (interactionCamera == null)
            {
                interactionCamera = Camera.main;
            }
        }

        private void EnsureHitBuffer()
        {
            int requiredSize = Mathf.Clamp(hitBufferSize, 4, 64);
            if (_hitBuffer == null || _hitBuffer.Length != requiredSize)
            {
                _hitBuffer = new RaycastHit[requiredSize];
            }
        }

        private void OnDisable()
        {
            ClearFocus();
        }

        private void OnValidate()
        {
            interactionDistance = Mathf.Max(0.1f, interactionDistance);
            hitBufferSize = Mathf.Clamp(hitBufferSize, 4, 64);
        }
    }
}
