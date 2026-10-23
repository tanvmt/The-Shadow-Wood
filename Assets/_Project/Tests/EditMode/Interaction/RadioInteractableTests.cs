using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TheShadowWood.Core.Audio;
using TheShadowWood.Core.Flags;
using UnityEngine;

namespace TheShadowWood.Interaction.Tests
{
    public sealed class RadioInteractableTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly List<Object> _created = new List<Object>();

        private FakeAudioService _audio;
        private FakeFlagService _flags;
        private GameObject _radioObject;
        private RadioInteractable _radio;
        private AudioCue _broadcast;
        private AudioCue _switch;
        private FlagId _flag;
        private InteractionContext _context;

        [SetUp]
        public void SetUp()
        {
            _audio = new FakeAudioService();
            _flags = new FakeFlagService();
            _broadcast = Cue(loop: true);
            _switch = Cue(loop: false);
            _flag = Track(ScriptableObject.CreateInstance<FlagId>());

            _radioObject = Track(new GameObject("Radio"));
            _radio = _radioObject.AddComponent<RadioInteractable>();
            Set(_radio, "broadcast", _broadcast);
            _radio.Construct(_audio, _flags);
            _context = new InteractionContext(null, null, default);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
        }

        [Test]
        public void FirstInteract_PlaysBroadcastOnSpeakerWithFade()
        {
            InteractionResult result = _radio.Interact(_context);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(_radio.IsOn, Is.True);
            Assert.That(_audio.Plays.Count, Is.EqualTo(1));
            Assert.That(_audio.Plays[0].Cue, Is.SameAs(_broadcast));
            Assert.That(_audio.Plays[0].Target, Is.SameAs(_radioObject.transform));
            Assert.That(_audio.Plays[0].FadeIn, Is.EqualTo(0.3f));
        }

        [Test]
        public void SecondInteract_StopsWithFade()
        {
            _radio.Interact(_context);

            _radio.Interact(_context);

            Assert.That(_radio.IsOn, Is.False);
            Assert.That(_audio.Stops.Count, Is.EqualTo(1));
            Assert.That(_audio.Stops[0].FadeOut, Is.EqualTo(0.3f));
        }

        [Test]
        public void Prompt_FollowsState()
        {
            Assert.That(_radio.Prompt, Is.EqualTo("Turn on"));

            _radio.Interact(_context);

            Assert.That(_radio.Prompt, Is.EqualTo("Turn off"));
        }

        [Test]
        public void SwitchCue_PlaysOnBothToggles()
        {
            Set(_radio, "switchCue", _switch);

            _radio.Interact(_context);
            _radio.Interact(_context);

            Assert.That(_audio.Plays.FindAll(play => play.Cue == _switch).Count, Is.EqualTo(2));
        }

        [Test]
        public void Flag_IsSetOnlyOnFirstTurnOn()
        {
            Set(_radio, "setOnFirstTurnOn", _flag);

            _radio.Interact(_context);
            _radio.Interact(_context);
            _radio.Interact(_context);

            Assert.That(_flags.SetCalls, Is.EqualTo(new[] { _flag }));
        }

        [Test]
        public void FinishedOneShot_TurnsRadioOff()
        {
            _radio.Interact(_context);
            _audio.Finish(_audio.LastHandle);

            Assert.That(_radio.IsOn, Is.False);
            Assert.That(_radio.Prompt, Is.EqualTo("Turn on"));
        }

        [Test]
        public void BroadcastWithoutClips_IsRejected_AndSetsNoFlag()
        {
            Set(_radio, "broadcast", Track(ScriptableObject.CreateInstance<AudioCue>()));
            Set(_radio, "setOnFirstTurnOn", _flag);

            InteractionResult result = _radio.Interact(_context);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(_radio.IsOn, Is.False);
            Assert.That(_flags.SetCalls, Is.Empty);
        }

        [Test]
        public void MissingInjectionOrBroadcast_IsBlocked()
        {
            Set(_radio, "broadcast", null);
            Assert.That(_radio.CanInteract(_context), Is.False);

            Set(_radio, "broadcast", _broadcast);
            _radio.Construct(null, null);
            Assert.That(_radio.CanInteract(_context), Is.False);
        }

        private AudioCue Cue(bool loop)
        {
            AudioCue cue = Track(ScriptableObject.CreateInstance<AudioCue>());
            AudioClip clip = Track(AudioClip.Create("clip", 4410, 1, 44100, false));
            Set(cue, "clips", new[] { clip });
            Set(cue, "loop", loop);
            return cue;
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, Private).SetValue(target, value);
        }

        private sealed class FakeFlagService : IFlagService
        {
            private readonly HashSet<FlagId> _flags = new HashSet<FlagId>();

            public List<FlagId> SetCalls { get; } = new List<FlagId>();

            public bool Has(FlagId flag)
            {
                return _flags.Contains(flag);
            }

            public void Set(FlagId flag)
            {
                SetCalls.Add(flag);
                _flags.Add(flag);
            }

            public void Clear(FlagId flag)
            {
                _flags.Remove(flag);
            }
        }
    }
}
