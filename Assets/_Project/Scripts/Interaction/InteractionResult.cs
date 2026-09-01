namespace TheShadowWood.Interaction
{
    /// <summary>
    /// Result returned by an interactable. Message is intended for optional UI feedback.
    /// </summary>
    public readonly struct InteractionResult
    {
        public bool Succeeded { get; }
        public string Message { get; }

        private InteractionResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
        }

        public static InteractionResult Success(string message = "")
        {
            return new InteractionResult(true, message);
        }

        public static InteractionResult Rejected(string message = "")
        {
            return new InteractionResult(false, message);
        }
    }
}
