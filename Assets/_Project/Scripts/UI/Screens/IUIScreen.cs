namespace TheShadowWood.UI
{
    public interface IUIScreen
    {
        bool CloseOnCancel { get; }

        void Show();
        void Hide();
        void SetInteractive(bool interactive);
    }
}
