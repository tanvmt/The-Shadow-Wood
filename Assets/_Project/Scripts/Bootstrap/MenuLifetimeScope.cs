using TheShadowWood.UI;
using VContainer;
using VContainer.Unity;

namespace TheShadowWood.Bootstrap
{
    public class MenuLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<UIScreenService>();
            builder.RegisterComponentInHierarchy<MainMenuView>();
            builder.RegisterComponentInHierarchy<SettingsScreen>();
            builder.RegisterEntryPoint<MainMenuPresenter>();
            builder.RegisterEntryPoint<SettingsPresenter>();
        }
    }
}
