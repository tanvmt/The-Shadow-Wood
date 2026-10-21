using System.Reflection;
using NUnit.Framework;
using TheShadowWood.Core.Audio;
using UnityEngine;

namespace TheShadowWood.Player.Tests
{
    public sealed class FootstepConfigTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private FootstepConfig _config;
        private AudioCue _default;
        private AudioCue _wood;
        private PhysicsMaterial _woodMaterial;
        private PhysicsMaterial _stoneMaterial;

        [SetUp]
        public void SetUp()
        {
            _default = ScriptableObject.CreateInstance<AudioCue>();
            _wood = ScriptableObject.CreateInstance<AudioCue>();
            _woodMaterial = new PhysicsMaterial("Wood");
            _stoneMaterial = new PhysicsMaterial("Stone");

            _config = ScriptableObject.CreateInstance<FootstepConfig>();
            SetField("defaultCue", _default);
            SetField("surfaces", new[] { new FootstepSurface(_woodMaterial, _wood) });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_default);
            Object.DestroyImmediate(_wood);
            Object.DestroyImmediate(_woodMaterial);
            Object.DestroyImmediate(_stoneMaterial);
        }

        [Test]
        public void ListedMaterial_UsesItsCue()
        {
            Assert.That(_config.CueFor(_woodMaterial), Is.SameAs(_wood));
        }

        [Test]
        public void UnknownOrMissingMaterial_UsesDefaultCue()
        {
            Assert.That(_config.CueFor(_stoneMaterial), Is.SameAs(_default));
            Assert.That(_config.CueFor(null), Is.SameAs(_default));
        }

        [Test]
        public void SurfaceWithoutCue_FallsBackToDefault()
        {
            SetField("surfaces", new[] { new FootstepSurface(_woodMaterial, null) });

            Assert.That(_config.CueFor(_woodMaterial), Is.SameAs(_default));
        }

        [Test]
        public void Volume_GrowsFromCrouchToWalkToSprint()
        {
            float crouch = _config.VolumeFor(crouching: true, sprinting: false);
            float walk = _config.VolumeFor(crouching: false, sprinting: false);
            float sprint = _config.VolumeFor(crouching: false, sprinting: true);

            Assert.That(crouch, Is.LessThan(walk));
            Assert.That(walk, Is.LessThan(sprint));
        }

        private void SetField(string field, object value)
        {
            typeof(FootstepConfig).GetField(field, Private).SetValue(_config, value);
        }
    }
}
