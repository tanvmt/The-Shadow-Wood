using System;
using System.Collections.Generic;
using TheShadowWood.Core.Audio;
using TheShadowWood.Core.Settings;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace TheShadowWood.Audio
{
    public sealed class AudioService : IAudioService, ITickable, IDisposable
    {
        public const int MaxVoices = 32;

        private readonly IGameSettings _settings;
        private readonly CueClipPicker _picker = new CueClipPicker(new System.Random());
        private readonly List<Voice> _active = new List<Voice>();
        private readonly Stack<Voice> _free = new Stack<Voice>();

        private GameObject _root;
        private int _voiceCount;
        private int _lastId;

        public AudioService(IGameSettings settings)
        {
            _settings = settings;
            _settings.Changed += ApplyVolumes;
        }

        public bool HasPoolObject => _root != null;

        public AudioHandle Play(AudioCue cue, float volumeScale = 1f, float fadeIn = 0f)
        {
            return Start(cue, null, null, volumeScale, fadeIn);
        }

        public AudioHandle PlayAt(AudioCue cue, Vector3 position, float volumeScale = 1f, float fadeIn = 0f)
        {
            return Start(cue, position, null, volumeScale, fadeIn);
        }

        public AudioHandle PlayAttached(AudioCue cue, Transform target, float volumeScale = 1f, float fadeIn = 0f)
        {
            return Start(cue, null, target, volumeScale, fadeIn);
        }

        public bool IsPlaying(AudioHandle handle)
        {
            return Find(handle) != null;
        }

        public void Stop(AudioHandle handle, float fadeOut = 0f)
        {
            Voice voice = Find(handle);
            if (voice == null)
            {
                return;
            }

            if (fadeOut <= 0f || voice.Fade <= 0f)
            {
                Release(voice);
                return;
            }

            voice.Stopping = true;
            voice.FadeTarget = 0f;
            voice.FadeSpeed = voice.Fade / fadeOut;
        }

        // Exposed for tests; null when the handle is no longer playing.
        public AudioSource FindSource(AudioHandle handle)
        {
            return Find(handle)?.Source;
        }

        public void Tick()
        {
            Tick(Time.unscaledDeltaTime);
        }

        public void Tick(float deltaTime)
        {
            bool listenerPaused = AudioListener.pause;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Voice voice = _active[i];

                // Paused sources keep their fade where it is instead of finishing it silently.
                if (listenerPaused && !voice.Source.ignoreListenerPause)
                {
                    continue;
                }

                if (voice.Target != null)
                {
                    voice.Transform.position = voice.Target.position;
                }

                if (!Mathf.Approximately(voice.Fade, voice.FadeTarget))
                {
                    voice.Fade = Mathf.MoveTowards(voice.Fade, voice.FadeTarget, voice.FadeSpeed * deltaTime);
                    ApplyVolume(voice);
                }

                bool fadedOut = voice.Stopping && voice.Fade <= 0f;
                bool finished = !voice.Cue.Loop && !voice.Source.isPlaying;
                if (fadedOut || finished)
                {
                    Release(voice);
                }
            }
        }

        public void Dispose()
        {
            _settings.Changed -= ApplyVolumes;
            _active.Clear();
            _free.Clear();
            _voiceCount = 0;

            if (_root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(_root);
            }
            else
            {
                Object.DestroyImmediate(_root);
            }

            _root = null;
        }

        private AudioHandle Start(AudioCue cue, Vector3? position, Transform target, float volumeScale, float fadeIn)
        {
            AudioClip clip = _picker.Pick(cue);
            if (clip == null)
            {
                return AudioHandle.None;
            }

            Voice voice = Acquire();
            if (voice == null)
            {
                Debug.LogWarning($"AudioService: no free source for '{cue.name}', all {MaxVoices} sources are looping.");
                return AudioHandle.None;
            }

            AudioSource source = voice.Source;
            source.clip = clip;
            source.loop = cue.Loop;
            source.pitch = _picker.Range(cue.MinPitch, cue.MaxPitch);
            source.spatialBlend = cue.SpatialBlend;
            source.minDistance = cue.MinDistance;
            source.maxDistance = cue.MaxDistance;
            source.rolloffMode = cue.Rolloff;
            source.dopplerLevel = 0f;
            source.ignoreListenerPause = cue.Bus == AudioBus.Ui;

            voice.Id = ++_lastId;
            voice.Cue = cue;
            voice.Target = target;
            voice.BaseVolume = _picker.Range(cue.MinVolume, cue.MaxVolume) * Mathf.Max(0f, volumeScale);
            voice.Stopping = false;
            voice.FadeTarget = 1f;
            voice.Fade = fadeIn > 0f ? 0f : 1f;
            voice.FadeSpeed = fadeIn > 0f ? 1f / fadeIn : 0f;

            voice.Transform.position = target != null ? target.position
                : position ?? _root.transform.position;

            ApplyVolume(voice);
            source.Play();
            _active.Add(voice);
            return new AudioHandle(voice.Id);
        }

        private Voice Acquire()
        {
            if (_free.Count > 0)
            {
                return _free.Pop();
            }

            if (_voiceCount < MaxVoices)
            {
                return CreateVoice();
            }

            // _active is in play order, so the first one-shot is the oldest.
            foreach (Voice voice in _active)
            {
                if (!voice.Cue.Loop)
                {
                    Release(voice);
                    return _free.Pop();
                }
            }

            return null;
        }

        private Voice CreateVoice()
        {
            if (_root == null)
            {
                _root = new GameObject("Audio");
                if (Application.isPlaying)
                {
                    Object.DontDestroyOnLoad(_root);
                }
            }

            _voiceCount++;
            GameObject sourceObject = new GameObject($"Source {_voiceCount}");
            sourceObject.transform.SetParent(_root.transform, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;

            return new Voice(source);
        }

        private void Release(Voice voice)
        {
            voice.Source.Stop();
            voice.Source.clip = null;
            voice.Cue = null;
            voice.Target = null;
            voice.Id = 0;

            _active.Remove(voice);
            _free.Push(voice);
        }

        private Voice Find(AudioHandle handle)
        {
            if (!handle.IsValid)
            {
                return null;
            }

            foreach (Voice voice in _active)
            {
                if (voice.Id == handle.Id)
                {
                    return voice;
                }
            }

            return null;
        }

        private void ApplyVolumes()
        {
            foreach (Voice voice in _active)
            {
                ApplyVolume(voice);
            }
        }

        private void ApplyVolume(Voice voice)
        {
            voice.Source.volume = voice.BaseVolume * voice.Fade * BusVolume(voice.Cue.Bus);
        }

        private float BusVolume(AudioBus bus)
        {
            return bus == AudioBus.Music ? _settings.MusicVolume : _settings.SfxVolume;
        }

        private sealed class Voice
        {
            public Voice(AudioSource source)
            {
                Source = source;
                Transform = source.transform;
            }

            public AudioSource Source { get; }
            public Transform Transform { get; }

            public int Id;
            public AudioCue Cue;
            public Transform Target;
            public float BaseVolume;
            public float Fade;
            public float FadeTarget;
            public float FadeSpeed;
            public bool Stopping;
        }
    }
}
