using UnityEngine;

namespace TheShadowWood.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractorView : MonoBehaviour
    {
        [SerializeField] private InteractionConfig config;

        public InteractionConfig Config => config;

        // Colliders under this root are skipped by the raycast.
        public Transform Root => transform;
    }
}
