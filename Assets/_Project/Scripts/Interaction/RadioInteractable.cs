using TheShadowWood.Core.Audio;
using TheShadowWood.Core.Flags;
using UnityEngine;
using VContainer;

namespace TheShadowWood.Interaction
{
    [DisallowMultipleComponent]
    public sealed class RadioInteractable : InteractableBehaviour
    {
        [Header("Radio")]
        [Tooltip("Looping cue plays until switched off; a one-shot turns the radio off when it ends.")]
        [SerializeField] private AudioCue broadcast;
        [SerializeField] private AudioCue switchCue;
        [Tooltip("Where the sound comes from. Empty uses this transform.")]
        [SerializeField] private Transform speaker;
        [SerializeField, Min(0f)] private float fade = 0.3f;
        [SerializeField] private string onPrompt = "Turn on";
        [SerializeField] private string offPrompt = "Turn off";
        [SerializeField] private bool playOnStart;

        [Header("Story")]
        [Tooltip("Set the first time the player turns the radio on.")]
        [SerializeField] private FlagId setOnFirstTurnOn;

        private IAudioService _audio;
        private IFlagService _flags;
        private AudioHandle _handle;
        private bool _flagSet;

        [Inject]
        public void Construct(IAudioService audio, IFlagService flags)
        {
            _audio = audio;
            _flags = flags;
        }

        public bool IsOn => _audio != null && _audio.IsPlaying(_handle);
        public override string Prompt => IsOn ? offPrompt : onPrompt;
        private Transform Speaker => speaker != null ? speaker : transform;

        protected override void Awake()
        {
            base.Awake();
            if (broadcast == null)
            {
                Debug.LogError($"{name}: Radio has no broadcast cue.", this);
            }
        }

        private void Start()
        {
            if (playOnStart && CanInteract(default))
            {
                TurnOn();
            }
        }

        public override bool CanInteract(InteractionContext context)
        {
            return _audio != null && _flags != null && broadcast != null && base.CanInteract(context);
        }

        protected override InteractionResult PerformInteraction(InteractionContext context)
        {
            PlaySwitch();

            if (IsOn)
            {
                TurnOff(fade);
                return InteractionResult.Success();
            }

            if (!TurnOn())
            {
                return InteractionResult.Rejected($"{broadcast.name} has no clips.");
            }

            if (setOnFirstTurnOn != null && !_flagSet)
            {
                _flagSet = true;
                _flags.Set(setOnFirstTurnOn);
            }

            return InteractionResult.Success();
        }

        protected override void OnDisable()
        {
            TurnOff(fade);
            base.OnDisable();
        }

        private bool TurnOn()
        {
            _handle = _audio.PlayAttached(broadcast, Speaker, fadeIn: fade);
            return _handle.IsValid;
        }

        private void TurnOff(float fadeOut)
        {
            if (_audio != null && _handle.IsValid)
            {
                _audio.Stop(_handle, fadeOut);
            }

            _handle = AudioHandle.None;
        }

        private void PlaySwitch()
        {
            if (switchCue != null)
            {
                _audio.PlayAt(switchCue, Speaker.position);
            }
        }
    }
}
