using System;
using StarterAssets;
using UnityEngine;

namespace TheShadowWood.Player
{
    public class PlayerStamina : MonoBehaviour
    {
        [Header("Stamina Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaDrainRate = 20f;
        [SerializeField] private float staminaRegenRate = 15f;

        [Header("Regen Delay")]
        [SerializeField] private float regenDelayDuration = 1.5f;

        public float CurrentStamina {get; private set;}
        public bool IsExhausted => CurrentStamina <= 0f;

        public event Action<float> OnStaminaChanged;
        public event Action OnStaminaExhausted;

        private float _regenDelayTimer;
        private StarterAssetsInputs _input;

        private void Awake()
        {
            CurrentStamina = maxStamina;
            _input = GetComponent<StarterAssetsInputs>();
        }
        
        private void Start()
        {
            OnStaminaChanged?.Invoke(CurrentStamina / maxStamina);
        }

        private void Update()
        {
            if (_input == null) return;

            if (_input.crouch || IsExhausted)
            {
                _input.sprint = false;
            }

            bool isMovingAndSprinting = _input.sprint && _input.move != Vector2.zero;

            if (isMovingAndSprinting)
            {
                Consume(Time.deltaTime);
            }
            else
            {
                Regenerate(Time.deltaTime);
            }
        }

        public void Consume(float deltaTime)
        {
            if (CurrentStamina > 0f)
            {
                CurrentStamina -= staminaDrainRate * deltaTime;
                CurrentStamina = Mathf.Max(CurrentStamina, 0f);

                OnStaminaChanged?.Invoke(CurrentStamina / maxStamina);

                _regenDelayTimer = regenDelayDuration;

                if (CurrentStamina <= 0f)
                {
                    OnStaminaExhausted?.Invoke();
                }
            }        
        }

        public void Regenerate(float deltaTime)
        {
            if (_regenDelayTimer > 0f)
            {
                _regenDelayTimer -= deltaTime;
                return;
            }
            
            if (CurrentStamina < maxStamina)
            {
                CurrentStamina += staminaRegenRate * deltaTime;
                CurrentStamina = Mathf.Min(CurrentStamina, maxStamina);

                OnStaminaChanged?.Invoke(CurrentStamina / maxStamina);
            }
        }
    }
}