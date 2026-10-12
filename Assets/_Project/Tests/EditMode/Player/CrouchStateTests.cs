using NUnit.Framework;
using UnityEngine;

namespace TheShadowWood.Player.Tests
{
    public sealed class CrouchStateTests
    {
        private const float Dt = 0.1f;

        private PlayerMovementConfig _config;
        private CrouchState _crouch;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            _crouch = new CrouchState(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void StartsStanding()
        {
            Assert.That(_crouch.IsCrouching, Is.False);
            Assert.That(_crouch.Height, Is.EqualTo(_config.StandingHeight));
        }

        [Test]
        public void Toggle_CrouchesAndLowersHeightGradually()
        {
            _crouch.Tick(togglePressed: true, canStand: true, Dt);

            Assert.That(_crouch.IsCrouching, Is.True);
            float expected = Mathf.Max(_config.CrouchHeight, _config.StandingHeight - _config.CrouchTransitionSpeed * Dt);
            Assert.That(_crouch.Height, Is.EqualTo(expected).Within(1e-4f));

            TickMany(togglePressed: false, canStand: true);
            Assert.That(_crouch.Height, Is.EqualTo(_config.CrouchHeight).Within(1e-4f));
        }

        [Test]
        public void LowCeiling_KeepsPlayerCrouched_UntilClear()
        {
            _crouch.Tick(togglePressed: true, canStand: true, Dt);
            TickMany(togglePressed: false, canStand: true);

            _crouch.Tick(togglePressed: true, canStand: false, Dt);
            Assert.That(_crouch.WantsCrouch, Is.False);
            Assert.That(_crouch.IsCrouching, Is.True);

            _crouch.Tick(togglePressed: false, canStand: true, Dt);
            Assert.That(_crouch.IsCrouching, Is.False);

            TickMany(togglePressed: false, canStand: true);
            Assert.That(_crouch.Height, Is.EqualTo(_config.StandingHeight).Within(1e-4f));
        }

        private void TickMany(bool togglePressed, bool canStand)
        {
            for (int i = 0; i < 100; i++)
            {
                _crouch.Tick(togglePressed, canStand, Dt);
            }
        }
    }
}
