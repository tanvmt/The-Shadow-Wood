using System;
using System.Collections.Generic;
using System.Reflection;
using TheShadowWood.Core.Audio;
using TheShadowWood.Core.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheShadowWood.Audio.Tests
{
    // Tracks every created object so a test can destroy them all in TearDown.
    internal sealed class TestAudio : IDisposable
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly List<Object> _created = new List<Object>();

        public AudioClip Clip(string name = "clip")
        {
            AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
            _created.Add(clip);
            return clip;
        }

        public AudioCue Cue(
            AudioBus bus = AudioBus.Sfx,
            bool loop = false,
            float volume = 1f,
            bool avoidRepeat = true,
            params AudioClip[] clips)
        {
            AudioCue cue = ScriptableObject.CreateInstance<AudioCue>();
            _created.Add(cue);

            Set(cue, "clips", clips.Length > 0 ? clips : new[] { Clip() });
            Set(cue, "bus", bus);
            Set(cue, "loop", loop);
            Set(cue, "minVolume", volume);
            Set(cue, "maxVolume", volume);
            Set(cue, "avoidRepeat", avoidRepeat);
            return cue;
        }

        public AudioCue EmptyCue()
        {
            AudioCue cue = ScriptableObject.CreateInstance<AudioCue>();
            _created.Add(cue);
            return cue;
        }

        public void Set(AudioCue cue, string field, object value)
        {
            typeof(AudioCue).GetField(field, Private).SetValue(cue, value);
        }

        public void Dispose()
        {
            foreach (Object created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
        }
    }

    internal sealed class FakeGameSettings : IGameSettings
    {
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;

        public event Action Changed;

        public float MasterVolume { get; set; } = 1f;
        public float LookSensitivity { get; set; } = 1f;

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = value;
                Changed?.Invoke();
            }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = value;
                Changed?.Invoke();
            }
        }

        public int ListenerCount => Changed?.GetInvocationList().Length ?? 0;

        public void Save()
        {
        }
    }
}
