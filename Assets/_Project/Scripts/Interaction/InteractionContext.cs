using UnityEngine;

namespace TheShadowWood.Interaction
{
    /// <summary>
    /// Immutable data describing the current interaction attempt.
    /// </summary>
    public readonly struct InteractionContext
    {
        public GameObject Interactor { get; }
        public Camera Camera { get; }
        public RaycastHit Hit { get; }

        public InteractionContext(GameObject interactor, Camera camera, RaycastHit hit)
        {
            Interactor = interactor;
            Camera = camera;
            Hit = hit;
        }
    }
}
