using TheShadowWood.Core.Items;

namespace TheShadowWood.Player
{
    public interface IFlashlightView
    {
        ItemDefinition RequiredItem { get; }
        void SetOn(bool on);
    }
}
