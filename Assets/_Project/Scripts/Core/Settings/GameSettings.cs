using System;
using UnityEngine;

namespace TheShadowWood.Core.Settings
{
    public sealed class GameSettings : IGameSettings
    {
        public const float MinLookSensitivity = 0.2f;
        public const float MaxLookSensitivity = 3f;

        private const string MasterVolumeKey = "settings.masterVolume";
        private const string LookSensitivityKey = "settings.lookSensitivity";

        private readonly ISettingsStore _store;
        private float _masterVolume;
        private float _lookSensitivity;

        public GameSettings(ISettingsStore store)
        {
            _store = store;
            _masterVolume = Mathf.Clamp01(store.GetFloat(MasterVolumeKey, 1f));
            _lookSensitivity = ClampSensitivity(store.GetFloat(LookSensitivityKey, 1f));
            AudioListener.volume = _masterVolume;
        }

        public event Action Changed;

        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(clamped, _masterVolume))
                {
                    return;
                }

                _masterVolume = clamped;
                AudioListener.volume = clamped;
                Changed?.Invoke();
            }
        }

        public float LookSensitivity
        {
            get => _lookSensitivity;
            set
            {
                float clamped = ClampSensitivity(value);
                if (Mathf.Approximately(clamped, _lookSensitivity))
                {
                    return;
                }

                _lookSensitivity = clamped;
                Changed?.Invoke();
            }
        }

        public void Save()
        {
            _store.SetFloat(MasterVolumeKey, _masterVolume);
            _store.SetFloat(LookSensitivityKey, _lookSensitivity);
            _store.Save();
        }

        private static float ClampSensitivity(float value)
        {
            return Mathf.Clamp(value, MinLookSensitivity, MaxLookSensitivity);
        }
    }
}
