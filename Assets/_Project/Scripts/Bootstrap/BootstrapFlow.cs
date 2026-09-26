using System.Threading;
using Cysharp.Threading.Tasks;
using TheShadowWood.Core.Scenes;
using VContainer.Unity;

namespace TheShadowWood.Bootstrap
{
    public sealed class BootstrapFlow : IAsyncStartable
    {
        private readonly ISceneLoader _sceneLoader;

        public BootstrapFlow(ISceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            await _sceneLoader.LoadAsync(SceneId.Menu);
        }
    }
}
