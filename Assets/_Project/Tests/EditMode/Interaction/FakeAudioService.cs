using System.Collections.Generic;
using TheShadowWood.Core.Audio;
using UnityEngine;

namespace TheShadowWood.Interaction.Tests
{
    internal sealed class FakeAudioService : IAudioService
    {
        private readonly HashSet<AudioHandle> _playing = new HashSet<AudioHandle>();
        private int _lastId;

        public List<(AudioCue Cue, Transform Target, Vector3? Position, float FadeIn)> Plays { get; } =
            new List<(AudioCue, Transform, Vector3?, float)>();
        public AudioHandle LastHandle { get; private set; }
        public List<(AudioHandle Handle, float FadeOut)> Stops { get; } = new List<(AudioHandle, float)>();

        // Cues without clips play nothing, like the real service.
        public AudioHandle Play(AudioCue cue, float volumeScale = 1f, float fadeIn = 0f)
        {
            return Record(cue, null, null, fadeIn);
        }

        public AudioHandle PlayAt(AudioCue cue, Vector3 position, float volumeScale = 1f, float fadeIn = 0f)
        {
            return Record(cue, null, position, fadeIn);
        }

        public AudioHandle PlayAttached(AudioCue cue, Transform target, float volumeScale = 1f, float fadeIn = 0f)
        {
            return Record(cue, target, null, fadeIn);
        }

        public bool IsPlaying(AudioHandle handle)
        {
            return _playing.Contains(handle);
        }

        public void Stop(AudioHandle handle, float fadeOut = 0f)
        {
            Stops.Add((handle, fadeOut));
            _playing.Remove(handle);
        }

        public void Finish(AudioHandle handle)
        {
            _playing.Remove(handle);
        }

        private AudioHandle Record(AudioCue cue, Transform target, Vector3? position, float fadeIn)
        {
            Plays.Add((cue, target, position, fadeIn));
            if (cue == null || !cue.HasClips)
            {
                return AudioHandle.None;
            }

            AudioHandle handle = new AudioHandle(++_lastId);
            _playing.Add(handle);
            LastHandle = handle;
            return handle;
        }
    }
}
