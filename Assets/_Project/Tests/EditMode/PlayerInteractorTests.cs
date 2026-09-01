using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TheShadowWood.Interaction.Tests
{
    public sealed class PlayerInteractorTests
    {
        private GameObject _cameraObject;
        private GameObject _playerObject;
        private GameObject _targetObject;
        private GameObject _blockerObject;
        private PlayerInteractor _interactor;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("TestCamera");
            Camera camera = _cameraObject.AddComponent<Camera>();

            _playerObject = new GameObject("TestPlayer");
            _interactor = _playerObject.AddComponent<PlayerInteractor>();
            SetInteractionCamera(camera);

            _targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _targetObject.name = "TestInteractable";
            _targetObject.transform.position = new Vector3(0f, 0f, 2f);
            _targetObject.AddComponent<TestInteractableStub>();

            Physics.SyncTransforms();
        }

        private void SetInteractionCamera(Camera camera)
        {
            SerializedObject serializedInteractor = new SerializedObject(_interactor);
            serializedInteractor.FindProperty("interactionCamera").objectReferenceValue = camera;
            serializedInteractor.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_blockerObject);
            Object.DestroyImmediate(_targetObject);
            Object.DestroyImmediate(_playerObject);
            Object.DestroyImmediate(_cameraObject);
        }

        [Test]
        public void RefreshFocus_FocusesVisibleInteractableAtViewportCentre()
        {
            _interactor.RefreshFocus();

            Assert.That(_interactor.CurrentFocus.HasTarget, Is.True);
            Assert.That(_interactor.CurrentFocus.CanInteract, Is.True);
        }

        [Test]
        public void RefreshFocus_DoesNotSeeInteractableThroughBlockingGeometry()
        {
            _blockerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _blockerObject.name = "TestBlocker";
            _blockerObject.transform.position = new Vector3(0f, 0f, 1f);
            Physics.SyncTransforms();

            _interactor.RefreshFocus();

            Assert.That(_interactor.CurrentFocus.HasTarget, Is.False);
        }

        [Test]
        public void TryInteract_InvokesFocusedTargetOnce()
        {
            TestInteractableStub target = _targetObject.GetComponent<TestInteractableStub>();
            _interactor.RefreshFocus();

            InteractionResult result = _interactor.TryInteract();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(target.InteractionCount, Is.EqualTo(1));
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
