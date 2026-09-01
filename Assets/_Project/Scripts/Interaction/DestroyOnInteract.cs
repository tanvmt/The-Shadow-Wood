using UnityEngine;
using UnityEngine.Events;

namespace TheShadowWood.Interaction
{
    /// <summary>
    /// Generic one-shot interactable. Use it for disposable test items or world objects.
    /// Inventory pickups should validate Inventory.TryAdd before destroying instead.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DestroyOnInteract : InteractableBehaviour
    {
        [Header("Destroy")]
        [SerializeField] private GameObject destroyTarget;
        [SerializeField, Min(0f)] private float destroyDelay;
        [SerializeField] private bool disableCollidersImmediately = true;

        [Header("Events")]
        [SerializeField] private UnityEvent beforeDestroyed;

        private bool _consumed;

        public override bool CanInteract(InteractionContext context)
        {
            return !_consumed && base.CanInteract(context);
        }

        protected override InteractionResult PerformInteraction(InteractionContext context)
        {
            _consumed = true;
            SetFocused(false, context);

            GameObject target = destroyTarget != null ? destroyTarget : gameObject;

            if (disableCollidersImmediately)
            {
                Collider[] colliders = target.GetComponentsInChildren<Collider>();
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliders[i].enabled = false;
                }
            }

            beforeDestroyed?.Invoke();
            Destroy(target, destroyDelay);
            return InteractionResult.Success();
        }

        private void OnValidate()
        {
            destroyDelay = Mathf.Max(0f, destroyDelay);
        }
    }
}
