using UnityEngine;

namespace TheShadowWood.Core.Audio
{
    // A cue with no clips is valid and plays nothing, so designers can wire cues before audio exists.
    [CreateAssetMenu(menuName = "The Shadow Wood/Audio/Audio Cue", fileName = "NewAudioCue")]
    public sealed class AudioCue : ScriptableObject
    {
        [SerializeField] private AudioClip[] clips = new AudioClip[0];
        [SerializeField] private AudioBus bus = AudioBus.Sfx;

        [Header("Variation")]
        [SerializeField, Range(0f, 1f)] private float minVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float maxVolume = 1f;
        [SerializeField, Range(0.1f, 3f)] private float minPitch = 0.95f;
        [SerializeField, Range(0.1f, 3f)] private float maxPitch = 1.05f;
        [Tooltip("Never pick the clip that played last time.")]
        [SerializeField] private bool avoidRepeat = true;

        [Header("Playback")]
        [SerializeField] private bool loop;
        [Tooltip("0 = 2D, 1 = fully 3D.")]
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
        [SerializeField, Min(0.01f)] private float minDistance = 1f;
        [SerializeField, Min(0.01f)] private float maxDistance = 20f;
        [SerializeField] private AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;

        public AudioClip[] Clips => clips;
        public AudioBus Bus => bus;
        public float MinVolume => minVolume;
        public float MaxVolume => maxVolume;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;
        public bool AvoidRepeat => avoidRepeat;
        public bool Loop => loop;
        public float SpatialBlend => spatialBlend;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public AudioRolloffMode Rolloff => rolloff;

        public bool HasClips
        {
            get
            {
                if (clips == null)
                {
                    return false;
                }

                foreach (AudioClip clip in clips)
                {
                    if (clip != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private void OnValidate()
        {
            maxVolume = Mathf.Max(minVolume, maxVolume);
            maxPitch = Mathf.Max(minPitch, maxPitch);
            maxDistance = Mathf.Max(minDistance, maxDistance);
        }
    }
}
