using UnityEngine;

namespace TheShadowWood.Player
{
    [CreateAssetMenu(menuName = "The Shadow Wood/Player/Player Movement Config", fileName = "PlayerMovementConfig")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [Header("Speed (m/s)")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 2f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 3.8f;
        [SerializeField, Min(0.1f)] private float crouchSpeed = 1.2f;
        [Tooltip("m/s²")]
        [SerializeField, Min(0.1f)] private float acceleration = 12f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -15f;
        [Tooltip("Max fall speed, positive.")]
        [SerializeField, Min(1f)] private float terminalVelocity = 50f;
        [Tooltip("Downward speed while grounded, keeps the controller on slopes and stairs.")]
        [SerializeField] private float groundedStickVelocity = -2f;

        [Header("Crouch")]
        [SerializeField, Min(0.5f)] private float standingHeight = 1.8f;
        [SerializeField, Min(0.5f)] private float crouchHeight = 1.1f;
        [Tooltip("m/s")]
        [SerializeField, Min(0.1f)] private float crouchTransitionSpeed = 4f;
        [Tooltip("Must not include the Player layer.")]
        [SerializeField] private LayerMask ceilingMask = 1;
        [SerializeField, Min(0.05f)] private float ceilingCheckRadius = 0.25f;

        [Header("Stamina")]
        [SerializeField, Min(1f)] private float maxStamina = 100f;
        [SerializeField, Min(0f)] private float staminaDrainPerSecond = 20f;
        [SerializeField, Min(0f)] private float staminaRegenPerSecond = 15f;
        [SerializeField, Min(0f)] private float staminaRegenDelay = 1.5f;
        [Tooltip("After running out, sprint unlocks again at this amount.")]
        [SerializeField, Min(0f)] private float staminaToResumeSprint = 25f;

        [Header("Head Bob")]
        [Tooltip("Steps per second.")]
        [SerializeField, Min(0f)] private float walkBobFrequency = 1.8f;
        [SerializeField, Min(0f)] private float sprintBobFrequency = 2.6f;
        [SerializeField, Min(0f)] private float crouchBobFrequency = 1.2f;
        [SerializeField, Min(0f)] private float bobHorizontalAmplitude = 0.02f;
        [SerializeField, Min(0f)] private float bobVerticalAmplitude = 0.03f;
        [SerializeField, Min(0.1f)] private float bobReturnSpeed = 8f;

        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public float CrouchSpeed => crouchSpeed;
        public float Acceleration => acceleration;

        public float Gravity => gravity;
        public float TerminalVelocity => terminalVelocity;
        public float GroundedStickVelocity => groundedStickVelocity;

        public float StandingHeight => standingHeight;
        public float CrouchHeight => crouchHeight;
        public float CrouchTransitionSpeed => crouchTransitionSpeed;
        public LayerMask CeilingMask => ceilingMask;
        public float CeilingCheckRadius => ceilingCheckRadius;

        public float MaxStamina => maxStamina;
        public float StaminaDrainPerSecond => staminaDrainPerSecond;
        public float StaminaRegenPerSecond => staminaRegenPerSecond;
        public float StaminaRegenDelay => staminaRegenDelay;
        public float StaminaToResumeSprint => staminaToResumeSprint;

        public float WalkBobFrequency => walkBobFrequency;
        public float SprintBobFrequency => sprintBobFrequency;
        public float CrouchBobFrequency => crouchBobFrequency;
        public float BobHorizontalAmplitude => bobHorizontalAmplitude;
        public float BobVerticalAmplitude => bobVerticalAmplitude;
        public float BobReturnSpeed => bobReturnSpeed;

        private void OnValidate()
        {
            sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
            crouchHeight = Mathf.Min(crouchHeight, standingHeight);
            staminaToResumeSprint = Mathf.Min(staminaToResumeSprint, maxStamina);
            gravity = Mathf.Min(gravity, -0.1f);
            groundedStickVelocity = Mathf.Min(groundedStickVelocity, 0f);
        }
    }
}
