using System;
using NUnit.Framework;
using TheShadowWood.Core.Pause;
using UnityEngine;

namespace TheShadowWood.Core.Tests
{
    public sealed class GamePauseTests
    {
        private float _savedTimeScale;
        private GamePause _pause;

        [SetUp]
        public void SetUp()
        {
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            _pause = new GamePause();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _savedTimeScale;
            AudioListener.pause = false;
        }

        [Test]
        public void Pause_StopsTimeAndAudio()
        {
            _pause.Pause();

            Assert.That(_pause.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(AudioListener.pause, Is.True);
        }

        [Test]
        public void LastHandle_ResumesToPreviousTimeScale()
        {
            Time.timeScale = 0.5f;
            IDisposable first = _pause.Pause();
            IDisposable second = _pause.Pause();

            first.Dispose();
            Assert.That(Time.timeScale, Is.EqualTo(0f), "Still held by the second handle.");

            second.Dispose();
            Assert.That(_pause.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(0.5f));
            Assert.That(AudioListener.pause, Is.False);
        }

        [Test]
        public void DisposingHandleTwice_ReleasesOnce()
        {
            IDisposable first = _pause.Pause();
            _pause.Pause();

            first.Dispose();
            first.Dispose();

            Assert.That(_pause.IsPaused, Is.True);
        }

        [Test]
        public void Dispose_ResumesEvenWithHandlesHeld()
        {
            _pause.Pause();

            _pause.Dispose();

            Assert.That(_pause.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }
    }
}
