using UnityEngine;

namespace TheShadowWood.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerView : MonoBehaviour
    {
        [Tooltip("Pitch is applied here, yaw on the root.")]
        [SerializeField] private Transform cameraRoot;

        [SerializeField] private FirstPersonLookConfig lookConfig;

        public Transform Body => transform;
        public Transform CameraRoot => cameraRoot;
        public FirstPersonLookConfig LookConfig => lookConfig;

        private void Reset()
        {
            if (cameraRoot == null)
            {
                cameraRoot = transform.Find("CameraRoot");
            }
        }
    }
}
