using NUnit.Framework;
using TheShadowWood.Core.Audio;
using UnityEngine;

namespace TheShadowWood.Audio.Tests
{
    public sealed class CueClipPickerTests
    {
        private TestAudio _audio;
        private CueClipPicker _picker;

        [SetUp]
        public void SetUp()
        {
            _audio = new TestAudio();
            _picker = new CueClipPicker(new System.Random(1234));
        }

        [TearDown]
        public void TearDown()
        {
            _audio.Dispose();
        }

        [Test]
        public void SingleClip_IsAlwaysPicked()
        {
            AudioClip clip = _audio.Clip();
            AudioCue cue = _audio.Cue(clips: new[] { clip });

            for (int i = 0; i < 10; i++)
            {
                Assert.That(_picker.Pick(cue), Is.SameAs(clip));
            }
        }

        [Test]
        public void AvoidRepeat_NeverPicksSameClipTwiceInARow()
        {
            AudioCue cue = _audio.Cue(clips: new[] { _audio.Clip("a"), _audio.Clip("b"), _audio.Clip("c") });

            AudioClip previous = _picker.Pick(cue);
            for (int i = 0; i < 100; i++)
            {
                AudioClip next = _picker.Pick(cue);
                Assert.That(next, Is.Not.SameAs(previous));
                previous = next;
            }
        }

        [Test]
        public void WithoutAvoidRepeat_CanPickSameClipAgain()
        {
            AudioCue cue = _audio.Cue(avoidRepeat: false, clips: new[] { _audio.Clip("a"), _audio.Clip("b") });

            bool repeated = false;
            AudioClip previous = _picker.Pick(cue);
            for (int i = 0; i < 100 && !repeated; i++)
            {
                AudioClip next = _picker.Pick(cue);
                repeated = next == previous;
                previous = next;
            }

            Assert.That(repeated, Is.True);
        }

        [Test]
        public void NullClips_AreSkipped()
        {
            AudioClip clip = _audio.Clip();
            AudioCue cue = _audio.Cue(clips: new[] { null, clip, null });

            for (int i = 0; i < 10; i++)
            {
                Assert.That(_picker.Pick(cue), Is.SameAs(clip));
            }
        }

        [Test]
        public void EmptyOrNullCue_ReturnsNull()
        {
            AudioCue allNull = _audio.Cue(clips: new AudioClip[] { null, null });

            Assert.That(_picker.Pick(null), Is.Null);
            Assert.That(_picker.Pick(_audio.EmptyCue()), Is.Null);
            Assert.That(_picker.Pick(allNull), Is.Null);
        }

        [Test]
        public void Range_StaysWithinBounds()
        {
            for (int i = 0; i < 100; i++)
            {
                Assert.That(_picker.Range(0.8f, 1.2f), Is.InRange(0.8f, 1.2f));
            }
        }
    }
}
