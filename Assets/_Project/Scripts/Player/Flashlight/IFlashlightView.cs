using TheShadowWood.Core.Audio;
using TheShadowWood.Core.Items;

namespace TheShadowWood.Player
{
    public interface IFlashlightView
    {
        ItemDefinition RequiredItem { get; }
        AudioCue SwitchCue { get; }
        void SetHeld(bool held);
        void SetOn(bool on);
    }
}
