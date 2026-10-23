using TheShadowWood.Core.Audio;
using TheShadowWood.Core.Items;
using UnityEngine;

namespace TheShadowWood.Player
{
    // Lives under CameraRoot so the beam follows pitch.
    [DisallowMultipleComponent]
    public sealed class FlashlightView : MonoBehaviour, IFlashlightView
    {
        [SerializeField] private Light beam;

        [Tooltip("Hand-held model, hidden until the item is picked up.")]
        [SerializeField] private GameObject model;

        [Tooltip("The flashlight cannot be switched on until this item is in the inventory.")]
        [SerializeField] private ItemDefinition requiredItem;

        [Tooltip("Optional; played on every toggle.")]
        [SerializeField] private AudioCue switchCue;

        public ItemDefinition RequiredItem => requiredItem;
        public AudioCue SwitchCue => switchCue;

        private void Awake()
        {
            SetHeld(false);
            SetOn(false);
        }

        public void SetHeld(bool held)
        {
            if (model != null)
            {
                model.SetActive(held);
            }
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
