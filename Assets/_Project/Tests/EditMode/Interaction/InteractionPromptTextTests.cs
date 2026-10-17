using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TheShadowWood.Interaction.Tests
{
    public sealed class InteractionPromptTextTests
    {
        private GameObject _targetObject;
        private TestInteractableStub _target;

        [SetUp]
        public void SetUp()
        {
            _targetObject = new GameObject("TestInteractable");
            _target = _targetObject.AddComponent<TestInteractableStub>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_targetObject);
        }

        [Test]
        public void Build_NoTarget_ReturnsNull()
        {
            Assert.That(InteractionPromptText.Build(InteractionFocusState.None, "E"), Is.Null);
        }

        [Test]
        public void Build_Available_PrefixesKey()
        {
            SetPrompt("Xem TV", blockedReason: "");

            string text = InteractionPromptText.Build(new InteractionFocusState(_target, true), "E");

            Assert.That(text, Is.EqualTo("[E] Xem TV"));
        }

        [Test]
        public void Build_Blocked_ShowsReasonWithoutKey()
        {
            SetPrompt("Mở cửa", blockedReason: "Cửa bị khoá");

            string text = InteractionPromptText.Build(new InteractionFocusState(_target, false), "E");

            Assert.That(text, Is.EqualTo("Cửa bị khoá"));
        }

        [Test]
        public void Build_BlockedWithoutReason_ReturnsNull()
        {
            SetPrompt("Mở cửa", blockedReason: "");

            Assert.That(InteractionPromptText.Build(new InteractionFocusState(_target, false), "E"), Is.Null);
        }

        [Test]
        public void Build_EmptyPrompt_ReturnsNull()
        {
            SetPrompt("", blockedReason: "");

            Assert.That(InteractionPromptText.Build(new InteractionFocusState(_target, true), "E"), Is.Null);
        }

        private void SetPrompt(string prompt, string blockedReason)
        {
            SerializedObject serialized = new SerializedObject(_target);
            serialized.FindProperty("prompt").stringValue = prompt;
            serialized.FindProperty("blockedReason").stringValue = blockedReason;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
