using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TheShadowWood.Core.Scenes
{
    public sealed class SceneLoader : ISceneLoader
    {
        private readonly ISceneBackend _backend;

        public bool IsLoading { get; private set; }

        public SceneLoader(ISceneBackend backend)
        {
            _backend = backend;
        }

        public async UniTask<bool> LoadAsync(SceneId sceneId, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] Ignored load of {sceneId}: another scene is still loading.");
                return false;
            }

            string sceneName = SceneCatalog.GetSceneName(sceneId);
            if (!_backend.CanLoad(sceneName))
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' is not enabled in Build Settings.");
                return false;
            }

            IsLoading = true;
            try
            {
                await _backend.LoadSingleAsync(sceneName, progress, cancellationToken);
                return true;
            }
            finally
            {
                // Reset on success, failure and cancellation alike.
                IsLoading = false;
            }
        }
    }
}
