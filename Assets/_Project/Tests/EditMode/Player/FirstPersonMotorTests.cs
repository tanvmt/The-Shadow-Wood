using NUnit.Framework;
using UnityEngine;

namespace TheShadowWood.Player.Tests
{
    public sealed class FirstPersonMotorTests
    {
        private const float Dt = 0.1f;

        private PlayerMovementConfig _config;
        private FirstPersonMotor _motor;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            _motor = new FirstPersonMotor(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Speed_RampsUpByAcceleration()
        {
            _motor.Tick(Vector3.forward, _config.WalkSpeed, grounded: true, Dt);

            float expected = Mathf.Min(_config.WalkSpeed, _config.Acceleration * Dt);
            Assert.That(_motor.Speed, Is.EqualTo(expected).Within(1e-4f));
        }

        [Test]
        public void Speed_SettlesAtMaxSpeed()
        {
            Vector3 velocity = Vector3.zero;
            for (int i = 0; i < 100; i++)
            {
                velocity = _motor.Tick(Vector3.forward, _config.WalkSpeed, grounded: true, Dt);
            }

            Assert.That(_motor.Speed, Is.EqualTo(_config.WalkSpeed).Within(1e-4f));
            Assert.That(velocity.z, Is.EqualTo(_config.WalkSpeed).Within(1e-4f));
        }

        [Test]
        public void PartialInput_ScalesTargetSpeed()
        {
            for (int i = 0; i < 100; i++)
            {
                _motor.Tick(Vector3.forward * 0.5f, _config.WalkSpeed, grounded: true, Dt);
            }

            Assert.That(_motor.Speed, Is.EqualTo(_config.WalkSpeed * 0.5f).Within(1e-4f));
        }

        [Test]
        public void ReleasingInput_DeceleratesInLastDirection()
        {
            for (int i = 0; i < 100; i++)
            {
                _motor.Tick(Vector3.right, _config.WalkSpeed, grounded: true, Dt);
            }

            Vector3 velocity = _motor.Tick(Vector3.zero, _config.WalkSpeed, grounded: true, Dt);

            float expected = Mathf.Max(0f, _config.WalkSpeed - _config.Acceleration * Dt);
            Assert.That(velocity.x, Is.EqualTo(expected).Within(1e-4f));
            Assert.That(velocity.z, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void Grounded_UsesStickVelocity()
        {
            _motor.Tick(Vector3.zero, _config.WalkSpeed, grounded: true, Dt);
            _motor.Tick(Vector3.zero, _config.WalkSpeed, grounded: true, Dt);

            Assert.That(_motor.VerticalVelocity, Is.EqualTo(_config.GroundedStickVelocity));
        }

        [Test]
        public void Falling_IsClampedToTerminalVelocity()
        {
            for (int i = 0; i < 1000; i++)
            {
                _motor.Tick(Vector3.zero, _config.WalkSpeed, grounded: false, Dt);
            }

            Assert.That(_motor.VerticalVelocity, Is.EqualTo(-_config.TerminalVelocity).Within(1e-4f));
        }
    }
}
