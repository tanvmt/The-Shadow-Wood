using System.Collections.Generic;
using System.Reflection;
using MessagePipe;
using NUnit.Framework;
using TheShadowWood.Core.Items;
using TheShadowWood.Interaction;
using UnityEngine;

namespace TheShadowWood.Inventory.Tests
{
    // Accepted pickups call Destroy, which Edit Mode refuses, so only the rejecting paths run here.
    public sealed class PickupItemTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private GameObject _pickupObject;
        private PickupItem _pickup;
        private InventoryService _inventory;
        private ItemDefinition _key;
        private InteractionContext _context;

        [SetUp]
        public void SetUp()
        {
            _inventory = new InventoryService(new NullPublisher<ItemAddedMessage>(), new NullPublisher<ItemRemovedMessage>());
            _key = ScriptableObject.CreateInstance<ItemDefinition>();

            _pickupObject = new GameObject("Pickup");
            _pickupObject.AddComponent<BoxCollider>();
            _pickup = _pickupObject.AddComponent<PickupItem>();
            _pickup.Construct(_inventory);
            _context = new InteractionContext(null, null, default);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_pickupObject);
            Object.DestroyImmediate(_key);
        }

        [Test]
        public void ItemWithRoom_CanBePickedUp()
        {
            SetItem(_key);

            Assert.That(_pickup.CanInteract(_context), Is.True);
        }

        [Test]
        public void UnassignedItem_IsBlocked()
        {
            Assert.That(_pickup.CanInteract(_context), Is.False);
            Assert.That(_pickup.Interact(_context).Succeeded, Is.False);
        }

        [Test]
        public void FullInventory_RejectsPickup_AndObjectStays()
        {
            SetItem(_key);
            _inventory.TryAdd(_key);

            InteractionResult result = _pickup.Interact(_context);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(_pickupObject.GetComponent<Collider>().enabled, Is.True);
            Assert.That(_inventory.Count(_key), Is.EqualTo(1));
        }

        [Test]
        public void WithoutInjection_IsBlocked()
        {
            SetItem(_key);
            _pickup.Construct(null);

            Assert.That(_pickup.CanInteract(_context), Is.False);
        }

        private void SetItem(ItemDefinition item)
        {
            typeof(PickupItem).GetField("item", Private).SetValue(_pickup, item);
        }

        private sealed class NullPublisher<T> : IPublisher<T>
        {
            public void Publish(T message)
            {
            }
        }
    }
}
