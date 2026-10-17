using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TheShadowWood.Core.Scenes;
using TheShadowWood.Core.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheShadowWood.Bootstrap.Tests
{
    public sealed class SceneTransitionTests
    {
        private List<string> _log;
        private FakeFader _fader;
        private FakeLoader _loader;
        private SceneTransition _transition;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _fader = new FakeFader(_log);
            _loader = new FakeLoader(_log);
            _transition = new SceneTransition(_loader, _fader);
        }

        [Test]
        public async Task LoadAsync_FadesOutThenLoadsThenFadesIn()
        {
            bool result = await _transition.LoadAsync(SceneId.Gameplay);

            Assert.That(result, Is.True);
            Assert.That(_log, Is.EqualTo(new[] { "fade 1", "load Gameplay", "fade 0" }));
            Assert.That(_transition.IsTransitioning, Is.False);
        }

        [Test]
        public async Task LoadAsync_FadesBackIn_WhenLoadIsRefused()
        {
            _loader.Result = false;

            bool result = await _transition.LoadAsync(SceneId.Gameplay);

            Assert.That(result, Is.False);
            Assert.That(_fader.Alpha, Is.EqualTo(0f));
        }

        [Test]
        public void LoadAsync_ClearsScreen_WhenLoadThrows()
        {
            _loader.ThrowOnLoad = true;

            Assert.CatchAsync<InvalidOperationException>(async () => await _transition.LoadAsync(SceneId.Gameplay));

            Assert.That(_fader.Alpha, Is.EqualTo(0f));
            Assert.That(_transition.IsTransitioning, Is.False);
        }

        [Test]
        public async Task LoadAsync_IgnoresSecondCall_WhileTransitioning()
        {
            _loader.HoldUntilReleased();
            UniTask<bool> first = _transition.LoadAsync(SceneId.Gameplay);

            LogAssert.Expect(LogType.Warning, new Regex(".*another transition is running.*"));
            bool second = await _transition.LoadAsync(SceneId.Menu);

            _loader.Release();
            bool firstResult = await first;

            Assert.That(second, Is.False);
            Assert.That(firstResult, Is.True);
            Assert.That(_loader.Loaded, Is.EqualTo(new[] { SceneId.Gameplay }));
        }

        private sealed class FakeFader : IScreenFader
        {
            private readonly List<string> _log;

            public FakeFader(List<string> log)
            {
                _log = log;
            }

            public float Alpha { get; private set; }

            public void SetAlpha(float alpha)
            {
                Alpha = alpha;
            }

            public UniTask FadeToAsync(float alpha, float duration, CancellationToken cancellationToken = default)
            {
                _log.Add($"fade {alpha}");
                Alpha = alpha;
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeLoader : ISceneLoader
        {
            private readonly List<string> _log;
            private UniTaskCompletionSource _hold;

            public FakeLoader(List<string> log)
            {
                _log = log;
            }

            public bool Result { get; set; } = true;
            public bool ThrowOnLoad { get; set; }
            public bool IsLoading { get; private set; }
            public List<SceneId> Loaded { get; } = new List<SceneId>();

            public void HoldUntilReleased()
            {
                _hold = new UniTaskCompletionSource();
            }

            public void Release()
            {
                _hold?.TrySetResult();
            }

            public async UniTask<bool> LoadAsync(SceneId sceneId, IProgress<float> progress = null, CancellationToken cancellationToken = default)
            {
                if (ThrowOnLoad)
                {
                    throw new InvalidOperationException("Load failed.");
                }

                IsLoading = true;
                try
                {
                    if (_hold != null)
                    {
                        await _hold.Task;
                    }

                    _log.Add($"load {sceneId}");
                    Loaded.Add(sceneId);
                    return Result;
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }
    }
}
