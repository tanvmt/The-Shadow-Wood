using System;
using TheShadowWood.Core.Audio;
using UnityEngine;

namespace TheShadowWood.Player
{
    [CreateAssetMenu(menuName = "The Shadow Wood/Player/Footstep Config", fileName = "FootstepConfig")]
    public sealed class FootstepConfig : ScriptableObject
    {
        [Tooltip("Used when the ground has no physics material or one not listed below.")]
        [SerializeField] private AudioCue defaultCue;
        [SerializeField] private FootstepSurface[] surfaces = new FootstepSurface[0];

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float crouchVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float walkVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float sprintVolume = 1f;

        [Header("Ground Check")]
        [Tooltip("Must not include the Player layer.")]
        [SerializeField] private LayerMask groundMask = ~0;
        [Tooltip("Metres below the feet to look for ground.")]
        [SerializeField, Min(0.05f)] private float rayLength = 0.4f;

        public AudioCue DefaultCue => defaultCue;
        public LayerMask GroundMask => groundMask;
        public float RayLength => rayLength;

        public AudioCue CueFor(PhysicsMaterial material)
        {
            if (material != null)
            {
                foreach (FootstepSurface surface in surfaces)
                {
                    if (surface.Material == material && surface.Cue != null)
                    {
                        return surface.Cue;
                    }
                }
            }

            return defaultCue;
        }

        public float VolumeFor(bool crouching, bool sprinting)
        {
            return crouching ? crouchVolume
                : sprinting ? sprintVolume
                : walkVolume;
        }
    }

    [Serializable]
    public sealed class FootstepSurface
    {
        [SerializeField] private PhysicsMaterial material;
        [SerializeField] private AudioCue cue;

        public FootstepSurface()
        {
        }

        public FootstepSurface(PhysicsMaterial material, AudioCue cue)
        {
            this.material = material;
            this.cue = cue;
        }

        public PhysicsMaterial Material => material;
        public AudioCue Cue => cue;
    }
}
