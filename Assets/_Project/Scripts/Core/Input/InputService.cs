using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheShadowWood.Core.Input
{
    public sealed class InputService : IInputService, IDisposable
    {
        private const string KeyboardMouseGroup = "Keyboard&Mouse";
        private const string GamepadGroup = "Gamepad";
        private const InputBinding.DisplayStringOptions BindingLabelOptions = InputBinding.DisplayStringOptions.DontIncludeInteractions;

        private readonly GameInputActions _actions;
        private readonly InputContextStack _stack;
        private bool _lookIsPointerDelta = true;
        private bool _usingGamepad;
        private string _keyboardInteractLabel;
        private string _gamepadInteractLabel;

        public InputService()
        {
            _actions = new GameInputActions();
            _stack = new InputContextStack(InputContext.Gameplay);
            _stack.Changed += OnStackChanged;
            Application.focusChanged += OnApplicationFocusChanged;

            Apply(_stack.Current);
        }

        public event Action<InputContext> ContextChanged;

        public InputContext Context => _stack.Current;

        public Vector2 Move => _actions.Player.Move.ReadValue<Vector2>();

        public Vector2 Look
        {
            get
            {
                InputAction look = _actions.Player.Look;
                if (look.activeControl != null)
                {
                    InputDevice device = look.activeControl.device;
                    _lookIsPointerDelta = device is Pointer;
                    _usingGamepad = device is Gamepad;
                }

                return look.ReadValue<Vector2>();
            }
        }

        public bool LookIsPointerDelta => _lookIsPointerDelta;
        public bool SprintHeld => _actions.Player.Sprint.IsPressed();
        public bool CrouchPressed => _actions.Player.Crouch.WasPressedThisFrame();
        public bool InteractPressed => _actions.Player.Interact.WasPressedThisFrame();
        public bool CancelPressed => _actions.Player.Cancel.WasPressedThisFrame();
        public bool PausePressed => _actions.Player.Pause.WasPressedThisFrame();

        public string InteractBindingLabel => _usingGamepad
            ? _gamepadInteractLabel ??= _actions.Player.Interact.GetBindingDisplayString(BindingLabelOptions, GamepadGroup)
            : _keyboardInteractLabel ??= _actions.Player.Interact.GetBindingDisplayString(BindingLabelOptions, KeyboardMouseGroup);

        public IDisposable PushContext(InputContext context)
        {
            return _stack.Push(context);
        }

        public void Dispose()
        {
            _stack.Changed -= OnStackChanged;
            Application.focusChanged -= OnApplicationFocusChanged;
            _actions.Dispose();
            ApplyCursor(false);
        }

        private void OnStackChanged(InputContext context)
        {
            Apply(context);
            ContextChanged?.Invoke(context);
        }

        // Unity releases the cursor lock when the window loses focus; restore it on return.
        private void OnApplicationFocusChanged(bool hasFocus)
        {
            if (hasFocus)
            {
                ApplyCursor(InputContextRules.Get(_stack.Current).CursorLocked);
            }
        }

        private void Apply(InputContext context)
        {
            InputContextRule rule = InputContextRules.Get(context);
            GameInputActions.PlayerActions player = _actions.Player;

            SetEnabled(player.Move, rule.Move);
            SetEnabled(player.Look, rule.Look);
            SetEnabled(player.Sprint, rule.Sprint);
            SetEnabled(player.Crouch, rule.Crouch);
            SetEnabled(player.Interact, rule.Interact);
            SetEnabled(player.Cancel, rule.Cancel);
            SetEnabled(player.Pause, rule.Pause);

            ApplyCursor(rule.CursorLocked);
        }

        private static void SetEnabled(InputAction action, bool enabled)
        {
            if (enabled)
            {
                action.Enable();
            }
            else
            {
                action.Disable();
            }
        }

        private static void ApplyCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
