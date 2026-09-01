using TheShadowWood.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace TheShadowWood.Inventory
{
    /// <summary>
    /// Inventory-aware pickup. The world object is removed only after its receiver accepts it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PickupItem : InteractableBehaviour
    {
        [Header("Item")]
        [SerializeField] private string itemId;
        [SerializeField, Min(1)] private int quantity = 1;

        [Header("Lifecycle")]
        [SerializeField] private GameObject destroyTarget;
        [SerializeField, Min(0f)] private float destroyDelay;
        [SerializeField] private bool disableCollidersImmediately = true;

        [Header("Events")]
        [SerializeField] private UnityEvent pickupAccepted;
        [SerializeField] private UnityEvent pickupRejected;

        private bool _consumed;

        public string ItemId => itemId;
        public int Quantity => quantity;

        public override bool CanInteract(InteractionContext context)
        {
            return !_consumed
                   && context.Interactor != null
                   && context.Interactor.GetComponentInParent<PickupReceiverBehaviour>() != null
                   && base.CanInteract(context);
        }

        protected override InteractionResult PerformInteraction(InteractionContext context)
        {
            PickupReceiverBehaviour receiver =
                context.Interactor.GetComponentInParent<PickupReceiverBehaviour>();

            if (receiver == null)
            {
                pickupRejected?.Invoke();
                return InteractionResult.Rejected("No pickup receiver is available.");
            }

            GameObject target = destroyTarget != null ? destroyTarget : gameObject;
            PickupRequest request = new PickupRequest(itemId, quantity, target);

            if (!receiver.TryReceive(request))
            {
                pickupRejected?.Invoke();
                return InteractionResult.Rejected("The inventory rejected this item.");
            }

            _consumed = true;
            SetFocused(false, context);

            if (disableCollidersImmediately)
            {
                Collider[] colliders = target.GetComponentsInChildren<Collider>();
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliders[i].enabled = false;
                }
            }

            pickupAccepted?.Invoke();
            Destroy(target, destroyDelay);
            return InteractionResult.Success();
        }

        private void OnValidate()
        {
            quantity = Mathf.Max(1, quantity);
            destroyDelay = Mathf.Max(0f, destroyDelay);
        }
    }
}
