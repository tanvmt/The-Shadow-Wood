using TheShadowWood.Core.Items;
using TheShadowWood.Interaction;
using UnityEngine;
using UnityEngine.Events;
using VContainer;

namespace TheShadowWood.Inventory
{
    // The world object is removed only after the inventory accepts the item.
    [DisallowMultipleComponent]
    public sealed class PickupItem : InteractableBehaviour
    {
        [Header("Item")]
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity = 1;

        [Header("Lifecycle")]
        [SerializeField] private GameObject destroyTarget;
        [SerializeField, Min(0f)] private float destroyDelay;
        [SerializeField] private bool disableCollidersImmediately = true;

        [Header("Events")]
        [SerializeField] private UnityEvent pickupAccepted;
        [SerializeField] private UnityEvent pickupRejected;

        private IInventoryService _inventory;
        private bool _consumed;

        public ItemDefinition Item => item;
        public int Quantity => quantity;

        [Inject]
        public void Construct(IInventoryService inventory)
        {
            _inventory = inventory;
        }

        // False while the inventory is full, so the prompt shows the blocked reason instead.
        public override bool CanInteract(InteractionContext context)
        {
            return !_consumed
                   && _inventory != null
                   && item != null
                   && _inventory.Count(item) + quantity <= item.MaxCount
                   && base.CanInteract(context);
        }

        protected override InteractionResult PerformInteraction(InteractionContext context)
        {
            if (!_inventory.TryAdd(item, quantity))
            {
                pickupRejected?.Invoke();
                return InteractionResult.Rejected("The inventory rejected this item.");
            }

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
