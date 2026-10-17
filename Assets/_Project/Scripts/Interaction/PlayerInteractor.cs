using System;
using TheShadowWood.Core.Input;
using UnityEngine;
using VContainer.Unity;

namespace TheShadowWood.Interaction
{
    public sealed class PlayerInteractor : IPlayerInteractor, ITickable, IDisposable
    {
        private static readonly Vector3 ViewportCentre = new Vector3(0.5f, 0.5f, 0f);

        private readonly PlayerInteractorView _view;
        private readonly InteractionConfig _config;
        private readonly Camera _camera;
        private readonly IInputService _input;
        private readonly RaycastHit[] _hitBuffer;

        private InteractableBehaviour _focusedTarget;
        private InteractionContext _focusedContext;
        private bool _focusedTargetAvailable;

        public PlayerInteractor(PlayerInteractorView view, Camera camera, IInputService input)
        {
            _view = view;
            _config = view.Config;
            _camera = camera;
            _input = input;
            _hitBuffer = new RaycastHit[Mathf.Clamp(_config.HitBufferSize, 4, 64)];

            _input.ContextChanged += HandleContextChanged;
        }

        public event Action<InteractionFocusState> FocusChanged;
        public event Action<InteractionResult> InteractionCompleted;

        public InteractionFocusState CurrentFocus =>
            new InteractionFocusState(_focusedTarget, _focusedTargetAvailable);

        public void Tick()
        {
            if (_input.Context != InputContext.Gameplay)
            {
                return;
            }

            RefreshFocus();

            if (_input.InteractPressed)
            {
                TryInteract();
            }
        }

        public void Dispose()
        {
            _input.ContextChanged -= HandleContextChanged;
            ClearFocus();
        }

        private void HandleContextChanged(InputContext context)
        {
            if (context != InputContext.Gameplay)
            {
                ClearFocus();
            }
        }

        private void TryInteract()
        {
            if (_focusedTarget == null)
            {
                return;
            }

            InteractionResult result = _focusedTargetAvailable
                ? _focusedTarget.Interact(_focusedContext)
                : InteractionResult.Rejected("Interaction is currently unavailable.");

            InteractionCompleted?.Invoke(result);
            RefreshFocus();
        }

        private void RefreshFocus()
        {
            if (_camera == null)
            {
                ClearFocus();
                return;
            }

            Ray ray = _camera.ViewportPointToRay(ViewportCentre);

            InteractableBehaviour nextTarget = null;
            InteractionContext nextContext = default;
            bool nextTargetAvailable = false;

            if (TryGetNearestVisibleHit(ray, out RaycastHit hit))
            {
                nextTarget = hit.collider.GetComponentInParent<InteractableBehaviour>();
                nextContext = new InteractionContext(_view.gameObject, _camera, hit);
                nextTargetAvailable = nextTarget != null && nextTarget.CanInteract(nextContext);
            }

            SetFocus(nextTarget, nextContext, nextTargetAvailable);

            if (_config.DrawDebugRay)
            {
                Color color = nextTarget == null ? Color.red
                    : nextTargetAvailable ? Color.green
                    : Color.yellow;
                Debug.DrawRay(ray.origin, ray.direction * _config.Distance, color);
            }
        }

        private bool TryGetNearestVisibleHit(Ray ray, out RaycastHit nearestHit)
        {
            nearestHit = default;
            float nearestDistance = float.PositiveInfinity;

            int hitCount = Physics.RaycastNonAlloc(
                ray,
                _hitBuffer,
                _config.Distance,
                _config.VisibilityLayers,
                _config.TriggerInteraction);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _hitBuffer[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(_view.Root))
                {
                    continue;
                }

                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    nearestHit = hit;
                }
            }

            return nearestDistance < float.PositiveInfinity;
        }

        private void SetFocus(InteractableBehaviour nextTarget, InteractionContext nextContext, bool nextTargetAvailable)
        {
            // ReferenceEquals so a destroyed target still counts as a change.
            if (ReferenceEquals(_focusedTarget, nextTarget))
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
    }
}
