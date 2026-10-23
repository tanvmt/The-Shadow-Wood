using TheShadowWood.Core.Audio;
using TheShadowWood.Core.Input;
using TheShadowWood.Core.Items;
using VContainer.Unity;

namespace TheShadowWood.Player
{
    // Input context decides when the toggle is live; the inventory decides whether there is a flashlight at all.
    public sealed class FlashlightController : ITickable
    {
        private readonly IFlashlightView _view;
        private readonly IInputService _input;
        private readonly IInventoryService _inventory;
        private readonly IAudioService _audio;

        public FlashlightController(IFlashlightView view, IInputService input, IInventoryService inventory, IAudioService audio)
        {
            _view = view;
            _input = input;
            _inventory = inventory;
            _audio = audio;
        }

        public bool IsHeld { get; private set; }
        public bool IsOn { get; private set; }

        public void Tick()
        {
            SyncHeld();

            if (!_input.FlashlightPressed || !IsHeld)
            {
                return;
            }

            SetOn(!IsOn);
            _audio.Play(_view.SwitchCue);
        }

        private void SyncHeld()
        {
            bool held = _inventory.Has(_view.RequiredItem);
            if (held == IsHeld)
            {
                return;
            }

            IsHeld = held;
            _view.SetHeld(held);

            if (!held && IsOn)
            {
                SetOn(false);
            }
        }

        private void SetOn(bool on)
        {
            IsOn = on;
            _view.SetOn(on);
        }
    }
}
