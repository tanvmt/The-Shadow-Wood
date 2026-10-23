using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TheShadowWood.Core.Audio;
using TheShadowWood.Core.Flags;
using UnityEngine;

namespace TheShadowWood.Audio.Tests
{
    public sealed class AudioDirectorTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private TestAudio _audio;
        private FakeAudioService _service;
        private TestBroker<FlagChangedMessage> _flagBroker;
        private TestBroker<TensionChangedMessage> _tensionBroker;
        private FlagService _flags;
        private GameObject _cameraObject;
        private AmbiencePlayer _ambience;
        private GameplayAudioConfig _config;
        private AudioDirector _director;

        private FlagId _unease;
        private FlagId _high;
        private FlagId _climax;
        private FlagId _unrelated;
        private AudioCue _rain;
        private AudioCue _wind;
        private AudioCue _tensionMusic;
        private AudioCue _climaxMusic;
        private AmbienceProfile _calmProfile;
        private AmbienceProfile _uneaseProfile;
        private AmbienceProfile _climaxProfile;

        [SetUp]
        public void SetUp()
        {
            _audio = new TestAudio();
            _service = new FakeAudioService();
            _flagBroker = new TestBroker<FlagChangedMessage>();
            _tensionBroker = new TestBroker<TensionChangedMessage>();
            _flags = new FlagService(_flagBroker);
            _cameraObject = new GameObject("Listener", typeof(Camera));
            _ambience = new AmbiencePlayer(_service, _cameraObject.GetComponent<Camera>());

            _unease = ScriptableObject.CreateInstance<FlagId>();
            _high = ScriptableObject.CreateInstance<FlagId>();
            _climax = ScriptableObject.CreateInstance<FlagId>();
            _unrelated = ScriptableObject.CreateInstance<FlagId>();

            _rain = _audio.Cue(AudioBus.Ambience, loop: true);
            _wind = _audio.Cue(AudioBus.Ambience, loop: true);
            _tensionMusic = _audio.Cue(AudioBus.Music, loop: true);
            _climaxMusic = _audio.Cue(AudioBus.Music, loop: true);

            _calmProfile = Profile(_rain);
            _uneaseProfile = Profile(_wind);
            _climaxProfile = Profile();

            _config = ScriptableObject.CreateInstance<GameplayAudioConfig>();
            SetField(_config, "defaultAmbience", _calmProfile);
            SetField(_config, "defaultMusic", null);
            SetField(_config, "defaultCrossfade", 4f);
            SetField(_config, "startFadeIn", 2f);
            SetStates(
                new AudioState("Unease", _unease, _uneaseProfile, null, TensionStage.Unease, 3f),
                new AudioState("Tension", _high, _uneaseProfile, _tensionMusic, TensionStage.Tension, 3f),
                new AudioState("Climax", _climax, _climaxProfile, _climaxMusic, TensionStage.Climax, 1f));

            _director = new AudioDirector(_service, _ambience, _config, _flags, _flagBroker, _tensionBroker);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cameraObject);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_calmProfile);
            Object.DestroyImmediate(_uneaseProfile);
            Object.DestroyImmediate(_climaxProfile);
            Object.DestroyImmediate(_unease);
            Object.DestroyImmediate(_high);
            Object.DestroyImmediate(_climax);
            Object.DestroyImmediate(_unrelated);
            _audio.Dispose();
        }

        [Test]
        public void Start_AppliesDefaultWithStartFadeIn()
        {
            _director.Start();

            Assert.That(_director.Current, Is.SameAs(_config.Default));
            Assert.That(_ambience.Current, Is.SameAs(_calmProfile));
            Assert.That(_service.Plays.Count, Is.EqualTo(1));
            Assert.That(_service.Plays[0].Cue, Is.SameAs(_rain));
            Assert.That(_service.Plays[0].FadeIn, Is.EqualTo(2f));
            Assert.That(_tensionBroker.Published, Is.Empty);
        }

        [Test]
        public void Start_FlagAlreadySet_AppliesThatState()
        {
            _flags.Set(_high);

            _director.Start();

            Assert.That(_director.Current.Tension, Is.EqualTo(TensionStage.Tension));
            Assert.That(PlaysOf(_wind), Is.EqualTo(1));
            Assert.That(PlaysOf(_rain), Is.Zero);
            Assert.That(PlayOf(_tensionMusic).FadeIn, Is.EqualTo(2f));
            Assert.That(_tensionBroker.Published, Is.Empty);
        }

        [Test]
        public void SettingFlag_CrossfadesAmbienceAndMusic_AndPublishesTension()
        {
            _director.Start();
            AudioHandle rain = PlayOf(_rain).Handle;

            _flags.Set(_high);

            Assert.That(_ambience.Current, Is.SameAs(_uneaseProfile));
            Assert.That(FadeOutOf(rain), Is.EqualTo(3f));
            Assert.That(PlayOf(_wind).FadeIn, Is.EqualTo(3f));
            Assert.That(PlayOf(_tensionMusic).FadeIn, Is.EqualTo(3f));
            Assert.That(_tensionBroker.Published.Count, Is.EqualTo(1));
            Assert.That(_tensionBroker.Published[0].Previous, Is.EqualTo(TensionStage.Calm));
            Assert.That(_tensionBroker.Published[0].Current, Is.EqualTo(TensionStage.Tension));
        }

        [Test]
        public void LowerListedState_Wins()
        {
            _director.Start();

            _flags.Set(_climax);
            _flags.Set(_unease);

            Assert.That(_director.Current.Tension, Is.EqualTo(TensionStage.Climax));
            Assert.That(PlaysOf(_wind), Is.Zero);
            Assert.That(_tensionBroker.Published.Count, Is.EqualTo(1));
        }

        [Test]
        public void UnrelatedFlag_DoesNothing()
        {
            _director.Start();

            _flags.Set(_unrelated);

            Assert.That(_service.Plays.Count, Is.EqualTo(1));
            Assert.That(_service.Stops, Is.Empty);
            Assert.That(_tensionBroker.Published, Is.Empty);
        }

        [Test]
        public void SameAmbience_KeepsLoopsPlaying()
        {
            _director.Start();
            _flags.Set(_unease);
            AudioHandle wind = PlayOf(_wind).Handle;

            _flags.Set(_high);

            Assert.That(PlaysOf(_wind), Is.EqualTo(1));
            Assert.That(FadeOutOf(wind), Is.Null);
        }

        [Test]
        public void SameMusic_DoesNotRestart()
        {
            SetStates(
                new AudioState("Tension", _high, _uneaseProfile, _tensionMusic, TensionStage.Tension, 3f),
                new AudioState("Climax", _climax, _climaxProfile, _tensionMusic, TensionStage.Climax, 1f));
            _director.Start();
            _flags.Set(_high);
            AudioHandle music = PlayOf(_tensionMusic).Handle;

            _flags.Set(_climax);

            Assert.That(_ambience.Current, Is.SameAs(_climaxProfile));
            Assert.That(PlaysOf(_tensionMusic), Is.EqualTo(1));
            Assert.That(FadeOutOf(music), Is.Null);
        }

        [Test]
        public void DifferentSoundSameTension_DoesNotPublish()
        {
            SetStates(
                new AudioState("TvOn", _unease, _uneaseProfile, null, TensionStage.Unease, 3f),
                new AudioState("Blackout", _high, _climaxProfile, null, TensionStage.Unease, 3f));
            _director.Start();
            _flags.Set(_unease);

            _flags.Set(_high);

            Assert.That(_ambience.Current, Is.SameAs(_climaxProfile));
            Assert.That(_tensionBroker.Published.Count, Is.EqualTo(1));
            Assert.That(_tensionBroker.Published[0].Current, Is.EqualTo(TensionStage.Unease));
        }

        [Test]
        public void ClearingFlag_ReturnsToPreviousState()
        {
            _director.Start();
            _flags.Set(_unease);
            _flags.Set(_high);
            AudioHandle music = PlayOf(_tensionMusic).Handle;

            _flags.Clear(_high);

            Assert.That(_director.Current.Tension, Is.EqualTo(TensionStage.Unease));
            Assert.That(FadeOutOf(music), Is.EqualTo(3f));
            Assert.That(_ambience.Current, Is.SameAs(_uneaseProfile));
            Assert.That(_tensionBroker.Published.Last().Previous, Is.EqualTo(TensionStage.Tension));
            Assert.That(_tensionBroker.Published.Last().Current, Is.EqualTo(TensionStage.Unease));
        }

        [Test]
        public void ClearingAll_ReturnsToDefaultWithDefaultCrossfade()
        {
            _director.Start();
            _flags.Set(_high);

            _flags.Clear(_high);

            Assert.That(_director.Current, Is.SameAs(_config.Default));
            Assert.That(_service.Plays.Last().Cue, Is.SameAs(_rain));
            Assert.That(_service.Plays.Last().FadeIn, Is.EqualTo(4f));
        }

        [Test]
        public void Dispose_FadesOutMusicAndUnsubscribes()
        {
            _director.Start();
            _flags.Set(_high);
            AudioHandle music = PlayOf(_tensionMusic).Handle;
            int plays = _service.Plays.Count;

            _director.Dispose();
            _flags.Set(_climax);

            Assert.That(FadeOutOf(music), Is.EqualTo(AudioDirector.ExitFadeOut));
            Assert.That(_service.Plays.Count, Is.EqualTo(plays));
            Assert.That(_flagBroker.SubscriberCount, Is.Zero);
        }

        private int PlaysOf(AudioCue cue)
        {
            return _service.Plays.Count(play => play.Cue == cue);
        }

        private FakeAudioService.PlayCall PlayOf(AudioCue cue)
        {
            return _service.Plays.Single(play => play.Cue == cue);
        }

        private float? FadeOutOf(AudioHandle handle)
        {
            foreach ((AudioHandle stopped, float fadeOut) in _service.Stops)
            {
                if (stopped.Equals(handle))
                {
                    return fadeOut;
                }
            }

            return null;
        }

        private void SetStates(params AudioState[] states)
        {
            SetField(_config, "states", states);
        }

        private static AmbienceProfile Profile(params AudioCue[] loops)
        {
            AmbienceProfile profile = ScriptableObject.CreateInstance<AmbienceProfile>();
            SetField(profile, "loops", loops);
            SetField(profile, "spots", new AmbienceSpot[0]);
            return profile;
        }

        private static void SetField(object target, string field, object value)
        {
            target.GetType().GetField(field, Private).SetValue(target, value);
        }
    }
}
