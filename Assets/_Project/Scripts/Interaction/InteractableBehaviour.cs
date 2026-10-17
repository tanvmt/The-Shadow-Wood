using System.Collections.Generic;
using UnityEngine;

namespace TheShadowWood.Interaction
{
    public abstract class InteractableBehaviour : MonoBehaviour, IInteractable
    {
        [Header("Prompt")]
        [SerializeField] private string prompt = "Interact";

        [Tooltip("Shown instead of the prompt while interaction is blocked. Empty hides the prompt.")]
        [SerializeField] private string blockedReason;

        [Tooltip("Where the prompt floats. Empty uses the top of the renderer bounds.")]
        [SerializeField] private Transform promptAnchor;

        [Header("Focus Feedback")]
        [Tooltip("Components must implement IInteractionFocusFeedback.")]
        [SerializeField] private MonoBehaviour[] focusFeedbackSources = new MonoBehaviour[0];

        private readonly List<IInteractionFocusFeedback> _focusFeedback = new List<IInteractionFocusFeedback>();
        private bool _isFocused;

        public Transform InteractionTransform => transform;
        public bool IsFocused => _isFocused;
        public virtual string Prompt => prompt;
        public virtual string BlockedReason => blockedReason;
        public Transform PromptAnchor => promptAnchor;

        protected virtual void Awake()
        {
            CacheFeedbackDependencies();
            ApplyFocusFeedback(false);
        }

        public virtual bool CanInteract(InteractionContext context)
        {
            return isActiveAndEnabled;
        }

        public void SetFocused(bool focused, InteractionContext context)
        {
            if (_isFocused == focused)
            {
                return;
            }

            _isFocused = focused;
            ApplyFocusFeedback(focused);
            OnFocusChanged(focused, context);
        }

        public InteractionResult Interact(InteractionContext context)
        {
            if (!CanInteract(context))
            {
                return InteractionResult.Rejected("Interaction is currently unavailable.");
            }

            return PerformInteraction(context);
        }

        protected abstract InteractionResult PerformInteraction(InteractionContext context);

        protected virtual void OnFocusChanged(bool focused, InteractionContext context)
        {
        }

        protected virtual void OnDisable()
        {
            _isFocused = false;
            ApplyFocusFeedback(false);
        }

        private void CacheFeedbackDependencies()
        {
            _focusFeedback.Clear();

            for (int i = 0; i < focusFeedbackSources.Length; i++)
            {
                MonoBehaviour source = focusFeedbackSources[i];
                if (source == null)
                {
                    continue;
                }

                if (source is IInteractionFocusFeedback feedback)
                {
                    _focusFeedback.Add(feedback);
                    continue;
                }

                Debug.LogError(
                    $"{source.GetType().Name} on {name} does not implement {nameof(IInteractionFocusFeedback)}.",
                    source);
            }
        }

        private void ApplyFocusFeedback(bool focused)
        {
            for (int i = 0; i < _focusFeedback.Count; i++)
            {
                _focusFeedback[i].SetHighlighted(focused);
            }
        }
    }
}
