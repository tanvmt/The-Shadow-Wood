using System.Reflection;
using NUnit.Framework;
using TheShadowWood.Core.Audio;
using UnityEngine;

namespace TheShadowWood.Audio.Tests
{
    public sealed class AmbiencePlayerTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private TestAudio _audio;
        private FakeAudioService _service;
        private GameObject _cameraObject;
        private AmbienceProfile _calm;
        private AmbienceProfile _tense;
        private AudioCue _rain;
        private AudioCue _wind;
        private AudioCue _frog;
        private AmbiencePlayer _player;

        [SetUp]
        public void SetUp()
        {
            _audio = new TestAudio();
            _service = new FakeAudioService();

            _cameraObject = new GameObject("Listener", typeof(Camera));
            _cameraObject.transform.position = new Vector3(10f, 2f, -4f);

            _rain = _audio.Cue(AudioBus.Ambience, loop: true);
            _wind = _audio.Cue(AudioBus.Ambience, loop: true);
            _frog = _audio.Cue(AudioBus.Ambience);

            _calm = Profile(new[] { _rain }, new AmbienceSpot(_frog, 1f, 1f, 6f, 15f, -1.6f, 0f));
            _tense = Profile(new[] { _wind });

            _player = new AmbiencePlayer(_service, _cameraObject.GetComponent<Camera>());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cameraObject);
            Object.DestroyImmediate(_calm);
            Object.DestroyImmediate(_tense);
            _audio.Dispose();
        }

        [Test]
        public void SetProfile_PlaysLoopsWithFadeIn()
        {
            _player.SetProfile(_calm, 2f);

            Assert.That(_player.Current, Is.SameAs(_calm));
            Assert.That(_service.Plays.Count, Is.EqualTo(1));
            Assert.That(_service.Plays[0].Cue, Is.SameAs(_rain));
            Assert.That(_service.Plays[0].FadeIn, Is.EqualTo(2f));
        }

        [Test]
        public void SetProfile_SameProfile_DoesNothing()
        {
            _player.SetProfile(_calm, 2f);

            _player.SetProfile(_calm, 1f);

            Assert.That(_service.Plays.Count, Is.EqualTo(1));
            Assert.That(_service.Stops, Is.Empty);
        }

        [Test]
        public void SetProfile_Other_CrossfadesLoops()
        {
            _player.SetProfile(_calm, 2f);
            AudioHandle rain = _service.Plays[0].Handle;

            _player.SetProfile(_tense, 3f);

            Assert.That(_player.Current, Is.SameAs(_tense));
            Assert.That(_service.Stops, Is.EqualTo(new[] { (rain, 3f) }));
            Assert.That(_service.Plays[1].Cue, Is.SameAs(_wind));
            Assert.That(_service.Plays[1].FadeIn, Is.EqualTo(3f));
        }

        [Test]
        public void Tick_PlaysDueSpotsAroundListener()
        {
            _player.SetProfile(_calm, 2f);

            _player.Tick(1f);

            Assert.That(_service.Plays.Count, Is.EqualTo(2));
            FakeAudioService.PlayCall frog = _service.Plays[1];
            Vector3 offset = frog.Position.Value - _cameraObject.transform.position;
            float horizontal = new Vector2(offset.x, offset.z).magnitude;

            Assert.That(frog.Cue, Is.SameAs(_frog));
            Assert.That(horizontal, Is.InRange(6f - 1e-3f, 15f + 1e-3f));
            Assert.That(offset.y, Is.InRange(-1.6f - 1e-3f, 1e-3f));
        }

        [Test]
        public void NullProfile_StopsLoopsAndSpots()
        {
            _player.SetProfile(_calm, 2f);

            _player.SetProfile(null, 1f);
            _player.Tick(10f);

            Assert.That(_player.Current, Is.Null);
            Assert.That(_service.Stops.Count, Is.EqualTo(1));
            Assert.That(_service.Plays.Count, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_FadesOutLoopsAndStopsSpots()
        {
            _player.SetProfile(_calm, 2f);
            AudioHandle rain = _service.Plays[0].Handle;

            _player.Dispose();
            _player.Tick(10f);

            Assert.That(_service.Stops, Is.EqualTo(new[] { (rain, AmbiencePlayer.ExitFadeOut) }));
            Assert.That(_service.Plays.Count, Is.EqualTo(1));
        }

        private static AmbienceProfile Profile(AudioCue[] loops, params AmbienceSpot[] spots)
        {
            AmbienceProfile profile = ScriptableObject.CreateInstance<AmbienceProfile>();
            SetField(profile, "loops", loops);
            SetField(profile, "spots", spots);
            return profile;
        }

        private static void SetField(object target, string field, object value)
        {
            target.GetType().GetField(field, Private).SetValue(target, value);
        }
    }
}
