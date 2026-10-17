using MessagePipe;
using TheShadowWood.Core.Input;
using TheShadowWood.Core.Scenes;
using TheShadowWood.Core.UI;
using TheShadowWood.UI;
using VContainer;
using VContainer.Unity;

namespace TheShadowWood.Bootstrap
{
    public class RootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterMessagePipe();
            builder.RegisterBuildCallback(container => GlobalMessagePipe.SetProvider(container.AsServiceProvider()));

            builder.Register<UnitySceneBackend>(Lifetime.Singleton).As<ISceneBackend>();
            builder.Register<SceneLoader>(Lifetime.Singleton).As<ISceneLoader>();
            builder.Register<ScreenFader>(Lifetime.Singleton).As<IScreenFader>();
            builder.Register<SceneTransition>(Lifetime.Singleton).As<ISceneTransition>();

            builder.Register<InputService>(Lifetime.Singleton).As<IInputService>();
        }
    }
}
