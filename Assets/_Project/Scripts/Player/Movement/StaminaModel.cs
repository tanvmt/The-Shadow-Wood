using UnityEngine;

namespace TheShadowWood.Player
{
    public sealed class StaminaModel
    {
        private readonly PlayerMovementConfig _config;
        private float _regenDelayLeft;

        public StaminaModel(PlayerMovementConfig config)
        {
            _config = config;
            Current = config.MaxStamina;
        }

        public float Current { get; private set; }
        public float Ratio => Current / _config.MaxStamina;
        public bool IsExhausted { get; private set; }
        public bool CanSprint => !IsExhausted && Current > 0f;

        // isSprinting should come from actual speed, not just the sprint button.
        public void Tick(bool isSprinting, float deltaTime)
        {
            if (isSprinting)
            {
                Current = Mathf.Max(0f, Current - _config.StaminaDrainPerSecond * deltaTime);
                _regenDelayLeft = _config.StaminaRegenDelay;

                if (Current <= 0f)
                {
                    IsExhausted = true;
                }

                return;
            }

            if (_regenDelayLeft > 0f)
            {
                _regenDelayLeft -= deltaTime;
                return;
            }

            Current = Mathf.Min(_config.MaxStamina, Current + _config.StaminaRegenPerSecond * deltaTime);

            if (IsExhausted && Current >= _config.StaminaToResumeSprint)
            {
                IsExhausted = false;
            }
        }
    }
}
