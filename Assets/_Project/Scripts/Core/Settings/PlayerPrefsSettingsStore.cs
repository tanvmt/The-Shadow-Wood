using UnityEngine;

namespace TheShadowWood.Core.Settings
{
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        public float GetFloat(string key, float defaultValue)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
