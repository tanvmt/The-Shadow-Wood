using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace TheShadowWood.Core.Scenes
{
    public interface ISceneLoader
    {
        bool IsLoading { get; }

        /// <summary>
        /// Loads the scene in Single mode. Returns false without loading when another load is running
        /// or the scene is not enabled in Build Settings.
        /// </summary>
        UniTask<bool> LoadAsync(SceneId sceneId, IProgress<float> progress = null, CancellationToken cancellationToken = default);
    }
}
