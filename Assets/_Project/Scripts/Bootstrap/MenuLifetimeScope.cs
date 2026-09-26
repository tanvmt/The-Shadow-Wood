using TheShadowWood.UI;
using VContainer;
using VContainer.Unity;

namespace TheShadowWood.Bootstrap
{
    public class MenuLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<MainMenuView>();
            builder.RegisterEntryPoint<MainMenuPresenter>();
        }
    }
}
