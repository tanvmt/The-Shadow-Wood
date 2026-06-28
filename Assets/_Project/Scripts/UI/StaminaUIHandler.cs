using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TheShadowWood.Player;

namespace TheShadowWood.UI
{
    public class StaminaUIHandler : MonoBehaviour
    {
        [Header("UI Resources")]
        [SerializeField] private Image staminaFillImage;
        [SerializeField] private CanvasGroup staminaCanvasGroup;

        [Header("Event Source")]
        [SerializeField] private PlayerStamina targetStaminaSystem;

        [Header("Fade Config")]
        [SerializeField] private float fadeDuration = 0.3f;

        private Tween _fadeTween; 

        private void OnEnable()
        {
            if (targetStaminaSystem != null)
            {
                targetStaminaSystem.OnStaminaChanged += UpdateStaminaUI;
            }
        }

        private void OnDisable()
        {
            if (targetStaminaSystem != null)
            {
                targetStaminaSystem.OnStaminaChanged -= UpdateStaminaUI;
            }

            _fadeTween?.Kill();
        }

        private void UpdateStaminaUI(float staminaRatio)
        {
            if (staminaFillImage != null)
            {
                staminaFillImage.transform.localScale = new Vector3(staminaRatio, 1f, 1f);
            }

            if (staminaCanvasGroup != null)
            {
                float targetAlpha = (staminaRatio < 0.99f) ? 1f : 0f;

                if (!Mathf.Approximately(staminaCanvasGroup.alpha, targetAlpha))
                {
                    _fadeTween?.Kill();

                    _fadeTween = staminaCanvasGroup.DOFade(targetAlpha, fadeDuration).SetUpdate(true);
                }
            }
        }
    }
}
