using MessagePipe;
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
        }
    }
}
