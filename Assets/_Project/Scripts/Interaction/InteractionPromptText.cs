namespace TheShadowWood.Interaction
{
    public static class InteractionPromptText
    {
        // Null means the prompt should be hidden.
        public static string Build(InteractionFocusState focus, string keyLabel)
        {
            if (!focus.HasTarget)
            {
                return null;
            }

            if (!focus.CanInteract)
            {
                string reason = focus.Target.BlockedReason;
                return string.IsNullOrEmpty(reason) ? null : reason;
            }

            string prompt = focus.Target.Prompt;
            if (string.IsNullOrEmpty(prompt))
            {
                return null;
            }

            return string.IsNullOrEmpty(keyLabel) ? prompt : $"[{keyLabel}] {prompt}";
        }
    }
}
