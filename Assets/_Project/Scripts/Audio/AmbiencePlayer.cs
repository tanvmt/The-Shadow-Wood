using System;
using System.Collections.Generic;
using TheShadowWood.Core.Audio;
using UnityEngine;
using VContainer.Unity;

namespace TheShadowWood.Audio
{
    public sealed class AmbiencePlayer : ITickable, IDisposable
    {
        public const float ExitFadeOut = 1f;

        private readonly IAudioService _audio;
        private readonly Transform _listener;
        private readonly System.Random _random = new System.Random();
        private readonly List<AudioHandle> _loops = new List<AudioHandle>();
        private readonly List<AmbienceSpot> _due = new List<AmbienceSpot>();

        private AmbienceScheduler _scheduler;

        public AmbiencePlayer(IAudioService audio, Camera listener)
        {
            _audio = audio;
            _listener = listener.transform;
        }

        public AmbienceProfile Current { get; private set; }

        public void SetProfile(AmbienceProfile profile, float crossfade)
        {
            if (profile == Current)
            {
                return;
            }

            StopLoops(crossfade);
            Current = profile;

            if (profile == null)
            {
                _scheduler = null;
                return;
            }

            foreach (AudioCue cue in profile.Loops)
            {
                AudioHandle handle = _audio.Play(cue, fadeIn: crossfade);
                if (handle.IsValid)
                {
                    _loops.Add(handle);
                }
            }

            _scheduler = new AmbienceScheduler(profile.Spots, _random);
        }

        // Scaled time, so spots stay quiet while the game is paused.
        public void Tick()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (_scheduler == null)
            {
                return;
            }

            _scheduler.Tick(deltaTime, _due);
            foreach (AmbienceSpot spot in _due)
            {
                _audio.PlayAt(spot.Cue, _listener.position + _scheduler.Offset(spot));
            }
        }

        public void Dispose()
        {
            StopLoops(ExitFadeOut);
            Current = null;
            _scheduler = null;
        }

        private void StopLoops(float fadeOut)
        {
            foreach (AudioHandle handle in _loops)
            {
                _audio.Stop(handle, fadeOut);
            }

            _loops.Clear();
        }
    }
}
