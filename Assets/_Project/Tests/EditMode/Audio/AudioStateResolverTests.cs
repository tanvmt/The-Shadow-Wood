using System.Collections.Generic;
using NUnit.Framework;
using TheShadowWood.Core.Audio;
using TheShadowWood.Core.Flags;
using UnityEngine;

namespace TheShadowWood.Audio.Tests
{
    public sealed class AudioStateResolverTests
    {
        private readonly HashSet<FlagId> _set = new HashSet<FlagId>();

        private FlagId _unease;
        private FlagId _climax;
        private AudioState _default;
        private AudioState _uneaseState;
        private AudioState _climaxState;
        private AudioState[] _states;

        [SetUp]
        public void SetUp()
        {
            _set.Clear();
            _unease = ScriptableObject.CreateInstance<FlagId>();
            _climax = ScriptableObject.CreateInstance<FlagId>();

            _default = State("Default", null, TensionStage.Calm);
            _uneaseState = State("Unease", _unease, TensionStage.Unease);
            _climaxState = State("Climax", _climax, TensionStage.Climax);
            _states = new[] { _uneaseState, _climaxState };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_unease);
            Object.DestroyImmediate(_climax);
        }

        [Test]
        public void NoFlagSet_ReturnsFallback()
        {
            Assert.That(Resolve(), Is.SameAs(_default));
        }

        [Test]
        public void SeveralSet_LowestListedWins()
        {
            _set.Add(_climax);
            _set.Add(_unease);

            Assert.That(Resolve(), Is.SameAs(_climaxState));
        }

        [Test]
        public void WinnerCleared_FallsBackToNextSetState()
        {
            _set.Add(_unease);
            _set.Add(_climax);
            _set.Remove(_climax);

            Assert.That(Resolve(), Is.SameAs(_uneaseState));
        }

        [Test]
        public void StateWithoutFlagOrNullEntry_IsSkipped()
        {
            _states = new[] { _uneaseState, State("Broken", null, TensionStage.Climax), null };
            _set.Add(_unease);

            Assert.That(Resolve(), Is.SameAs(_uneaseState));
        }

        private AudioState Resolve()
        {
            return AudioStateResolver.Resolve(_states, _default, _set.Contains);
        }

        private static AudioState State(string name, FlagId flag, TensionStage tension)
        {
            return new AudioState(name, flag, null, null, tension, 1f);
        }
    }
}
