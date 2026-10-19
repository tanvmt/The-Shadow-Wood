using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheShadowWood.Core.Audio;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheShadowWood.Audio.Tests
{
    // isPlaying is not reliable in Edit Mode, so tests drive release through Stop and use looping cues.
    public sealed class AudioServiceTests
    {
        private TestAudio _audio;
        private FakeGameSettings _settings;
        private AudioService _service;
        private bool _savedListenerPause;

        [SetUp]
        public void SetUp()
        {
            _savedListenerPause = AudioListener.pause;
            _audio = new TestAudio();
            _settings = new FakeGameSettings();
            _service = new AudioService(_settings);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
            _audio.Dispose();
            AudioListener.pause = _savedListenerPause;
        }

        [Test]
        public void NullOrEmptyCue_ReturnsNone_AndCreatesNothing()
        {
            Assert.That(_service.Play(null).IsValid, Is.False);
            Assert.That(_service.PlayAt(_audio.EmptyCue(), Vector3.zero).IsValid, Is.False);
            Assert.That(_service.HasPoolObject, Is.False);
        }

        [Test]
        public void PlayAt_ConfiguresSource()
        {
            AudioClip clip = _audio.Clip();
            AudioCue cue = _audio.Cue(loop: true, clips: new[] { clip });

            AudioHandle handle = _service.PlayAt(cue, new Vector3(1f, 2f, 3f));
            AudioSource source = _service.FindSource(handle);

            Assert.That(handle.IsValid, Is.True);
            Assert.That(_service.IsPlaying(handle), Is.True);
            Assert.That(source.clip, Is.SameAs(clip));
            Assert.That(source.loop, Is.True);
            Assert.That(source.spatialBlend, Is.EqualTo(1f));
            Assert.That(source.dopplerLevel, Is.Zero);
            Assert.That(source.ignoreListenerPause, Is.False);
            Assert.That(source.transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)));
        }

        [Test]
        public void UiBus_IgnoresListenerPause()
        {
            AudioHandle handle = _service.Play(_audio.Cue(AudioBus.Ui, loop: true));

            Assert.That(_service.FindSource(handle).ignoreListenerPause, Is.True);
        }

        [Test]
        public void Volume_IsCueTimesScaleTimesBus()
        {
            _settings.MusicVolume = 0.5f;
            _settings.SfxVolume = 0.25f;

            AudioHandle music = _service.Play(_audio.Cue(AudioBus.Music, loop: true, volume: 0.8f), volumeScale: 0.5f);
            AudioHandle ambience = _service.Play(_audio.Cue(AudioBus.Ambience, loop: true));

            Assert.That(_service.FindSource(music).volume, Is.EqualTo(0.2f).Within(1e-4f));
            Assert.That(_service.FindSource(ambience).volume, Is.EqualTo(0.25f).Within(1e-4f));
        }

        [Test]
        public void SettingsChange_ReappliesVolumeToPlayingSources()
        {
            AudioHandle handle = _service.Play(_audio.Cue(AudioBus.Sfx, loop: true));

            _settings.SfxVolume = 0.3f;

            Assert.That(_service.FindSource(handle).volume, Is.EqualTo(0.3f).Within(1e-4f));
        }

        [Test]
        public void FadeIn_RampsVolumeOverTime()
        {
            AudioHandle handle = _service.Play(_audio.Cue(loop: true), fadeIn: 1f);
            AudioSource source = _service.FindSource(handle);

            Assert.That(source.volume, Is.Zero);

            _service.Tick(0.5f);
            Assert.That(source.volume, Is.EqualTo(0.5f).Within(1e-4f));

            _service.Tick(1f);
            Assert.That(source.volume, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Stop_WithoutFade_ReleasesImmediately()
        {
            AudioHandle handle = _service.Play(_audio.Cue(loop: true));

            _service.Stop(handle);

            Assert.That(_service.IsPlaying(handle), Is.False);
            Assert.That(_service.FindSource(handle), Is.Null);
        }

        [Test]
        public void Stop_WithFade_ReleasesWhenFadedOut()
        {
            AudioHandle handle = _service.Play(_audio.Cue(loop: true));
            AudioSource source = _service.FindSource(handle);

            _service.Stop(handle, fadeOut: 1f);
            _service.Tick(0.5f);

            Assert.That(_service.IsPlaying(handle), Is.True);
            Assert.That(source.volume, Is.EqualTo(0.5f).Within(1e-4f));

            _service.Tick(0.6f);
            Assert.That(_service.IsPlaying(handle), Is.False);
        }

        [Test]
        public void ListenerPaused_FreezesFadesExceptUiBus()
        {
            AudioHandle sfx = _service.Play(_audio.Cue(AudioBus.Sfx, loop: true), fadeIn: 1f);
            AudioHandle ui = _service.Play(_audio.Cue(AudioBus.Ui, loop: true), fadeIn: 1f);

            AudioListener.pause = true;
            _service.Tick(0.5f);

            Assert.That(_service.FindSource(sfx).volume, Is.Zero);
            Assert.That(_service.FindSource(ui).volume, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void StaleHandle_DoesNotStopReusedSource()
        {
            AudioCue cue = _audio.Cue(loop: true);
            AudioHandle first = _service.Play(cue);
            AudioSource firstSource = _service.FindSource(first);
            _service.Stop(first);

            AudioHandle second = _service.Play(cue);
            _service.Stop(first);

            Assert.That(_service.FindSource(second), Is.SameAs(firstSource));
            Assert.That(_service.IsPlaying(second), Is.True);
        }

        [Test]
        public void PlayAttached_FollowsTarget_ThenStaysWhenTargetIsDestroyed()
        {
            GameObject target = new GameObject("AudioTarget");
            AudioHandle handle = _service.PlayAttached(_audio.Cue(loop: true), target.transform);
            AudioSource source = _service.FindSource(handle);

            target.transform.position = new Vector3(5f, 0f, 0f);
            _service.Tick(0.1f);
            Assert.That(source.transform.position, Is.EqualTo(new Vector3(5f, 0f, 0f)));

            Object.DestroyImmediate(target);
            _service.Tick(0.1f);

            Assert.That(_service.IsPlaying(handle), Is.True);
            Assert.That(source.transform.position, Is.EqualTo(new Vector3(5f, 0f, 0f)));
        }

        [Test]
        public void FullPool_StealsOldestOneShot()
        {
            AudioCue cue = _audio.Cue();
            List<AudioHandle> handles = new List<AudioHandle>();
            for (int i = 0; i < AudioService.MaxVoices; i++)
            {
                handles.Add(_service.Play(cue));
            }

            AudioHandle extra = _service.Play(cue);

            Assert.That(extra.IsValid, Is.True);
            Assert.That(_service.IsPlaying(handles[0]), Is.False);
            Assert.That(_service.IsPlaying(handles[1]), Is.True);
        }

        [Test]
        public void FullPoolOfLoops_ReturnsNone()
        {
            AudioCue cue = _audio.Cue(loop: true);
            for (int i = 0; i < AudioService.MaxVoices; i++)
            {
                _service.Play(cue);
            }

            LogAssert.Expect(LogType.Warning, new Regex("no free source"));
            AudioHandle extra = _service.Play(cue);

            Assert.That(extra.IsValid, Is.False);
        }

        [Test]
        public void Dispose_DestroysPoolAndUnsubscribes()
        {
            _service.Play(_audio.Cue(loop: true));
            Assert.That(_service.HasPoolObject, Is.True);

            _service.Dispose();

            Assert.That(_service.HasPoolObject, Is.False);
            Assert.That(_settings.ListenerCount, Is.Zero);
        }
    }
}
