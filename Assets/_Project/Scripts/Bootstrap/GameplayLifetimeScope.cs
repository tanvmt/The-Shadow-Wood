using MessagePipe;
using TheShadowWood.Core.Cameras;
using TheShadowWood.Core.Flags;
using TheShadowWood.Core.Items;
using TheShadowWood.Core.Pause;
using TheShadowWood.Interaction;
using TheShadowWood.Inventory;
using TheShadowWood.Player;
using TheShadowWood.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TheShadowWood.Bootstrap
{
    public class GameplayLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Progress messages die with the scene, so stale subscribers cannot leak into the next run.
            MessagePipeOptions options = Parent.Container.Resolve<MessagePipeOptions>();
            builder.RegisterMessageBroker<FlagChangedMessage>(options);
            builder.Register<FlagService>(Lifetime.Singleton).As<IFlagService>();
            builder.RegisterMessageBroker<ItemAddedMessage>(options);
            builder.RegisterMessageBroker<ItemRemovedMessage>(options);
            builder.Register<InventoryService>(Lifetime.Singleton).As<IInventoryService>();

            // Scene needs one Player and one CameraRig.
            builder.RegisterComponentInHierarchy<PlayerView>();
            builder.RegisterComponentInHierarchy<PlayerInteractorView>();
            builder.RegisterComponentInHierarchy<CameraRig>();
            builder.Register<Camera>(r => r.Resolve<CameraRig>().OutputCamera, Lifetime.Singleton);
            builder.Register<CameraFocusService>(Lifetime.Singleton).As<ICameraFocusService>();
            builder.Register<CameraShakeService>(Lifetime.Singleton).As<ICameraShakeService>();
            builder.RegisterEntryPoint<CameraRigBinder>();
            builder.RegisterEntryPoint<PlayerController>();
            builder.RegisterComponentInHierarchy<FlashlightView>().As<IFlashlightView>();
            builder.RegisterEntryPoint<FlashlightController>();
            builder.RegisterEntryPoint<PlayerInteractor>();

            builder.RegisterEntryPoint<UIScreenService>();
            builder.RegisterComponentInHierarchy<InteractionPromptView>();
            builder.RegisterComponentInHierarchy<InteractionCrosshairUI>();

            builder.Register<GamePause>(Lifetime.Singleton).As<IGamePause>();
            builder.RegisterComponentInHierarchy<PauseScreen>();
            builder.RegisterComponentInHierarchy<SettingsScreen>();
            builder.RegisterEntryPoint<PausePresenter>();
            builder.RegisterEntryPoint<SettingsPresenter>();

            builder.RegisterComponentInHierarchy<SubtitleView>().As<ISubtitleView>();
            builder.RegisterEntryPoint<SubtitleService>();
            builder.RegisterEntryPoint<ItemPickupNotifier>();
        }
    }
}
