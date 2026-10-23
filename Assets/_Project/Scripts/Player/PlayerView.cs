using UnityEngine;

namespace TheShadowWood.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] private CharacterController controller;

        [Tooltip("Pitch is applied here, yaw on the root.")]
        [SerializeField] private Transform cameraRoot;

        [SerializeField] private FirstPersonLookConfig lookConfig;
        [SerializeField] private PlayerMovementConfig movementConfig;
        [Tooltip("Optional; no footstep sounds when empty.")]
        [SerializeField] private FootstepConfig footsteps;

        public Transform Body => transform;
        public CharacterController Controller => controller;
        public Transform CameraRoot => cameraRoot;
        public FirstPersonLookConfig LookConfig => lookConfig;
        public PlayerMovementConfig MovementConfig => movementConfig;
        public FootstepConfig Footsteps => footsteps;

        private void Reset()
        {
            controller = GetComponent<CharacterController>();

            if (cameraRoot == null)
            {
                cameraRoot = transform.Find("CameraRoot");
            }
        }
    }
}
