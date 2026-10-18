using System;
using System.Collections.Generic;
using MessagePipe;
using NUnit.Framework;
using TheShadowWood.Core.Flags;
using UnityEngine;

namespace TheShadowWood.Core.Tests
{
    public sealed class FlagServiceTests
    {
        private RecordingPublisher _publisher;
        private FlagService _flags;
        private FlagId _flag;
        private FlagId _other;

        [SetUp]
        public void SetUp()
        {
            _publisher = new RecordingPublisher();
            _flags = new FlagService(_publisher);
            _flag = ScriptableObject.CreateInstance<FlagId>();
            _other = ScriptableObject.CreateInstance<FlagId>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_flag);
            UnityEngine.Object.DestroyImmediate(_other);
        }

        [Test]
        public void NewService_HasNoFlags()
        {
            Assert.That(_flags.Has(_flag), Is.False);
        }

        [Test]
        public void Set_MakesOnlyThatFlagTrue_AndPublishes()
        {
            _flags.Set(_flag);

            Assert.That(_flags.Has(_flag), Is.True);
            Assert.That(_flags.Has(_other), Is.False);
            Assert.That(_publisher.Messages, Has.Count.EqualTo(1));
            Assert.That(_publisher.Messages[0].Flag, Is.SameAs(_flag));
            Assert.That(_publisher.Messages[0].Value, Is.True);
        }

        [Test]
        public void SetTwice_PublishesOnce()
        {
            _flags.Set(_flag);
            _flags.Set(_flag);

            Assert.That(_publisher.Messages, Has.Count.EqualTo(1));
        }

        [Test]
        public void Clear_RemovesFlag_AndPublishesFalse()
        {
            _flags.Set(_flag);

            _flags.Clear(_flag);

            Assert.That(_flags.Has(_flag), Is.False);
            Assert.That(_publisher.Messages, Has.Count.EqualTo(2));
            Assert.That(_publisher.Messages[1].Value, Is.False);
        }

        [Test]
        public void ClearUnsetFlag_DoesNotPublish()
        {
            _flags.Clear(_flag);

            Assert.That(_publisher.Messages, Is.Empty);
        }

        [Test]
        public void NullFlag_HasIsFalse_SetAndClearThrow()
        {
            Assert.That(_flags.Has(null), Is.False);
            Assert.Throws<ArgumentNullException>(() => _flags.Set(null));
            Assert.Throws<ArgumentNullException>(() => _flags.Clear(null));
            Assert.That(_publisher.Messages, Is.Empty);
        }

        private sealed class RecordingPublisher : IPublisher<FlagChangedMessage>
        {
            public List<FlagChangedMessage> Messages { get; } = new List<FlagChangedMessage>();

            public void Publish(FlagChangedMessage message)
            {
                Messages.Add(message);
            }
        }
    }
}
