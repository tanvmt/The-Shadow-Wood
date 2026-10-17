using TheShadowWood.Core.UI;
using UnityEngine;
using VContainer.Unity;

namespace TheShadowWood.UI
{
    // Counts scaled time, so a line freezes while the game is paused.
    public sealed class SubtitleService : ISubtitleService, ITickable
    {
        private readonly ISubtitleView _view;
        private float _remaining;
        private bool _timed;

        public SubtitleService(ISubtitleView view)
        {
            _view = view;
        }

        public bool IsShowing { get; private set; }

        public void Show(string text, float duration)
        {
            if (string.IsNullOrEmpty(text))
            {
                Hide();
                return;
            }

            IsShowing = true;
            _timed = duration > 0f;
            _remaining = duration;
            _view.Display(text);
        }

        public void Hide()
        {
            if (!IsShowing)
            {
                return;
            }

            IsShowing = false;
            _timed = false;
            _view.Clear();
        }

        public void Tick()
        {
            Advance(Time.deltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (!_timed)
            {
                return;
            }

            _remaining -= deltaTime;
            if (_remaining <= 0f)
            {
                Hide();
            }
        }
    }
}
