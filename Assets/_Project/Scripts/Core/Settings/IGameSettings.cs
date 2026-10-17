using System;

namespace TheShadowWood.Core.Settings
{
    public interface IGameSettings
    {
        event Action Changed;

        // 0..1, applied to AudioListener.volume.
        float MasterVolume { get; set; }

        // Multiplier on the look config sensitivity, see GameSettings limits.
        float LookSensitivity { get; set; }

        void Save();
    }
}
