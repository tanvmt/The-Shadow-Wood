using UnityEngine;

namespace TheShadowWood.Interaction
{
    [CreateAssetMenu(menuName = "The Shadow Wood/Interaction/Interaction Config", fileName = "InteractionConfig")]
    public sealed class InteractionConfig : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float distance = 3f;

        [Tooltip("Include both interactables and geometry that should block interaction.")]
        [SerializeField] private LayerMask visibilityLayers = Physics.DefaultRaycastLayers;

        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField, Range(4, 64)] private int hitBufferSize = 16;
        [SerializeField] private bool drawDebugRay;

        public float Distance => distance;
        public LayerMask VisibilityLayers => visibilityLayers;
        public QueryTriggerInteraction TriggerInteraction => triggerInteraction;
        public int HitBufferSize => hitBufferSize;
        public bool DrawDebugRay => drawDebugRay;
    }
}
