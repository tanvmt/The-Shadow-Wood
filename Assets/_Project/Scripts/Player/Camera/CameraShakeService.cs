using TheShadowWood.Core.Cameras;
using UnityEngine;

namespace TheShadowWood.Player
{
    public sealed class CameraShakeService : ICameraShakeService
    {
        private readonly CameraRig _rig;

        public CameraShakeService(CameraRig rig)
        {
            _rig = rig;
        }

        public void Shake(float force = 1f)
        {
            if (_rig.ImpulseSource == null)
            {
                Debug.LogWarning("[CameraShakeService] CameraRig has no impulse source assigned.", _rig);
                return;
            }

            _rig.ImpulseSource.GenerateImpulseWithForce(force);
        }
    }
}
