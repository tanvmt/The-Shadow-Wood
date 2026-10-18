using TheShadowWood.Core.Items;
using UnityEngine;

namespace TheShadowWood.Player
{
    // Lives under CameraRoot so the beam follows pitch.
    [DisallowMultipleComponent]
    public sealed class FlashlightView : MonoBehaviour, IFlashlightView
    {
        [SerializeField] private Light beam;

        [Tooltip("The flashlight cannot be switched on until this item is in the inventory.")]
        [SerializeField] private ItemDefinition requiredItem;

        public ItemDefinition RequiredItem => requiredItem;

        private void Awake()
        {
            SetOn(false);
        }

        public void SetOn(bool on)
        {
            beam.enabled = on;
        }

        private void Reset()
        {
            beam = GetComponentInChildren<Light>();
        }
    }
}
