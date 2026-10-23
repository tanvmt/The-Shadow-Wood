using UnityEngine;
using UnityEngine.UI;

namespace TheShadowWood.UI
{
    [DisallowMultipleComponent]
    public sealed class SettingsScreen : UIScreen
    {
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider lookSensitivitySlider;
        [SerializeField] private Button backButton;

        public Slider MasterVolumeSlider => masterVolumeSlider;
        public Slider MusicVolumeSlider => musicVolumeSlider;
        public Slider SfxVolumeSlider => sfxVolumeSlider;
        public Slider LookSensitivitySlider => lookSensitivitySlider;
        public Button BackButton => backButton;
    }
}
