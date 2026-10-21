using UnityEngine;

namespace TheShadowWood.Audio
{
    [CreateAssetMenu(menuName = "The Shadow Wood/Audio/Gameplay Audio Config", fileName = "GameplayAudioConfig")]
    public sealed class GameplayAudioConfig : ScriptableObject
    {
        [SerializeField] private AmbienceProfile defaultAmbience;
        [Tooltip("Seconds to fade the ambience in when the scene starts.")]
        [SerializeField, Min(0f)] private float startFadeIn = 2f;

        public AmbienceProfile DefaultAmbience => defaultAmbience;
        public float StartFadeIn => startFadeIn;
    }
}
