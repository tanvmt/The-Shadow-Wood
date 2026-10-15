using TheShadowWood.Core.Cameras;
using TheShadowWood.Interaction;
using TheShadowWood.Player;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TheShadowWood.Bootstrap
{
    public class GameplayLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Scene needs one Player and one CameraRig.
            builder.RegisterComponentInHierarchy<PlayerView>();
            builder.RegisterComponentInHierarchy<PlayerInteractorView>();
            builder.RegisterComponentInHierarchy<CameraRig>();
            builder.Register<Camera>(r => r.Resolve<CameraRig>().OutputCamera, Lifetime.Singleton);
            builder.Register<CameraFocusService>(Lifetime.Singleton).As<ICameraFocusService>();
            builder.Register<CameraShakeService>(Lifetime.Singleton).As<ICameraShakeService>();
            builder.RegisterEntryPoint<CameraRigBinder>();
            builder.RegisterEntryPoint<PlayerController>();
            builder.RegisterEntryPoint<PlayerInteractor>();
        }
    }
}
