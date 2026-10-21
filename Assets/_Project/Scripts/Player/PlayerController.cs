using System;
using TheShadowWood.Core.Audio;
using TheShadowWood.Core.Input;
using TheShadowWood.Core.Settings;
using UnityEngine;
using VContainer.Unity;

namespace TheShadowWood.Player
{
    public sealed class PlayerController : IStartable, ITickable, ILateTickable, IDisposable
    {
        private const float SprintSpeedMargin = 0.1f;

        private readonly PlayerView _view;
        private readonly IInputService _input;
        private readonly IGameSettings _settings;
        private readonly IAudioService _audio;
        private readonly PlayerMovementConfig _config;
        private readonly FirstPersonLook _look;
        private readonly FirstPersonMotor _motor;
        private readonly CrouchState _crouch;
        private readonly StaminaModel _stamina;
        private readonly HeadBob _headBob;

        private Vector3 _cameraRootBase;
        private float _horizontalSpeed;
        private bool _isSprinting;

        public PlayerController(PlayerView view, IInputService input, IGameSettings settings, IAudioService audio)
        {
            _view = view;
            _input = input;
            _settings = settings;
            _audio = audio;
            _config = view.MovementConfig;
            _look = new FirstPersonLook(view.LookConfig);
            _motor = new FirstPersonMotor(_config);
            _crouch = new CrouchState(_config);
            _stamina = new StaminaModel(_config);
            _headBob = new HeadBob(_config);
        }

        public StaminaModel Stamina => _stamina;
        public bool IsCrouching => _crouch.IsCrouching;
        public bool IsSprinting => _isSprinting;

        public void Start()
        {
            _cameraRootBase = _view.CameraRoot.localPosition;

            float authoredPitch = Mathf.DeltaAngle(0f, _view.CameraRoot.localEulerAngles.x);
            _look.SetPitch(authoredPitch);

            ApplyControllerHeight();
            ApplyCameraRoot();

            ApplySettings();
            _settings.Changed += ApplySettings;
        }

        // Settings live in Root and outlive this scene.
        public void Dispose()
        {
            _settings.Changed -= ApplySettings;
        }

        public void Tick()
        {
            float deltaTime = Time.deltaTime;
            CharacterController controller = _view.Controller;

            bool canStand = !_crouch.IsCrouching || !IsCeilingBlocked();
            _crouch.Tick(_input.CrouchPressed, canStand, deltaTime);
            ApplyControllerHeight();

            Vector2 move = _input.Move;
            bool wantsSprint = _input.SprintHeld && move.sqrMagnitude > 0f && !_crouch.IsCrouching && _stamina.CanSprint;
            float maxSpeed = _crouch.IsCrouching ? _config.CrouchSpeed
                : wantsSprint ? _config.SprintSpeed
                : _config.WalkSpeed;

            Transform body = _view.Body;
            Vector3 moveWorld = body.right * move.x + body.forward * move.y;
            Vector3 velocity = _motor.Tick(moveWorld, maxSpeed, controller.isGrounded, deltaTime);
            controller.Move(velocity * deltaTime);

            Vector3 actual = controller.velocity;
            _horizontalSpeed = new Vector2(actual.x, actual.z).magnitude;
            _isSprinting = wantsSprint && _horizontalSpeed > _config.WalkSpeed + SprintSpeedMargin;
            _stamina.Tick(_isSprinting, deltaTime);
        }

        public void LateTick()
        {
            float deltaTime = Time.deltaTime;

            // Read Look first: it updates LookIsPointerDelta.
            Vector2 look = _input.Look;
            if (look != Vector2.zero)
            {
                float yawDelta = _look.Tick(look, _input.LookIsPointerDelta, deltaTime);
                _view.Body.Rotate(0f, yawDelta, 0f, Space.World);
            }

            float frequency = _crouch.IsCrouching ? _config.CrouchBobFrequency
                : _isSprinting ? _config.SprintBobFrequency
                : _config.WalkBobFrequency;
            _headBob.Tick(_horizontalSpeed, _view.Controller.isGrounded, frequency, deltaTime);
            if (_headBob.Stepped)
            {
                PlayFootstep();
            }

            ApplyCameraRoot();
        }

        private void PlayFootstep()
        {
            FootstepConfig footsteps = _view.Footsteps;
            if (footsteps == null)
            {
                return;
            }

            // Starts just inside the capsule, so the ray never hits the player's own controller.
            Vector3 feet = _view.Body.position;
            Vector3 origin = feet + Vector3.up * 0.1f;
            PhysicsMaterial material = null;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, footsteps.RayLength + 0.1f,
                    footsteps.GroundMask, QueryTriggerInteraction.Ignore))
            {
                material = hit.collider.sharedMaterial;
                feet = hit.point;
            }

            float volume = footsteps.VolumeFor(_crouch.IsCrouching, _isSprinting);
            _audio.PlayAt(footsteps.CueFor(material), feet, volume);
        }

        private void ApplySettings()
        {
            _look.SensitivityScale = _settings.LookSensitivity;
        }

        private void ApplyControllerHeight()
        {
            CharacterController controller = _view.Controller;
            controller.height = _crouch.Height;
            controller.center = new Vector3(0f, _crouch.Height * 0.5f, 0f);
        }

        private void ApplyCameraRoot()
        {
            Vector2 bob = _headBob.Offset;
            float crouchDrop = _config.StandingHeight - _crouch.Height;

            Transform cameraRoot = _view.CameraRoot;
            cameraRoot.localPosition = new Vector3(
                _cameraRootBase.x + bob.x,
                _cameraRootBase.y - crouchDrop + bob.y,
                _cameraRootBase.z);
            cameraRoot.localRotation = Quaternion.Euler(_look.Pitch, 0f, 0f);
        }

        private bool IsCeilingBlocked()
        {
            float radius = _config.CeilingCheckRadius;
            Vector3 top = _view.Body.position + Vector3.up * (_config.StandingHeight - radius);
            return Physics.CheckSphere(top, radius, _config.CeilingMask, QueryTriggerInteraction.Ignore);
        }
    }
}
