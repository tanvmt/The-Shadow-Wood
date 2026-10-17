using UnityEngine;
using UnityEngine.UI;

namespace TheShadowWood.UI
{
    [DisallowMultipleComponent]
    public sealed class SettingsScreen : UIScreen
    {
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider lookSensitivitySlider;
        [SerializeField] private Button backButton;

        public Slider MasterVolumeSlider => masterVolumeSlider;
        public Slider LookSensitivitySlider => lookSensitivitySlider;
        public Button BackButton => backButton;
    }
}
