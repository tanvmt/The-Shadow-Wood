using NUnit.Framework;
using UnityEngine;

namespace TheShadowWood.Player.Tests
{
    public sealed class StaminaModelTests
    {
        private const float Dt = 0.1f;

        private PlayerMovementConfig _config;
        private StaminaModel _stamina;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            _stamina = new StaminaModel(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void StartsFull()
        {
            Assert.That(_stamina.Ratio, Is.EqualTo(1f));
            Assert.That(_stamina.CanSprint, Is.True);
        }

        [Test]
        public void Sprinting_Drains()
        {
            _stamina.Tick(isSprinting: true, Dt);

            float expected = _config.MaxStamina - _config.StaminaDrainPerSecond * Dt;
            Assert.That(_stamina.Current, Is.EqualTo(expected).Within(1e-4f));
        }

        [Test]
        public void Regen_WaitsForDelay()
        {
            _stamina.Tick(isSprinting: true, 1f);
            float afterSprint = _stamina.Current;

            _stamina.Tick(isSprinting: false, _config.StaminaRegenDelay * 0.5f);
            Assert.That(_stamina.Current, Is.EqualTo(afterSprint));

            _stamina.Tick(isSprinting: false, _config.StaminaRegenDelay);
            _stamina.Tick(isSprinting: false, Dt);
            Assert.That(_stamina.Current, Is.GreaterThan(afterSprint));
        }

        [Test]
        public void Exhaustion_BlocksSprint_UntilResumeThreshold()
        {
            DrainToZero();
            Assert.That(_stamina.IsExhausted, Is.True);
            Assert.That(_stamina.CanSprint, Is.False);

            _stamina.Tick(isSprinting: false, _config.StaminaRegenDelay + Dt);
            while (_stamina.Current < _config.StaminaToResumeSprint - 1e-3f)
            {
                Assert.That(_stamina.CanSprint, Is.False);
                _stamina.Tick(isSprinting: false, Dt);
            }

            _stamina.Tick(isSprinting: false, Dt);
            Assert.That(_stamina.CanSprint, Is.True);
        }

        [Test]
        public void NeverExceedsMax()
        {
            for (int i = 0; i < 1000; i++)
            {
                _stamina.Tick(isSprinting: false, Dt);
            }

            Assert.That(_stamina.Current, Is.EqualTo(_config.MaxStamina));
        }

        private void DrainToZero()
        {
            for (int i = 0; i < 10000 && _stamina.Current > 0f; i++)
            {
                _stamina.Tick(isSprinting: true, Dt);
            }
        }
    }
}
