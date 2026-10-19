using System.Collections.Generic;
using TheShadowWood.Core.Audio;
using UnityEngine;

namespace TheShadowWood.Audio
{
    public sealed class CueClipPicker
    {
        private readonly System.Random _random;
        private readonly Dictionary<AudioCue, int> _lastPicked = new Dictionary<AudioCue, int>();
        private readonly List<int> _candidates = new List<int>();

        public CueClipPicker(System.Random random)
        {
            _random = random;
        }

        // Returns null when the cue has no usable clip.
        public AudioClip Pick(AudioCue cue)
        {
            if (cue == null || cue.Clips == null)
            {
                return null;
            }

            AudioClip[] clips = cue.Clips;
            _candidates.Clear();
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                {
                    _candidates.Add(i);
                }
            }

            if (_candidates.Count == 0)
            {
                return null;
            }

            if (cue.AvoidRepeat && _candidates.Count > 1 && _lastPicked.TryGetValue(cue, out int last))
            {
                _candidates.Remove(last);
            }

            int index = _candidates[_random.Next(_candidates.Count)];
            _lastPicked[cue] = index;
            return clips[index];
        }

        public float Range(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }
    }
}
