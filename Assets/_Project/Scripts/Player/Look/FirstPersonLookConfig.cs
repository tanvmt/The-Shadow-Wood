using UnityEngine;

namespace TheShadowWood.Player
{
    [CreateAssetMenu(menuName = "The Shadow Wood/Player/First Person Look Config", fileName = "FirstPersonLookConfig")]
    public sealed class FirstPersonLookConfig : ScriptableObject
    {
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.1f;

        [Tooltip("Degrees per second with the stick fully deflected.")]
        [SerializeField, Min(0f)] private float gamepadSensitivity = 180f;

        [SerializeField] private bool invertY;

        [Tooltip("Lowest pitch (looking up), in degrees.")]
        [SerializeField, Range(-89f, 0f)] private float minPitch = -80f;

        [Tooltip("Highest pitch (looking down), in degrees.")]
        [SerializeField, Range(0f, 89f)] private float maxPitch = 80f;

        public float MouseSensitivity => mouseSensitivity;
        public float GamepadSensitivity => gamepadSensitivity;
        public bool InvertY => invertY;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;
    }
}
