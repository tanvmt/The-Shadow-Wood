using System;
using TheShadowWood.Core.Settings;
using VContainer.Unity;

namespace TheShadowWood.UI
{
    public sealed class SettingsPresenter : IStartable, IDisposable
    {
        private readonly SettingsScreen _view;
        private readonly IGameSettings _settings;
        private readonly IUIScreenService _screens;

        public SettingsPresenter(SettingsScreen view, IGameSettings settings, IUIScreenService screens)
        {
            _view = view;
            _settings = settings;
            _screens = screens;
        }

        public void Start()
        {
            _view.MasterVolumeSlider.minValue = 0f;
            _view.MasterVolumeSlider.maxValue = 1f;
            _view.LookSensitivitySlider.minValue = GameSettings.MinLookSensitivity;
            _view.LookSensitivitySlider.maxValue = GameSettings.MaxLookSensitivity;

            _view.MasterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            _view.LookSensitivitySlider.onValueChanged.AddListener(OnLookSensitivityChanged);
            _view.BackButton.onClick.AddListener(OnBackClicked);
            _view.Shown += OnShown;
            _view.Hidden += OnHidden;
        }

        public void Dispose()
        {
            if (_view == null)
            {
                return;
            }

            _view.MasterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            _view.LookSensitivitySlider.onValueChanged.RemoveListener(OnLookSensitivityChanged);
            _view.BackButton.onClick.RemoveListener(OnBackClicked);
            _view.Shown -= OnShown;
            _view.Hidden -= OnHidden;
        }

        private void OnShown()
        {
            _view.MasterVolumeSlider.SetValueWithoutNotify(_settings.MasterVolume);
            _view.LookSensitivitySlider.SetValueWithoutNotify(_settings.LookSensitivity);
        }

        private void OnHidden()
        {
            _settings.Save();
        }

        private void OnMasterVolumeChanged(float value)
        {
            _settings.MasterVolume = value;
        }

        private void OnLookSensitivityChanged(float value)
        {
            _settings.LookSensitivity = value;
        }

        private void OnBackClicked()
        {
            _screens.Close(_view);
        }
    }
}
