using System.Threading;
using Cysharp.Threading.Tasks;
using TheShadowWood.Core.Scenes;
using TheShadowWood.Core.UI;
using VContainer.Unity;

namespace TheShadowWood.Bootstrap
{
    public sealed class BootstrapFlow : IAsyncStartable
    {
        private readonly ISceneTransition _transition;
        private readonly IScreenFader _fader;

        public BootstrapFlow(ISceneTransition transition, IScreenFader fader)
        {
            _transition = transition;
            _fader = fader;
        }

        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            // Start black so Menu fades in. No token: this scope is destroyed by the load itself.
            _fader.SetAlpha(1f);
            await _transition.LoadAsync(SceneId.Menu);
        }
    }
}
