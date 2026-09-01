using UnityEngine;

namespace TheShadowWood.Inventory
{
    /// <summary>
    /// Inventory boundary injected through the interacting Player hierarchy.
    /// A real inventory implements this class and decides whether a pickup is accepted.
    /// </summary>
    public abstract class PickupReceiverBehaviour : MonoBehaviour
    {
        public abstract bool TryReceive(PickupRequest request);
    }
}
