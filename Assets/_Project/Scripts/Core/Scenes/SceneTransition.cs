using System.Threading;
using Cysharp.Threading.Tasks;
using TheShadowWood.Core.UI;
using UnityEngine;

namespace TheShadowWood.Core.Scenes
{
    public sealed class SceneTransition : ISceneTransition
    {
        public const float FadeOutDuration = 0.6f;
        public const float FadeInDuration = 0.8f;

        private readonly ISceneLoader _loader;
        private readonly IScreenFader _fader;

        public SceneTransition(ISceneLoader loader, IScreenFader fader)
        {
            _loader = loader;
            _fader = fader;
        }

        public bool IsTransitioning { get; private set; }

        public async UniTask<bool> LoadAsync(SceneId sceneId, CancellationToken cancellationToken = default)
        {
            if (IsTransitioning || _loader.IsLoading)
            {
                Debug.LogWarning($"[SceneTransition] Ignored transition to {sceneId}: another transition is running.");
                return false;
            }

            IsTransitioning = true;
            bool completed = false;
            try
            {
                await _fader.FadeToAsync(1f, FadeOutDuration, cancellationToken);
                bool loaded = await _loader.LoadAsync(sceneId, null, cancellationToken);
                await _fader.FadeToAsync(0f, FadeInDuration, cancellationToken);
                completed = true;
                return loaded;
            }
            finally
            {
                IsTransitioning = false;

                // Never leave the player behind a black screen after a failure or cancellation.
                if (!completed)
                {
                    _fader.SetAlpha(0f);
                }
            }
        }
    }
}
