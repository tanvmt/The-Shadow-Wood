namespace TheShadowWood.Core.Audio
{
    public readonly struct TensionChangedMessage
    {
        public TensionStage Previous { get; }
        public TensionStage Current { get; }

        public TensionChangedMessage(TensionStage previous, TensionStage current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
