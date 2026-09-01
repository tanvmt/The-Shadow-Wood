using UnityEngine;

namespace TheShadowWood.Interaction
{
    public abstract class InteractionHighlight : MonoBehaviour, IInteractionFocusFeedback
    {
        public abstract void SetHighlighted(bool highlighted);
    }
}
