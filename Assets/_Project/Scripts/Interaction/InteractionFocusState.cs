namespace TheShadowWood.Interaction
{
    public readonly struct InteractionFocusState
    {
        public InteractableBehaviour Target { get; }
        public bool HasTarget => Target != null;
        public bool CanInteract { get; }

        public InteractionFocusState(InteractableBehaviour target, bool canInteract)
        {
            Target = target;
            CanInteract = target != null && canInteract;
        }

        public static InteractionFocusState None => new InteractionFocusState(null, false);
    }
}
