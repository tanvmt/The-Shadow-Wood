namespace TheShadowWood.UI
{
    public interface IUIScreenService
    {
        IUIScreen Top { get; }
        bool HasOpenScreen { get; }

        bool IsOpen(IUIScreen screen);

        // Pushes the UI input context; only the top screen is interactive.
        void Open(IUIScreen screen);
        void Close(IUIScreen screen);
    }
}
