namespace TheShadowWood.Core.Settings
{
    public interface ISettingsStore
    {
        float GetFloat(string key, float defaultValue);
        void SetFloat(string key, float value);
        void Save();
    }
}
