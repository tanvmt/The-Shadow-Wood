using UnityEngine;

namespace TheShadowWood.Player
{
    public sealed class CrouchState
    {
        private readonly PlayerMovementConfig _config;

        public CrouchState(PlayerMovementConfig config)
        {
            _config = config;
            Height = config.StandingHeight;
        }

        public bool WantsCrouch { get; private set; }
        public bool IsCrouching { get; private set; }
        public float Height { get; private set; }

        public void Tick(bool togglePressed, bool canStand, float deltaTime)
        {
            if (togglePressed)
            {
                WantsCrouch = !WantsCrouch;
            }

            // Stay down under a low ceiling even after the player asks to stand.
            IsCrouching = WantsCrouch || (IsCrouching && !canStand);

            float target = IsCrouching ? _config.CrouchHeight : _config.StandingHeight;
            Height = Mathf.MoveTowards(Height, target, _config.CrouchTransitionSpeed * deltaTime);
        }
    }
}
