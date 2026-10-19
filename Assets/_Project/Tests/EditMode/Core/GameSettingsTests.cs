using System.Collections.Generic;
using NUnit.Framework;
using TheShadowWood.Core.Settings;
using UnityEngine;

namespace TheShadowWood.Core.Tests
{
    public sealed class GameSettingsTests
    {
        private float _savedVolume;
        private FakeSettingsStore _store;

        [SetUp]
        public void SetUp()
        {
            _savedVolume = AudioListener.volume;
            _store = new FakeSettingsStore();
        }

        [TearDown]
        public void TearDown()
        {
            AudioListener.volume = _savedVolume;
        }

        [Test]
        public void NewStore_UsesDefaults()
        {
            GameSettings settings = new GameSettings(_store);

            Assert.That(settings.MasterVolume, Is.EqualTo(1f));
            Assert.That(settings.MusicVolume, Is.EqualTo(1f));
            Assert.That(settings.SfxVolume, Is.EqualTo(1f));
            Assert.That(settings.LookSensitivity, Is.EqualTo(1f));
        }

        [Test]
        public void StoredValues_AreLoadedClampedAndApplied()
        {
            _store.Values["settings.masterVolume"] = 0.4f;
            _store.Values["settings.musicVolume"] = 0.6f;
            _store.Values["settings.sfxVolume"] = -2f;
            _store.Values["settings.lookSensitivity"] = 99f;

            GameSettings settings = new GameSettings(_store);

            Assert.That(settings.MasterVolume, Is.EqualTo(0.4f));
            Assert.That(settings.MusicVolume, Is.EqualTo(0.6f));
            Assert.That(settings.SfxVolume, Is.EqualTo(0f));
            Assert.That(settings.LookSensitivity, Is.EqualTo(GameSettings.MaxLookSensitivity));
            Assert.That(AudioListener.volume, Is.EqualTo(0.4f).Within(1e-4f));
        }

        [Test]
        public void Setters_ClampAndRaiseChanged()
        {
            GameSettings settings = new GameSettings(_store);
            int changes = 0;
            settings.Changed += () => changes++;

            settings.MasterVolume = -1f;
            settings.MusicVolume = 5f;
            settings.SfxVolume = -5f;
            settings.LookSensitivity = 0f;

            Assert.That(settings.MasterVolume, Is.EqualTo(0f));
            Assert.That(settings.MusicVolume, Is.EqualTo(1f));
            Assert.That(settings.SfxVolume, Is.EqualTo(0f));
            Assert.That(settings.LookSensitivity, Is.EqualTo(GameSettings.MinLookSensitivity));
            Assert.That(changes, Is.EqualTo(3));
        }

        [Test]
        public void SettingSameValue_DoesNotRaiseChanged()
        {
            GameSettings settings = new GameSettings(_store);
            int changes = 0;
            settings.Changed += () => changes++;

            settings.MasterVolume = 1f;
            settings.MusicVolume = 1f;
            settings.SfxVolume = 1f;

            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void Changes_AreOnlyWrittenOnSave()
        {
            GameSettings settings = new GameSettings(_store);

            settings.LookSensitivity = 2f;
            settings.MusicVolume = 0.3f;
            settings.SfxVolume = 0.7f;
            Assert.That(_store.SaveCount, Is.Zero);
            Assert.That(_store.Values.ContainsKey("settings.lookSensitivity"), Is.False);

            settings.Save();
            Assert.That(_store.SaveCount, Is.EqualTo(1));
            Assert.That(_store.Values["settings.lookSensitivity"], Is.EqualTo(2f));
            Assert.That(_store.Values["settings.musicVolume"], Is.EqualTo(0.3f));
            Assert.That(_store.Values["settings.sfxVolume"], Is.EqualTo(0.7f));
        }

        private sealed class FakeSettingsStore : ISettingsStore
        {
            public Dictionary<string, float> Values { get; } = new Dictionary<string, float>();
            public int SaveCount { get; private set; }

            public float GetFloat(string key, float defaultValue)
            {
                return Values.TryGetValue(key, out float value) ? value : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                Values[key] = value;
            }

            public void Save()
            {
                SaveCount++;
            }
        }
    }
}
