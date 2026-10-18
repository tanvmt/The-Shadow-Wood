using System;
using System.Collections.Generic;
using MessagePipe;
using NUnit.Framework;
using TheShadowWood.Core.Items;
using UnityEngine;

namespace TheShadowWood.Inventory.Tests
{
    public sealed class InventoryServiceTests
    {
        private RecordingPublisher<ItemAddedMessage> _added;
        private RecordingPublisher<ItemRemovedMessage> _removed;
        private InventoryService _inventory;
        private ItemDefinition _key;
        private ItemDefinition _candle;

        [SetUp]
        public void SetUp()
        {
            _added = new RecordingPublisher<ItemAddedMessage>();
            _removed = new RecordingPublisher<ItemRemovedMessage>();
            _inventory = new InventoryService(_added, _removed);
            _key = CreateItem(1);
            _candle = CreateItem(3);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_key);
            UnityEngine.Object.DestroyImmediate(_candle);
        }

        [Test]
        public void NewInventory_IsEmpty()
        {
            Assert.That(_inventory.Items, Is.Empty);
            Assert.That(_inventory.Count(_key), Is.EqualTo(0));
            Assert.That(_inventory.Has(_key), Is.False);
        }

        [Test]
        public void TryAdd_StoresItem_AndPublishesCountAndTotal()
        {
            Assert.That(_inventory.TryAdd(_candle, 2), Is.True);
            Assert.That(_inventory.TryAdd(_candle), Is.True);

            Assert.That(_inventory.Count(_candle), Is.EqualTo(3));
            Assert.That(_inventory.Has(_candle, 3), Is.True);
            Assert.That(_inventory.Has(_candle, 4), Is.False);
            Assert.That(_added.Messages, Has.Count.EqualTo(2));
            Assert.That(_added.Messages[1].Item, Is.SameAs(_candle));
            Assert.That(_added.Messages[1].Count, Is.EqualTo(1));
            Assert.That(_added.Messages[1].Total, Is.EqualTo(3));
        }

        [Test]
        public void TryAdd_OverMaxCount_IsRejected_AndChangesNothing()
        {
            _inventory.TryAdd(_key);

            Assert.That(_inventory.TryAdd(_key), Is.False);
            Assert.That(_inventory.TryAdd(_candle, 4), Is.False);

            Assert.That(_inventory.Count(_key), Is.EqualTo(1));
            Assert.That(_inventory.Has(_candle), Is.False);
            Assert.That(_added.Messages, Has.Count.EqualTo(1));
        }

        [Test]
        public void TryRemove_LowersCount_AndPublishes()
        {
            _inventory.TryAdd(_candle, 3);

            Assert.That(_inventory.TryRemove(_candle, 2), Is.True);

            Assert.That(_inventory.Count(_candle), Is.EqualTo(1));
            Assert.That(_removed.Messages, Has.Count.EqualTo(1));
            Assert.That(_removed.Messages[0].Count, Is.EqualTo(2));
            Assert.That(_removed.Messages[0].Total, Is.EqualTo(1));
        }

        [Test]
        public void TryRemove_LastOne_DropsStack()
        {
            _inventory.TryAdd(_key);

            _inventory.TryRemove(_key);

            Assert.That(_inventory.Items, Is.Empty);
            Assert.That(_inventory.Has(_key), Is.False);
        }

        [Test]
        public void TryRemove_NotEnough_IsRejected_AndChangesNothing()
        {
            _inventory.TryAdd(_candle);

            Assert.That(_inventory.TryRemove(_candle, 2), Is.False);
            Assert.That(_inventory.TryRemove(_key), Is.False);

            Assert.That(_inventory.Count(_candle), Is.EqualTo(1));
            Assert.That(_removed.Messages, Is.Empty);
        }

        [Test]
        public void Items_KeepPickupOrder()
        {
            _inventory.TryAdd(_candle);
            _inventory.TryAdd(_key);
            _inventory.TryAdd(_candle);

            Assert.That(_inventory.Items, Has.Count.EqualTo(2));
            Assert.That(_inventory.Items[0].Item, Is.SameAs(_candle));
            Assert.That(_inventory.Items[0].Count, Is.EqualTo(2));
            Assert.That(_inventory.Items[1].Item, Is.SameAs(_key));
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.That(_inventory.Has(null), Is.False);
            Assert.Throws<ArgumentNullException>(() => _inventory.TryAdd(null));
            Assert.Throws<ArgumentNullException>(() => _inventory.TryRemove(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => _inventory.TryAdd(_key, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _inventory.TryRemove(_key, -1));
            Assert.That(_added.Messages, Is.Empty);
        }

        private static ItemDefinition CreateItem(int maxCount)
        {
            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            typeof(ItemDefinition)
                .GetField("maxCount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(item, maxCount);
            return item;
        }

        private sealed class RecordingPublisher<T> : IPublisher<T>
        {
            public List<T> Messages { get; } = new List<T>();

            public void Publish(T message)
            {
                Messages.Add(message);
            }
        }
    }
}
