using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace TheShadowWood.Core.Scenes
{
    /// <summary>
    /// Thin wrapper over Unity's SceneManager so SceneLoader can be tested without loading real scenes.
    /// </summary>
    public interface ISceneBackend
    {
        bool CanLoad(string sceneName);
        UniTask LoadSingleAsync(string sceneName, IProgress<float> progress, CancellationToken cancellationToken);
    }
}
