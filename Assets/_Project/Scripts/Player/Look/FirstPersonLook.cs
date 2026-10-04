using UnityEngine;

namespace TheShadowWood.Player
{
    public sealed class FirstPersonLook
    {
        private readonly FirstPersonLookConfig _config;

        public FirstPersonLook(FirstPersonLookConfig config)
        {
            _config = config;
        }

        public float Pitch { get; private set; }

        public float Tick(Vector2 look, bool isPointerDelta, float deltaTime)
        {
            float scale = isPointerDelta
                ? _config.MouseSensitivity
                : _config.GamepadSensitivity * deltaTime;

            float yawDelta = look.x * scale;

            // Up input = negative pitch.
            float pitchDelta = look.y * scale * (_config.InvertY ? 1f : -1f);
            SetPitch(Pitch + pitchDelta);

            return yawDelta;
        }

        public void SetPitch(float pitch)
        {
            Pitch = Mathf.Clamp(pitch, _config.MinPitch, _config.MaxPitch);
        }
    }
}
