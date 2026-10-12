using UnityEngine;

namespace TheShadowWood.Player
{
    public sealed class FirstPersonMotor
    {
        private const float InputThreshold = 0.0001f;

        private readonly PlayerMovementConfig _config;
        private Vector3 _direction;

        public FirstPersonMotor(PlayerMovementConfig config)
        {
            _config = config;
        }

        public float Speed { get; private set; }
        public float VerticalVelocity { get; private set; }

        // moveInput is world-space on the XZ plane, magnitude 0..1.
        public Vector3 Tick(Vector3 moveInput, float maxSpeed, bool grounded, float deltaTime)
        {
            float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);
            if (inputMagnitude > InputThreshold)
            {
                _direction = moveInput / moveInput.magnitude;
            }

            float targetSpeed = maxSpeed * inputMagnitude;
            Speed = Mathf.MoveTowards(Speed, targetSpeed, _config.Acceleration * deltaTime);

            if (grounded && VerticalVelocity < 0f)
            {
                VerticalVelocity = _config.GroundedStickVelocity;
            }
            else
            {
                VerticalVelocity = Mathf.Max(
                    VerticalVelocity + _config.Gravity * deltaTime,
                    -_config.TerminalVelocity);
            }

            return _direction * Speed + Vector3.up * VerticalVelocity;
        }
    }
}
