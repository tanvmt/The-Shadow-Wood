using System.Collections.Generic;
using TheShadowWood.Core.Audio;
using UnityEngine;

namespace TheShadowWood.Audio.Tests
{
    internal sealed class FakeAudioService : IAudioService
    {
        private int _lastId;

        public List<PlayCall> Plays { get; } = new List<PlayCall>();
        public List<(AudioHandle Handle, float FadeOut)> Stops { get; } = new List<(AudioHandle, float)>();

        public AudioHandle Play(AudioCue cue, float volumeScale = 1f, float fadeIn = 0f)
        {
            return Record(cue, null, volumeScale, fadeIn);
        }

        public AudioHandle PlayAt(AudioCue cue, Vector3 position, float volumeScale = 1f, float fadeIn = 0f)
        {
            return Record(cue, position, volumeScale, fadeIn);
        }

        public AudioHandle PlayAttached(AudioCue cue, Transform target, float volumeScale = 1f, float fadeIn = 0f)
        {
            return Record(cue, target.position, volumeScale, fadeIn);
        }

        public bool IsPlaying(AudioHandle handle)
        {
            return handle.IsValid && !Stops.Exists(stop => stop.Handle.Equals(handle));
        }

        public void Stop(AudioHandle handle, float fadeOut = 0f)
        {
            Stops.Add((handle, fadeOut));
        }

        private AudioHandle Record(AudioCue cue, Vector3? position, float volumeScale, float fadeIn)
        {
            AudioHandle handle = new AudioHandle(++_lastId);
            Plays.Add(new PlayCall(cue, position, volumeScale, fadeIn, handle));
            return handle;
        }

        public readonly struct PlayCall
        {
            public PlayCall(AudioCue cue, Vector3? position, float volumeScale, float fadeIn, AudioHandle handle)
            {
                Cue = cue;
                Position = position;
                VolumeScale = volumeScale;
                FadeIn = fadeIn;
                Handle = handle;
            }

            public AudioCue Cue { get; }
            public Vector3? Position { get; }
            public float VolumeScale { get; }
            public float FadeIn { get; }
            public AudioHandle Handle { get; }
        }
    }
}
