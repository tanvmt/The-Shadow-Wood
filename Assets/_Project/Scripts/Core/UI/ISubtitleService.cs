namespace TheShadowWood.Core.UI
{
    public interface ISubtitleService
    {
        bool IsShowing { get; }

        // Replaces the current line. duration <= 0 keeps it until Hide.
        void Show(string text, float duration);
        void Hide();
    }
}
