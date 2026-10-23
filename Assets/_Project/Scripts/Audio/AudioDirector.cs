using System;
using MessagePipe;
using TheShadowWood.Core.Audio;
using TheShadowWood.Core.Flags;
using VContainer.Unity;

namespace TheShadowWood.Audio
{
    // The only place that decides which ambience and music play.
    public sealed class AudioDirector : IStartable, IDisposable
    {
        public const float ExitFadeOut = 1f;

        private readonly IAudioService _audio;
        private readonly AmbiencePlayer _ambience;
        private readonly GameplayAudioConfig _config;
        private readonly IFlagService _flags;
        private readonly ISubscriber<FlagChangedMessage> _flagChanged;
        private readonly IPublisher<TensionChangedMessage> _tensionChanged;

        private IDisposable _subscription;
        private AudioCue _music;
        private AudioHandle _musicHandle;

        public AudioDirector(
            IAudioService audio,
            AmbiencePlayer ambience,
            GameplayAudioConfig config,
            IFlagService flags,
            ISubscriber<FlagChangedMessage> flagChanged,
            IPublisher<TensionChangedMessage> tensionChanged)
        {
            _audio = audio;
            _ambience = ambience;
            _config = config;
            _flags = flags;
            _flagChanged = flagChanged;
            _tensionChanged = tensionChanged;
        }

        public AudioState Current { get; private set; }

        public void Start()
        {
            Apply(Resolve(), _config.StartFadeIn);
            _subscription = _flagChanged.Subscribe(_ => Refresh());
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;

            if (_musicHandle.IsValid)
            {
                _audio.Stop(_musicHandle, ExitFadeOut);
            }

            _music = null;
            _musicHandle = AudioHandle.None;
            Current = null;
        }

        private void Refresh()
        {
            AudioState next = Resolve();
            if (next == Current)
            {
                return;
            }

            TensionStage previous = Current.Tension;
            Apply(next, next.Crossfade);

            if (next.Tension != previous)
            {
                _tensionChanged.Publish(new TensionChangedMessage(previous, next.Tension));
            }
        }

        private AudioState Resolve()
        {
            return AudioStateResolver.Resolve(_config.States, _config.Default, _flags.Has);
        }

        private void Apply(AudioState state, float fade)
        {
            Current = state;
            _ambience.SetProfile(state.Ambience, fade);

            // Same track keeps playing instead of restarting.
            if (state.Music == _music)
            {
                return;
            }

            if (_musicHandle.IsValid)
            {
                _audio.Stop(_musicHandle, fade);
            }

            _music = state.Music;
            _musicHandle = _music != null ? _audio.Play(_music, fadeIn: fade) : AudioHandle.None;
        }
    }
}
