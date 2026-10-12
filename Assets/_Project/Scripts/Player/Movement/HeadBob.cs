using UnityEngine;

namespace TheShadowWood.Player
{
    public sealed class HeadBob
    {
        private const float MinSpeed = 0.1f;

        private readonly PlayerMovementConfig _config;
        private float _phase;

        public HeadBob(PlayerMovementConfig config)
        {
            _config = config;
        }

        // Offset to add on top of the camera root's base position. Never accumulates.
        public Vector2 Offset { get; private set; }

        public Vector2 Tick(float horizontalSpeed, bool grounded, float frequency, float deltaTime)
        {
            if (!grounded || horizontalSpeed < MinSpeed)
            {
                Offset = Vector2.Lerp(Offset, Vector2.zero, 1f - Mathf.Exp(-_config.BobReturnSpeed * deltaTime));
                return Offset;
            }

            float amplitudeScale = Mathf.Clamp01(horizontalSpeed / _config.WalkSpeed);
            _phase = Mathf.Repeat(_phase + frequency * deltaTime * Mathf.PI, Mathf.PI * 2f);

            // One sway per two steps, one dip per step.
            Offset = new Vector2(
                Mathf.Cos(_phase) * _config.BobHorizontalAmplitude * amplitudeScale,
                Mathf.Sin(_phase * 2f) * _config.BobVerticalAmplitude * amplitudeScale);

            return Offset;
        }
    }
}
