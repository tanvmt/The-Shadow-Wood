using System.Collections.Generic;
using NUnit.Framework;
using TheShadowWood.Core.Items;
using TheShadowWood.Interaction.Tests;
using UnityEngine;

namespace TheShadowWood.Player.Tests
{
    public sealed class FlashlightControllerTests
    {
        private FakeInputService _input;
        private FakeInventory _inventory;
        private FakeFlashlightView _view;
        private FlashlightController _flashlight;

        [SetUp]
        public void SetUp()
        {
            _input = new FakeInputService();
            _inventory = new FakeInventory();
            _view = new FakeFlashlightView { RequiredItem = ScriptableObject.CreateInstance<ItemDefinition>() };
            _flashlight = new FlashlightController(_view, _input, _inventory);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_view.RequiredItem);
        }

        [Test]
        public void WithoutItem_PressDoesNothing()
        {
            Press();

            Assert.That(_flashlight.IsOn, Is.False);
            Assert.That(_view.Calls, Is.Empty);
        }

        [Test]
        public void WithItem_PressTogglesOnThenOff()
        {
            _inventory.Owned.Add(_view.RequiredItem);

            Press();
            Assert.That(_flashlight.IsOn, Is.True);
            Assert.That(_view.IsOn, Is.True);

            Press();
            Assert.That(_flashlight.IsOn, Is.False);
            Assert.That(_view.IsOn, Is.False);
        }

        [Test]
        public void NoPress_KeepsState()
        {
            _inventory.Owned.Add(_view.RequiredItem);
            Press();

            _input.FlashlightPressed = false;
            _flashlight.Tick();

            Assert.That(_flashlight.IsOn, Is.True);
            Assert.That(_view.Calls, Has.Count.EqualTo(1));
        }

        private void Press()
        {
            _input.FlashlightPressed = true;
            _flashlight.Tick();
        }

        private sealed class FakeFlashlightView : IFlashlightView
        {
            public ItemDefinition RequiredItem { get; set; }
            public bool IsOn { get; private set; }
            public List<bool> Calls { get; } = new List<bool>();

            public void SetOn(bool on)
            {
                IsOn = on;
                Calls.Add(on);
            }
        }

        private sealed class FakeInventory : IInventoryService
        {
            public HashSet<ItemDefinition> Owned { get; } = new HashSet<ItemDefinition>();

            public IReadOnlyList<ItemStack> Items => new List<ItemStack>();
            public int Count(ItemDefinition item) => item != null && Owned.Contains(item) ? 1 : 0;
            public bool Has(ItemDefinition item, int count = 1) => Count(item) >= count;
            public bool TryAdd(ItemDefinition item, int count = 1) => Owned.Add(item);
            public bool TryRemove(ItemDefinition item, int count = 1) => Owned.Remove(item);
        }
    }
}
