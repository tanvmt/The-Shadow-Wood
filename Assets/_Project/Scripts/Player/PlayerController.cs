using TheShadowWood.Core.Input;
using UnityEngine;
using VContainer.Unity;

namespace TheShadowWood.Player
{
    public sealed class PlayerController : IStartable, ILateTickable
    {
        private readonly PlayerView _view;
        private readonly IInputService _input;
        private readonly FirstPersonLook _look;

        public PlayerController(PlayerView view, IInputService input)
        {
            _view = view;
            _input = input;
            _look = new FirstPersonLook(view.LookConfig);
        }

        public void Start()
        {
            float authoredPitch = Mathf.DeltaAngle(0f, _view.CameraRoot.localEulerAngles.x);
            _look.SetPitch(authoredPitch);
            ApplyPitch();
        }

        public void LateTick()
        {
            // Read Look first: it updates LookIsPointerDelta.
            Vector2 look = _input.Look;
            if (look == Vector2.zero)
            {
                return;
            }

            float yawDelta = _look.Tick(look, _input.LookIsPointerDelta, Time.deltaTime);
            _view.Body.Rotate(0f, yawDelta, 0f, Space.World);
            ApplyPitch();
        }

        private void ApplyPitch()
        {
            _view.CameraRoot.localRotation = Quaternion.Euler(_look.Pitch, 0f, 0f);
        }
    }
}
