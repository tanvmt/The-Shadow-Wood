using System;
using UnityEngine;

namespace TheShadowWood.Core.Input
{
    public interface IInputService
    {
        InputContext Context { get; }
        event Action<InputContext> ContextChanged;

        /// <summary>Takes control until the returned handle is disposed. Handles may be disposed in any order.</summary>
        IDisposable PushContext(InputContext context);

        Vector2 Move { get; }

        /// <summary>Per-frame delta for mouse, stick value for gamepad. See <see cref="LookIsPointerDelta"/>.</summary>
        Vector2 Look { get; }

        /// <summary>True when Look came from a mouse/pen delta, which must not be scaled by deltaTime.</summary>
        bool LookIsPointerDelta { get; }

        bool SprintHeld { get; }
        bool CrouchPressed { get; }
        bool InteractPressed { get; }
        bool CancelPressed { get; }
        bool PausePressed { get; }

        // Interact binding for the last used device, e.g. "E".
        string InteractBindingLabel { get; }
    }
}
