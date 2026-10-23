using System.Collections.Generic;
using UnityEngine;

namespace TheShadowWood.Audio
{
    public sealed class AmbienceScheduler
    {
        private readonly IReadOnlyList<AmbienceSpot> _spots;
        private readonly System.Random _random;
        private readonly float[] _remaining;

        public AmbienceScheduler(IReadOnlyList<AmbienceSpot> spots, System.Random random)
        {
            _spots = spots ?? new AmbienceSpot[0];
            _random = random;
            _remaining = new float[_spots.Count];

            // Each spot waits a full interval first so nothing fires the moment the scene opens.
            for (int i = 0; i < _remaining.Length; i++)
            {
                _remaining[i] = NextInterval(_spots[i]);
            }
        }

        public void Tick(float deltaTime, List<AmbienceSpot> due)
        {
            due.Clear();

            for (int i = 0; i < _remaining.Length; i++)
            {
                AmbienceSpot spot = _spots[i];
                if (spot == null)
                {
                    continue;
                }

                _remaining[i] -= deltaTime;
                if (_remaining[i] <= 0f)
                {
                    due.Add(spot);
                    _remaining[i] = NextInterval(spot);
                }
            }
        }

        public Vector3 Offset(AmbienceSpot spot)
        {
            float angle = Range(0f, Mathf.PI * 2f);
            float distance = Range(spot.MinDistance, spot.MaxDistance);
            float height = Range(spot.MinHeight, spot.MaxHeight);
            return new Vector3(Mathf.Cos(angle) * distance, height, Mathf.Sin(angle) * distance);
        }

        private float NextInterval(AmbienceSpot spot)
        {
            return spot == null ? 0f : Range(spot.MinInterval, spot.MaxInterval);
        }

        private float Range(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }
    }
}
