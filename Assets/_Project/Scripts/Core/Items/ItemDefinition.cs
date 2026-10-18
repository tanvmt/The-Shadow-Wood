using UnityEngine;

namespace TheShadowWood.Core.Items
{
    // Identity is the asset reference, like FlagId.
    [CreateAssetMenu(menuName = "The Shadow Wood/Items/Item", fileName = "NewItem")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Tooltip("Empty uses the asset name.")]
        [SerializeField] private string displayName;

        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(1)] private int maxCount = 1;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public int MaxCount => Mathf.Max(1, maxCount);

        private void OnValidate()
        {
            maxCount = Mathf.Max(1, maxCount);
        }
    }
}
