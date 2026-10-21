using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TheShadowWood.Audio.Tests
{
    public sealed class AmbienceSchedulerTests
    {
        private readonly List<AmbienceSpot> _due = new List<AmbienceSpot>();

        [Test]
        public void Spot_DoesNotFireBeforeMinInterval()
        {
            AmbienceSpot spot = Spot(minInterval: 3f, maxInterval: 9f);
            AmbienceScheduler scheduler = new AmbienceScheduler(new[] { spot }, new System.Random(7));

            scheduler.Tick(2.9f, _due);

            Assert.That(_due, Is.Empty);
        }

        [Test]
        public void Spot_FiresByMaxInterval_ThenWaitsAgain()
        {
            AmbienceSpot spot = Spot(minInterval: 3f, maxInterval: 9f);
            AmbienceScheduler scheduler = new AmbienceScheduler(new[] { spot }, new System.Random(7));

            scheduler.Tick(9f, _due);
            Assert.That(_due, Is.EqualTo(new[] { spot }));

            scheduler.Tick(2.9f, _due);
            Assert.That(_due, Is.Empty);
        }

        [Test]
        public void Spot_FiresRepeatedlyOverTime()
        {
            AmbienceSpot spot = Spot(minInterval: 1f, maxInterval: 2f);
            AmbienceScheduler scheduler = new AmbienceScheduler(new[] { spot }, new System.Random(7));

            int fired = 0;
            for (int i = 0; i < 100; i++)
            {
                scheduler.Tick(0.1f, _due);
                fired += _due.Count;
            }

            // About 10 seconds at one play per 1..2 seconds; float steps may drop the last one.
            Assert.That(fired, Is.InRange(4, 10));
        }

        [Test]
        public void Offset_StaysWithinDistanceAndHeight()
        {
            AmbienceSpot spot = Spot(minDistance: 6f, maxDistance: 15f, minHeight: -1.6f, maxHeight: 0f);
            AmbienceScheduler scheduler = new AmbienceScheduler(new[] { spot }, new System.Random(7));

            for (int i = 0; i < 100; i++)
            {
                Vector3 offset = scheduler.Offset(spot);
                float horizontal = new Vector2(offset.x, offset.z).magnitude;

                Assert.That(horizontal, Is.InRange(6f - 1e-3f, 15f + 1e-3f));
                Assert.That(offset.y, Is.InRange(-1.6f, 0f));
            }
        }

        [Test]
        public void NoSpots_NeverFires()
        {
            AmbienceScheduler scheduler = new AmbienceScheduler(new AmbienceSpot[0], new System.Random(7));

            scheduler.Tick(100f, _due);

            Assert.That(_due, Is.Empty);
        }

        [Test]
        public void NullSpots_AreIgnored()
        {
            AmbienceSpot spot = Spot(minInterval: 1f, maxInterval: 1f);
            AmbienceScheduler scheduler = new AmbienceScheduler(new[] { null, spot }, new System.Random(7));

            scheduler.Tick(1f, _due);

            Assert.That(_due, Is.EqualTo(new[] { spot }));
        }

        private static AmbienceSpot Spot(
            float minInterval = 3f,
            float maxInterval = 9f,
            float minDistance = 6f,
            float maxDistance = 15f,
            float minHeight = -1.6f,
            float maxHeight = 0f)
        {
            return new AmbienceSpot(null, minInterval, maxInterval, minDistance, maxDistance, minHeight, maxHeight);
        }
    }
}
