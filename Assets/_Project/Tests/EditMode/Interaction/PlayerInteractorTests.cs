using System.Collections.Generic;
using NUnit.Framework;
using TheShadowWood.Core.Input;
using UnityEditor;
using UnityEngine;

namespace TheShadowWood.Interaction.Tests
{
    public sealed class PlayerInteractorTests
    {
        private readonly List<InteractionFocusState> _focusEvents = new List<InteractionFocusState>();

        private GameObject _cameraObject;
        private GameObject _playerObject;
        private GameObject _targetObject;
        private GameObject _blockerObject;
        private InteractionConfig _config;
        private FakeInputService _input;
        private TestInteractableStub _target;
        private PlayerInteractor _interactor;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("TestCamera");
            Camera camera = _cameraObject.AddComponent<Camera>();

            _config = ScriptableObject.CreateInstance<InteractionConfig>();
            _playerObject = new GameObject("TestPlayer");
            PlayerInteractorView view = _playerObject.AddComponent<PlayerInteractorView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("config").objectReferenceValue = _config;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            _targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _targetObject.name = "TestInteractable";
            _targetObject.transform.position = new Vector3(0f, 0f, 2f);
            _target = _targetObject.AddComponent<TestInteractableStub>();

            Physics.SyncTransforms();

            _input = new FakeInputService();
            _interactor = new PlayerInteractor(view, camera, _input);
            _focusEvents.Clear();
            _interactor.FocusChanged += _focusEvents.Add;
        }

        [TearDown]
        public void TearDown()
        {
            _interactor.Dispose();
            Object.DestroyImmediate(_blockerObject);
            Object.DestroyImmediate(_targetObject);
            Object.DestroyImmediate(_playerObject);
            Object.DestroyImmediate(_cameraObject);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Tick_FocusesVisibleInteractableAtViewportCentre()
        {
            _interactor.Tick();

            Assert.That(_interactor.CurrentFocus.Target, Is.SameAs(_target));
            Assert.That(_interactor.CurrentFocus.CanInteract, Is.True);
            Assert.That(_target.IsFocused, Is.True);
        }

        [Test]
        public void Tick_DoesNotSeeInteractableThroughBlockingGeometry()
        {
            _blockerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _blockerObject.name = "TestBlocker";
            _blockerObject.transform.position = new Vector3(0f, 0f, 1f);
            Physics.SyncTransforms();

            _interactor.Tick();

            Assert.That(_interactor.CurrentFocus.HasTarget, Is.False);
        }

        [Test]
        public void Tick_InteractPressed_InvokesFocusedTargetOnce()
        {
            InteractionResult? completed = null;
            _interactor.InteractionCompleted += result => completed = result;
            _input.InteractPressed = true;

            _interactor.Tick();

            Assert.That(_target.InteractionCount, Is.EqualTo(1));
            Assert.That(completed?.Succeeded, Is.True);
        }

        [Test]
        public void Tick_OutsideGameplay_DoesNotFocusOrInteract()
        {
            _input.SetContext(InputContext.Focus);
            _input.InteractPressed = true;

            _interactor.Tick();

            Assert.That(_interactor.CurrentFocus.HasTarget, Is.False);
            Assert.That(_target.InteractionCount, Is.Zero);
        }

        [Test]
        public void ContextChange_AwayFromGameplay_ClearsFocus()
        {
            _interactor.Tick();
            _focusEvents.Clear();

            _input.SetContext(InputContext.UI);

            Assert.That(_interactor.CurrentFocus.HasTarget, Is.False);
            Assert.That(_target.IsFocused, Is.False);
            Assert.That(_focusEvents, Has.Count.EqualTo(1));
        }

        [Test]
        public void Tick_FocusedTargetDestroyed_ClearsFocus()
        {
            _interactor.Tick();
            _focusEvents.Clear();

            Object.DestroyImmediate(_targetObject);
            Physics.SyncTransforms();
            _interactor.Tick();

            Assert.That(_interactor.CurrentFocus.HasTarget, Is.False);
            Assert.That(_focusEvents, Has.Count.EqualTo(1));
            Assert.That(_focusEvents[0].HasTarget, Is.False);
        }
    }

    public sealed class TestInteractableStub : InteractableBehaviour
    {
        public int InteractionCount { get; private set; }

        protected override InteractionResult PerformInteraction(InteractionContext context)
        {
            InteractionCount++;
            return InteractionResult.Success();
        }
    }
}
