using UnityEngine;

namespace TheShadowWood.Core.Audio
{
    public interface IAudioService
    {
        // Null or empty cues return AudioHandle.None and play nothing.
        AudioHandle Play(AudioCue cue, float volumeScale = 1f, float fadeIn = 0f);
        AudioHandle PlayAt(AudioCue cue, Vector3 position, float volumeScale = 1f, float fadeIn = 0f);

        // Follows the target until it is destroyed, then stays at its last position.
        AudioHandle PlayAttached(AudioCue cue, Transform target, float volumeScale = 1f, float fadeIn = 0f);

        bool IsPlaying(AudioHandle handle);
        void Stop(AudioHandle handle, float fadeOut = 0f);
    }
}
