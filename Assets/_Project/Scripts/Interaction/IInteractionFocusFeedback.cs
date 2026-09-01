namespace TheShadowWood.Interaction
{
    /// <summary>
    /// Presentation boundary used by interactables. Implementations may use an outline,
    /// material, light, animation, or any other visual feedback.
    /// </summary>
    public interface IInteractionFocusFeedback
    {
        void SetHighlighted(bool highlighted);
    }
}
