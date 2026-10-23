using System;
using TheShadowWood.Core.Audio;
using TheShadowWood.Core.Flags;
using UnityEngine;

namespace TheShadowWood.Audio
{
    [Serializable]
    public sealed class AudioState
    {
        [Tooltip("Label for the Inspector only.")]
        [SerializeField] private string name;
        [SerializeField] private FlagId enterFlag;
        [Tooltip("Empty means no ambience.")]
        [SerializeField] private AmbienceProfile ambience;
        [Tooltip("Empty means no music.")]
        [SerializeField] private AudioCue music;
        [SerializeField] private TensionStage tension;
        [Tooltip("Seconds to crossfade into this state.")]
        [SerializeField, Min(0f)] private float crossfade = 3f;

        public AudioState()
        {
        }

        public AudioState(string name, FlagId enterFlag, AmbienceProfile ambience, AudioCue music,
            TensionStage tension, float crossfade)
        {
            this.name = name;
            this.enterFlag = enterFlag;
            this.ambience = ambience;
            this.music = music;
            this.tension = tension;
            this.crossfade = crossfade;
        }

        public string Name => name;
        public FlagId EnterFlag => enterFlag;
        public AmbienceProfile Ambience => ambience;
        public AudioCue Music => music;
        public TensionStage Tension => tension;
        public float Crossfade => crossfade;
    }
}
