using UnityEngine;

namespace TheShadowWood.Inventory
{
    public readonly struct PickupRequest
    {
        public string ItemId { get; }
        public int Quantity { get; }
        public GameObject WorldObject { get; }

        public PickupRequest(string itemId, int quantity, GameObject worldObject)
        {
            ItemId = itemId;
            Quantity = quantity;
            WorldObject = worldObject;
        }
    }
}
