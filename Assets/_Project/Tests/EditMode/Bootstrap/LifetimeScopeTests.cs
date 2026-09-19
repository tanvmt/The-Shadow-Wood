using System;
using System.Reflection;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TheShadowWood.Bootstrap.Tests
{
    /// <summary>
    /// Builds the real scope configurations without entering Play Mode:
    /// each scope's Configure is invoked on a plain ContainerBuilder, mirroring Root -> Gameplay.
    /// </summary>
    public sealed class LifetimeScopeTests
    {
        private static readonly MethodInfo ConfigureMethod =
            typeof(LifetimeScope).GetMethod("Configure", BindingFlags.Instance | BindingFlags.NonPublic);

        private GameObject _rootObject;
        private GameObject _gameplayObject;
        private IObjectResolver _rootContainer;
        private IScopedObjectResolver _gameplayContainer;

        [SetUp]
        public void SetUp()
        {
            // LifetimeScope.Awake does not run in Edit Mode, so AddComponent does not auto-build a container.
            _rootObject = new GameObject("TestRootLifetimeScope");
            RootLifetimeScope rootScope = _rootObject.AddComponent<RootLifetimeScope>();

            _gameplayObject = new GameObject("TestGameplayLifetimeScope");
            GameplayLifetimeScope gameplayScope = _gameplayObject.AddComponent<GameplayLifetimeScope>();

            ContainerBuilder rootBuilder = new ContainerBuilder();
            Configure(rootScope, rootBuilder);
            _rootContainer = rootBuilder.Build();

            _gameplayContainer = _rootContainer.CreateScope(builder => Configure(gameplayScope, builder));
        }

        [TearDown]
        public void TearDown()
        {
            _gameplayContainer?.Dispose();
            _rootContainer?.Dispose();
            UnityEngine.Object.DestroyImmediate(_gameplayObject);
            UnityEngine.Object.DestroyImmediate(_rootObject);
        }

        [Test]
        public void Root_ResolvesMessagePipe_AndDeliversMessages()
        {
            AssertDelivers(_rootContainer);
        }

        [Test]
        public void Root_InitializesGlobalMessagePipe()
        {
            Assert.That(GlobalMessagePipe.IsInitialized, Is.True);
        }

        [Test]
        public void Gameplay_InheritsMessagePipeFromRoot()
        {
            AssertDelivers(_gameplayContainer);
        }

        [Test]
        public void Gameplay_CanRegisterSceneScopedBroker_WithRootOptions()
        {
            MessagePipeOptions options = _gameplayContainer.Resolve<MessagePipeOptions>();
            using IScopedObjectResolver sceneScope =
                _gameplayContainer.CreateScope(builder => builder.RegisterMessageBroker<SceneScopedMessage>(options));

            SceneScopedMessage received = default;
            using IDisposable subscription = sceneScope.Resolve<ISubscriber<SceneScopedMessage>>()
                .Subscribe(message => received = message);

            sceneScope.Resolve<IPublisher<SceneScopedMessage>>().Publish(new SceneScopedMessage(7));

            Assert.That(received.Value, Is.EqualTo(7));
        }

        private static void AssertDelivers(IObjectResolver resolver)
        {
            TestMessage received = default;
            using IDisposable subscription = resolver.Resolve<ISubscriber<TestMessage>>()
                .Subscribe(message => received = message);

            resolver.Resolve<IPublisher<TestMessage>>().Publish(new TestMessage(42));

            Assert.That(received.Value, Is.EqualTo(42));
        }

        private static void Configure(LifetimeScope scope, IContainerBuilder builder)
        {
            ConfigureMethod.Invoke(scope, new object[] { builder });
        }

        private readonly struct TestMessage
        {
            public int Value { get; }

            public TestMessage(int value)
            {
                Value = value;
            }
        }

        private readonly struct SceneScopedMessage
        {
            public int Value { get; }

            public SceneScopedMessage(int value)
            {
                Value = value;
            }
        }
    }
}
