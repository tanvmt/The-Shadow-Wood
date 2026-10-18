using UnityEngine;

namespace TheShadowWood.Core.Flags
{
    // Identity is the asset reference, so renaming or moving the asset keeps every link intact.
    [CreateAssetMenu(menuName = "The Shadow Wood/Flags/Flag", fileName = "NewFlag")]
    public sealed class FlagId : ScriptableObject
    {
        [Tooltip("Designer note: when this flag gets set.")]
        [SerializeField, TextArea] private string note;
    }
}
