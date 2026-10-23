using System;
using System.Collections.Generic;
using TheShadowWood.Core.Audio;
using UnityEngine;

namespace TheShadowWood.Audio
{
    [CreateAssetMenu(menuName = "The Shadow Wood/Audio/Gameplay Audio Config", fileName = "GameplayAudioConfig")]
    public sealed class GameplayAudioConfig : ScriptableObject
    {
        [Header("Default (no state flag set)")]
        [SerializeField] private AmbienceProfile defaultAmbience;
        [SerializeField] private AudioCue defaultMusic;
        [Tooltip("Seconds to crossfade back to the default.")]
        [SerializeField, Min(0f)] private float defaultCrossfade = 3f;
        [Tooltip("Seconds to fade in when the scene starts.")]
        [SerializeField, Min(0f)] private float startFadeIn = 2f;

        [Tooltip("The lowest entry whose flag is set wins, so list states in story order.")]
        [SerializeField] private AudioState[] states = new AudioState[0];

        [NonSerialized] private AudioState _default;

        public AudioState Default => _default ??= new AudioState(
            "Default", null, defaultAmbience, defaultMusic, TensionStage.Calm, defaultCrossfade);

        public IReadOnlyList<AudioState> States => states;
        public float StartFadeIn => startFadeIn;

        private void OnValidate()
        {
            _default = null;
            foreach (AudioState state in states)
            {
                if (state != null && state.EnterFlag == null)
                {
                    Debug.LogWarning($"{name}: audio state '{state.Name}' has no enter flag and will never play.", this);
                }
            }
        }
    }
}
