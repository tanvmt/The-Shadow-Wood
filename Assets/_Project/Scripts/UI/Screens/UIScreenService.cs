using System;
using System.Collections.Generic;
using TheShadowWood.Core.Input;
using VContainer.Unity;

namespace TheShadowWood.UI
{
    public sealed class UIScreenService : IUIScreenService, ITickable, IDisposable
    {
        private readonly IInputService _input;
        private readonly List<Entry> _stack = new List<Entry>();

        public UIScreenService(IInputService input)
        {
            _input = input;
        }

        public IUIScreen Top => _stack.Count == 0 ? null : _stack[_stack.Count - 1].Screen;
        public bool HasOpenScreen => _stack.Count > 0;

        public bool IsOpen(IUIScreen screen)
        {
            return IndexOf(screen) >= 0;
        }

        public void Open(IUIScreen screen)
        {
            if (screen == null || IsOpen(screen))
            {
                return;
            }

            Top?.SetInteractive(false);

            IDisposable context = _input.PushContext(InputContext.UI);
            _stack.Add(new Entry(screen, context));
            screen.Show();
            screen.SetInteractive(true);
        }

        public void Close(IUIScreen screen)
        {
            int index = IndexOf(screen);
            if (index < 0)
            {
                return;
            }

            bool wasTop = index == _stack.Count - 1;
            Entry entry = _stack[index];
            _stack.RemoveAt(index);

            screen.SetInteractive(false);
            screen.Hide();
            entry.Context.Dispose();

            if (wasTop)
            {
                Top?.SetInteractive(true);
            }
        }

        public void Tick()
        {
            IUIScreen top = Top;
            if (top != null && top.CloseOnCancel && _input.CancelPressed)
            {
                Close(top);
            }
        }

        // Scene views may already be destroyed here, so only hand the input contexts back.
        public void Dispose()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                _stack[i].Context.Dispose();
            }

            _stack.Clear();
        }

        private int IndexOf(IUIScreen screen)
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                if (ReferenceEquals(_stack[i].Screen, screen))
                {
                    return i;
                }
            }

            return -1;
        }

        private readonly struct Entry
        {
            public Entry(IUIScreen screen, IDisposable context)
            {
                Screen = screen;
                Context = context;
            }

            public IUIScreen Screen { get; }
            public IDisposable Context { get; }
        }
    }
}
