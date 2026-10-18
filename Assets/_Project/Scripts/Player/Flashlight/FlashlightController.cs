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

        public FlashlightController(IFlashlightView view, IInputService input, IInventoryService inventory)
        {
            _view = view;
            _input = input;
            _inventory = inventory;
        }

        public bool IsOn { get; private set; }

        public void Tick()
        {
            if (!_input.FlashlightPressed)
            {
                return;
            }

            if (IsOn)
            {
                SetOn(false);
            }
            else if (_inventory.Has(_view.RequiredItem))
            {
                SetOn(true);
            }
        }

        private void SetOn(bool on)
        {
            IsOn = on;
            _view.SetOn(on);
        }
    }
}
