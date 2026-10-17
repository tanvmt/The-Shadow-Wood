using System;

namespace TheShadowWood.Core.Input
{
    public readonly struct InputContextRule
    {
        public bool Move { get; }
        public bool Look { get; }
        public bool Sprint { get; }
        public bool Crouch { get; }
        public bool Interact { get; }
        public bool Cancel { get; }
        public bool Pause { get; }
        public bool CursorLocked { get; }

        public InputContextRule(bool move, bool look, bool sprint, bool crouch, bool interact, bool cancel, bool pause, bool cursorLocked)
        {
            Move = move;
            Look = look;
            Sprint = sprint;
            Crouch = crouch;
            Interact = interact;
            Cancel = cancel;
            Pause = pause;
            CursorLocked = cursorLocked;
        }
    }

    public static class InputContextRules
    {
        public static InputContextRule Get(InputContext context)
        {
            switch (context)
            {
                //                                                      move look sprint crouch interact cancel pause cursorLocked
                case InputContext.Gameplay: return new InputContextRule(true, true, true, true, true, false, true, true);
                case InputContext.Focus: return new InputContextRule(false, false, false, false, false, true, false, true);
                case InputContext.Puzzle: return new InputContextRule(false, false, false, false, true, true, false, false);
                case InputContext.UI: return new InputContextRule(false, false, false, false, false, true, false, false);
                default: throw new ArgumentOutOfRangeException(nameof(context), context, "Input context has no rule.");
            }
        }
    }
}
