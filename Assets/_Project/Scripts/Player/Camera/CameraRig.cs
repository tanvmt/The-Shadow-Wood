using Unity.Cinemachine;
using UnityEngine;

namespace TheShadowWood.Player
{
    [DisallowMultipleComponent]
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private CinemachineBrain brain;

        [Tooltip("Follows the player CameraRoot.")]
        [SerializeField] private CinemachineCamera fpsCamera;

        [Tooltip("Raised above FpsCamera while focused.")]
        [SerializeField] private CinemachineCamera focusCamera;

        [Tooltip("Put on FpsCamera so shakes start at the player.")]
        [SerializeField] private CinemachineImpulseSource impulseSource;

        [SerializeField, Min(0f)] private float defaultFocusBlend = 0.6f;

        public CinemachineBrain Brain => brain;
        // Brain caches its camera in Awake, which can run after the scope builds.
        public Camera OutputCamera => brain.GetComponent<Camera>();
        public CinemachineCamera FpsCamera => fpsCamera;
        public CinemachineCamera FocusCamera => focusCamera;
        public CinemachineImpulseSource ImpulseSource => impulseSource;
        public float DefaultFocusBlend => defaultFocusBlend;
    }
}
