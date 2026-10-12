using VContainer.Unity;

namespace TheShadowWood.Player
{
    public sealed class CameraRigBinder : IStartable
    {
        private readonly CameraRig _rig;
        private readonly PlayerView _player;

        public CameraRigBinder(CameraRig rig, PlayerView player)
        {
            _rig = rig;
            _player = player;
        }

        public void Start()
        {
            _rig.FpsCamera.Target.TrackingTarget = _player.CameraRoot;
        }
    }
}
