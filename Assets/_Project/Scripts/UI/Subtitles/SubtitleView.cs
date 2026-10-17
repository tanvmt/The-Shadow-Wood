using TMPro;
using UnityEngine;

namespace TheShadowWood.UI
{
    // Holds the label only; SubtitleService owns timing.
    [DisallowMultipleComponent]
    public sealed class SubtitleView : MonoBehaviour, ISubtitleView
    {
        [SerializeField] private TMP_Text label;

        private bool _displaying;

        private void Awake()
        {
            if (!_displaying)
            {
                Clear();
            }
        }

        public void Display(string text)
        {
            _displaying = true;
            label.text = text;
            label.enabled = true;
        }

        public void Clear()
        {
            _displaying = false;
            label.text = string.Empty;
            label.enabled = false;
        }
    }
}
