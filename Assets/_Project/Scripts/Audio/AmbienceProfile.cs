using System;
using TheShadowWood.Core.Audio;
using UnityEngine;

namespace TheShadowWood.Audio
{
    [CreateAssetMenu(menuName = "The Shadow Wood/Audio/Ambience Profile", fileName = "Ambience_New")]
    public sealed class AmbienceProfile : ScriptableObject
    {
        [Tooltip("Looping 2D beds, e.g. rain.")]
        [SerializeField] private AudioCue[] loops = new AudioCue[0];

        [Tooltip("One-shots played at random points around the listener, e.g. frogs.")]
        [SerializeField] private AmbienceSpot[] spots = new AmbienceSpot[0];

        public AudioCue[] Loops => loops;
        public AmbienceSpot[] Spots => spots;

        private void OnValidate()
        {
            foreach (AmbienceSpot spot in spots)
            {
                spot?.Validate();
            }
        }
    }

    [Serializable]
    public sealed class AmbienceSpot
    {
        [SerializeField] private AudioCue cue;

        [Tooltip("Seconds between two plays.")]
        [SerializeField, Min(0.1f)] private float minInterval = 3f;
        [SerializeField, Min(0.1f)] private float maxInterval = 9f;

        [Tooltip("Horizontal metres from the listener.")]
        [SerializeField, Min(0f)] private float minDistance = 6f;
        [SerializeField, Min(0f)] private float maxDistance = 15f;

        [Tooltip("Metres relative to the listener; negative is below the head.")]
        [SerializeField] private float minHeight = -1.6f;
        [SerializeField] private float maxHeight = 0f;

        public AmbienceSpot()
        {
        }

        public AmbienceSpot(
            AudioCue cue,
            float minInterval,
            float maxInterval,
            float minDistance,
            float maxDistance,
            float minHeight,
            float maxHeight)
        {
            this.cue = cue;
            this.minInterval = minInterval;
            this.maxInterval = maxInterval;
            this.minDistance = minDistance;
            this.maxDistance = maxDistance;
            this.minHeight = minHeight;
            this.maxHeight = maxHeight;
            Validate();
        }

        public AudioCue Cue => cue;
        public float MinInterval => minInterval;
        public float MaxInterval => maxInterval;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public float MinHeight => minHeight;
        public float MaxHeight => maxHeight;

        internal void Validate()
        {
            minInterval = Mathf.Max(0.1f, minInterval);
            maxInterval = Mathf.Max(minInterval, maxInterval);
            maxDistance = Mathf.Max(minDistance, maxDistance);
            maxHeight = Mathf.Max(minHeight, maxHeight);
        }
    }
}
