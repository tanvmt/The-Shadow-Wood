using NUnit.Framework;
using UnityEngine;

namespace TheShadowWood.Player.Tests
{
    public sealed class HeadBobTests
    {
        private const float Dt = 0.02f;

        private PlayerMovementConfig _config;
        private HeadBob _bob;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            _bob = new HeadBob(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Idle_HasNoOffset()
        {
            _bob.Tick(horizontalSpeed: 0f, grounded: true, _config.WalkBobFrequency, Dt);

            Assert.That(_bob.Offset, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Walking_StaysWithinAmplitude()
        {
            bool moved = false;
            for (int i = 0; i < 200; i++)
            {
                Vector2 offset = _bob.Tick(_config.WalkSpeed, grounded: true, _config.WalkBobFrequency, Dt);
                moved |= offset != Vector2.zero;

                Assert.That(Mathf.Abs(offset.x), Is.LessThanOrEqualTo(_config.BobHorizontalAmplitude + 1e-5f));
                Assert.That(Mathf.Abs(offset.y), Is.LessThanOrEqualTo(_config.BobVerticalAmplitude + 1e-5f));
            }

            Assert.That(moved, Is.True);
        }

        [Test]
        public void Airborne_ReturnsToZero()
        {
            for (int i = 0; i < 37; i++)
            {
                _bob.Tick(_config.WalkSpeed, grounded: true, _config.WalkBobFrequency, Dt);
            }

            for (int i = 0; i < 500; i++)
            {
                _bob.Tick(_config.WalkSpeed, grounded: false, _config.WalkBobFrequency, Dt);
            }

            Assert.That(_bob.Offset.magnitude, Is.LessThan(1e-4f));
        }

        [Test]
        public void Walking_StepsAtBobFrequency()
        {
            int steps = CountSteps(_config.WalkSpeed, grounded: true, _config.WalkBobFrequency, seconds: 5f);

            float expected = _config.WalkBobFrequency * 5f;
            Assert.That(steps, Is.InRange(Mathf.FloorToInt(expected) - 1, Mathf.CeilToInt(expected) + 1));
        }

        [Test]
        public void Sprinting_StepsMoreOftenThanWalking()
        {
            int walk = CountSteps(_config.WalkSpeed, grounded: true, _config.WalkBobFrequency, seconds: 5f);
            _bob = new HeadBob(_config);
            int sprint = CountSteps(_config.SprintSpeed, grounded: true, _config.SprintBobFrequency, seconds: 5f);

            Assert.That(sprint, Is.GreaterThan(walk));
        }

        [Test]
        public void Idle_NeverSteps()
        {
            int steps = CountSteps(horizontalSpeed: 0f, grounded: true, _config.WalkBobFrequency, seconds: 5f);

            Assert.That(steps, Is.Zero);
        }

        [Test]
        public void Airborne_NeverSteps()
        {
            int steps = CountSteps(_config.WalkSpeed, grounded: false, _config.WalkBobFrequency, seconds: 5f);

            Assert.That(steps, Is.Zero);
        }

        [Test]
        public void Steps_AreNeverOnConsecutiveTicks()
        {
            bool previous = false;
            for (int i = 0; i < 500; i++)
            {
                _bob.Tick(_config.SprintSpeed, grounded: true, _config.SprintBobFrequency, Dt);

                Assert.That(previous && _bob.Stepped, Is.False);
                previous = _bob.Stepped;
            }
        }

        [Test]
        public void Stepped_ResetsWhenMovementStops()
        {
            for (int i = 0; i < 500 && !_bob.Stepped; i++)
            {
                _bob.Tick(_config.WalkSpeed, grounded: true, _config.WalkBobFrequency, Dt);
            }

            Assert.That(_bob.Stepped, Is.True);

            _bob.Tick(horizontalSpeed: 0f, grounded: true, _config.WalkBobFrequency, Dt);
            Assert.That(_bob.Stepped, Is.False);
        }

        private int CountSteps(float horizontalSpeed, bool grounded, float frequency, float seconds)
        {
            int steps = 0;
            int ticks = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < ticks; i++)
            {
                _bob.Tick(horizontalSpeed, grounded, frequency, Dt);
                if (_bob.Stepped)
                {
                    steps++;
                }
            }

            return steps;
        }
    }
}
