using System.Threading;
using Cysharp.Threading.Tasks;

namespace TheShadowWood.Core.Scenes
{
    public interface ISceneTransition
    {
        bool IsTransitioning { get; }

        // Fades out, loads, fades back in. Returns false when the load was refused; the screen still fades back in.
        UniTask<bool> LoadAsync(SceneId sceneId, CancellationToken cancellationToken = default);
    }
}
