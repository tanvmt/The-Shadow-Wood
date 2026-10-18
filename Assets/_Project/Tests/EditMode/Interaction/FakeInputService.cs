using System;
using TheShadowWood.Core.Input;
using UnityEngine;

namespace TheShadowWood.Interaction.Tests
{
    public sealed class FakeInputService : IInputService
    {
        public InputContext Context { get; private set; } = InputContext.Gameplay;
        public event Action<InputContext> ContextChanged;

        public Vector2 Move { get; set; }
        public Vector2 Look { get; set; }
        public bool LookIsPointerDelta { get; set; }
        public bool SprintHeld { get; set; }
        public bool CrouchPressed { get; set; }
        public bool InteractPressed { get; set; }
        public bool CancelPressed { get; set; }
        public bool PausePressed { get; set; }
        public bool FlashlightPressed { get; set; }
        public string InteractBindingLabel { get; set; } = "E";

        public IDisposable PushContext(InputContext context)
        {
            InputContext previous = Context;
            SetContext(context);
            return new Handle(() => SetContext(previous));
        }

        public void SetContext(InputContext context)
        {
            if (Context == context)
            {
                return;
            }

            Context = context;
            ContextChanged?.Invoke(context);
        }

        private sealed class Handle : IDisposable
        {
            private Action _release;

            public Handle(Action release)
            {
                _release = release;
            }

            public void Dispose()
            {
                _release?.Invoke();
                _release = null;
            }
        }
    }
}
