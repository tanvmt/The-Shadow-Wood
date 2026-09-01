using UnityEngine;

namespace TheShadowWood.Interaction
{
    /// <summary>
    /// Package-agnostic adapter. Assign outline behaviours from any third-party package.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BehaviourInteractionHighlight : InteractionHighlight
    {
        [SerializeField] private Behaviour[] outlineBehaviours = new Behaviour[0];

        private void Awake()
        {
            SetHighlighted(false);
        }

        public override void SetHighlighted(bool highlighted)
        {
            for (int i = 0; i < outlineBehaviours.Length; i++)
            {
                if (outlineBehaviours[i] != null)
                {
                    outlineBehaviours[i].enabled = highlighted;
                }
            }
        }
    }
}
