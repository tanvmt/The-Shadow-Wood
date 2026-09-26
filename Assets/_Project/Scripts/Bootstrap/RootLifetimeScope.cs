using MessagePipe;
using TheShadowWood.Core.Scenes;
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
        }
    }
}
