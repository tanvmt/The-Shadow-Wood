using System;
using UnityEngine;

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

    private void Awake()
    {
        CurrentStamina = maxStamina;
    }
    
    private void Start()
    {
        OnStaminaChanged?.Invoke(CurrentStamina / maxStamina);
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
