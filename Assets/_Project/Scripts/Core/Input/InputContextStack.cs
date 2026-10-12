using System;
using System.Collections.Generic;

namespace TheShadowWood.Core.Input
{
    public sealed class InputContextStack
    {
        private readonly List<Handle> _handles = new List<Handle>();
        private readonly InputContext _baseContext;

        public InputContextStack(InputContext baseContext = InputContext.Gameplay)
        {
            _baseContext = baseContext;
        }

        public event Action<InputContext> Changed;

        public InputContext Current => _handles.Count == 0 ? _baseContext : _handles[_handles.Count - 1].Context;
        public int Depth => _handles.Count;

        public IDisposable Push(InputContext context)
        {
            InputContext previous = Current;
            Handle handle = new Handle(this, context);
            _handles.Add(handle);
            RaiseIfChanged(previous);
            return handle;
        }

        private void Remove(Handle handle)
        {
            InputContext previous = Current;
            if (_handles.Remove(handle))
            {
                RaiseIfChanged(previous);
            }
        }

        private void RaiseIfChanged(InputContext previous)
        {
            InputContext current = Current;
            if (current != previous)
            {
                Changed?.Invoke(current);
            }
        }

        private sealed class Handle : IDisposable
        {
            private InputContextStack _owner;

            public Handle(InputContextStack owner, InputContext context)
            {
                _owner = owner;
                Context = context;
            }

            public InputContext Context { get; }

            public void Dispose()
            {
                InputContextStack owner = _owner;
                _owner = null;
                owner?.Remove(this);
            }
        }
    }
}
