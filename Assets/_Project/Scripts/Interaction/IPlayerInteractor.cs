using System;

namespace TheShadowWood.Interaction
{
    public interface IPlayerInteractor
    {
        InteractionFocusState CurrentFocus { get; }
        event Action<InteractionFocusState> FocusChanged;
        event Action<InteractionResult> InteractionCompleted;
    }
}
