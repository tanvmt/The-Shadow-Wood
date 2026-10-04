using Cysharp.Threading.Tasks;
using TheShadowWood.Core.Cameras;
using TheShadowWood.Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace TheShadowWood.Player
{
    // Dev only: F focus, G shake, Esc release. Add to the scope Auto Inject list.
    public sealed class CameraSandboxDebug : MonoBehaviour
    {
        [SerializeField] private Transform focusPose;
        [SerializeField] private float shakeForce = 1f;

        private ICameraFocusService _focus;
        private ICameraShakeService _shake;
        private IInputService _input;

        [Inject]
        public void Construct(ICameraFocusService focus, ICameraShakeService shake, IInputService input)
        {
            _focus = focus;
            _shake = shake;
            _input = input;
        }

        private void Update()
        {
            if (_focus == null || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.fKey.wasPressedThisFrame && focusPose != null)
            {
                ToggleFocus();
            }
            else if (_focus.IsFocused && _input.CancelPressed)
            {
                _focus.ReleaseAsync(cancellationToken: destroyCancellationToken).Forget();
            }

            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                _shake.Shake(shakeForce);
            }
        }

        private void ToggleFocus()
        {
            if (_focus.IsFocused)
            {
                _focus.ReleaseAsync(cancellationToken: destroyCancellationToken).Forget();
            }
            else
            {
                _focus.FocusAsync(focusPose, cancellationToken: destroyCancellationToken).Forget();
            }
        }
    }
}
