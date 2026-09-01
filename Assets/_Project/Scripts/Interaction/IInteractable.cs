using UnityEngine;

namespace TheShadowWood.Interaction
{
    public interface IInteractable
    {
        Transform InteractionTransform { get; }
        bool CanInteract(InteractionContext context);
        void SetFocused(bool focused, InteractionContext context);
        InteractionResult Interact(InteractionContext context);
    }
}
