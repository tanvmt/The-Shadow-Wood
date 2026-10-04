using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TheShadowWood.Player.Tests
{
    public sealed class FirstPersonLookTests
    {
        private FirstPersonLookConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<FirstPersonLookConfig>();
            Configure(mouse: 0.1f, gamepad: 180f, invertY: false, minPitch: -80f, maxPitch: 80f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void MouseDelta_IsNotScaledByDeltaTime()
        {
            FirstPersonLook look = new FirstPersonLook(_config);

            float yawAtSlowFrame = look.Tick(new Vector2(100f, 0f), isPointerDelta: true, deltaTime: 0.1f);
            float yawAtFastFrame = look.Tick(new Vector2(100f, 0f), isPointerDelta: true, deltaTime: 0.001f);

            Assert.That(yawAtSlowFrame, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(yawAtFastFrame, Is.EqualTo(10f).Within(1e-4f));
        }

        [Test]
        public void StickValue_IsScaledByDeltaTime()
        {
            FirstPersonLook look = new FirstPersonLook(_config);

            float yaw = look.Tick(new Vector2(1f, 0f), isPointerDelta: false, deltaTime: 0.5f);

            Assert.That(yaw, Is.EqualTo(90f).Within(1e-4f));
        }

        [Test]
        public void MovingUp_LooksUp()
        {
            FirstPersonLook look = new FirstPersonLook(_config);

            look.Tick(new Vector2(0f, 100f), isPointerDelta: true, deltaTime: 0.016f);

            Assert.That(look.Pitch, Is.EqualTo(-10f).Within(1e-4f), "Negative pitch means looking up.");
        }

        [Test]
        public void InvertY_FlipsPitchDirection()
        {
            Configure(mouse: 0.1f, gamepad: 180f, invertY: true, minPitch: -80f, maxPitch: 80f);
            FirstPersonLook look = new FirstPersonLook(_config);

            look.Tick(new Vector2(0f, 100f), isPointerDelta: true, deltaTime: 0.016f);

            Assert.That(look.Pitch, Is.EqualTo(10f).Within(1e-4f));
        }

        [Test]
        public void Pitch_IsClampedToConfiguredRange()
        {
            FirstPersonLook look = new FirstPersonLook(_config);

            look.Tick(new Vector2(0f, 10000f), isPointerDelta: true, deltaTime: 0.016f);
            Assert.That(look.Pitch, Is.EqualTo(-80f));

            look.Tick(new Vector2(0f, -10000f), isPointerDelta: true, deltaTime: 0.016f);
            Assert.That(look.Pitch, Is.EqualTo(80f));
        }

        [Test]
        public void SetPitch_Clamps()
        {
            FirstPersonLook look = new FirstPersonLook(_config);

            look.SetPitch(200f);

            Assert.That(look.Pitch, Is.EqualTo(80f));
        }

        private void Configure(float mouse, float gamepad, bool invertY, float minPitch, float maxPitch)
        {
            SerializedObject serialized = new SerializedObject(_config);
            serialized.FindProperty("mouseSensitivity").floatValue = mouse;
            serialized.FindProperty("gamepadSensitivity").floatValue = gamepad;
            serialized.FindProperty("invertY").boolValue = invertY;
            serialized.FindProperty("minPitch").floatValue = minPitch;
            serialized.FindProperty("maxPitch").floatValue = maxPitch;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
