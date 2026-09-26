using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TheShadowWood.Core.Scenes;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheShadowWood.Bootstrap.Tests
{
    /// <summary>
    /// Exercises SceneLoader against a fake ISceneBackend, so tests run without loading real scenes
    /// or depending on Build Settings.
    /// </summary>
    public sealed class SceneLoaderTests
    {
        [Test]
        public async Task LoadAsync_ReturnsTrue_WhenSceneCanBeLoaded()
        {
            FakeSceneBackend backend = new FakeSceneBackend { CanLoadResult = true };
            SceneLoader loader = new SceneLoader(backend);

            bool result = await loader.LoadAsync(SceneId.Menu);

            Assert.That(result, Is.True);
            Assert.That(backend.LoadedSceneNames, Is.EqualTo(new[] { "Menu" }));
        }

        [Test]
        public async Task LoadAsync_ReturnsFalse_WhenSceneIsNotInBuildSettings()
        {
            FakeSceneBackend backend = new FakeSceneBackend { CanLoadResult = false };
            SceneLoader loader = new SceneLoader(backend);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*not enabled in Build Settings.*"));
            bool result = await loader.LoadAsync(SceneId.Gameplay);

            Assert.That(result, Is.False);
            Assert.That(backend.LoadedSceneNames, Is.Empty);
        }

        [Test]
        public void IsLoading_IsFalse_BeforeAnyLoad()
        {
            SceneLoader loader = new SceneLoader(new FakeSceneBackend { CanLoadResult = true });

            Assert.That(loader.IsLoading, Is.False);
        }

        [Test]
        public async Task LoadAsync_IgnoresSecondCall_WhileFirstLoadIsInFlight()
        {
            FakeSceneBackend backend = new FakeSceneBackend { CanLoadResult = true };
            backend.HoldUntilReleased();
            SceneLoader loader = new SceneLoader(backend);

            UniTask<bool> firstLoad = loader.LoadAsync(SceneId.Menu);
            Assert.That(loader.IsLoading, Is.True);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*another scene is still loading.*"));
            bool secondResult = await loader.LoadAsync(SceneId.Gameplay);

            Assert.That(secondResult, Is.False);

            backend.Release();
            bool firstResult = await firstLoad;

            Assert.That(firstResult, Is.True);
            Assert.That(loader.IsLoading, Is.False);
            Assert.That(backend.LoadedSceneNames, Is.EqualTo(new[] { "Menu" }));
        }

        [Test]
        public void IsLoading_ResetsToFalse_WhenBackendThrows()
        {
            FakeSceneBackend backend = new FakeSceneBackend { CanLoadResult = true, ThrowOnLoad = true };
            SceneLoader loader = new SceneLoader(backend);

            Assert.CatchAsync<InvalidOperationException>(async () => await loader.LoadAsync(SceneId.Menu));
            Assert.That(loader.IsLoading, Is.False);
        }

        [Test]
        public void IsLoading_ResetsToFalse_WhenLoadIsCancelled()
        {
            FakeSceneBackend backend = new FakeSceneBackend { CanLoadResult = true };
            backend.HoldUntilReleased();
            SceneLoader loader = new SceneLoader(backend);

            using CancellationTokenSource cts = new CancellationTokenSource();
            UniTask<bool> load = loader.LoadAsync(SceneId.Menu, cancellationToken: cts.Token);
            cts.Cancel();

            Assert.CatchAsync<OperationCanceledException>(async () => await load);
            Assert.That(loader.IsLoading, Is.False);

            backend.Release();
        }

        private sealed class FakeSceneBackend : ISceneBackend
        {
            private System.Collections.Generic.List<string> _loadedSceneNames = new();
            private TaskCompletionSource<bool> _gate;

            public bool CanLoadResult { get; set; }
            public bool ThrowOnLoad { get; set; }
            public System.Collections.Generic.IReadOnlyList<string> LoadedSceneNames => _loadedSceneNames;

            public bool CanLoad(string sceneName)
            {
                return CanLoadResult;
            }

            public void HoldUntilReleased()
            {
                _gate = new TaskCompletionSource<bool>();
            }

            public void Release()
            {
                _gate?.TrySetResult(true);
            }

            public async UniTask LoadSingleAsync(string sceneName, IProgress<float> progress, CancellationToken cancellationToken)
            {
                if (ThrowOnLoad)
                {
                    throw new InvalidOperationException("Simulated load failure.");
                }

                if (_gate != null)
                {
                    await _gate.Task.AsUniTask().AttachExternalCancellation(cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                _loadedSceneNames.Add(sceneName);
            }
        }
    }
}
