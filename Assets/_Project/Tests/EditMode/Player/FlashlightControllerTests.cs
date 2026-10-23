using System.Collections.Generic;
using NUnit.Framework;
using TheShadowWood.Core.Audio;
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
        private FakeAudioService _audio;
        private FlashlightController _flashlight;

        [SetUp]
        public void SetUp()
        {
            _input = new FakeInputService();
            _inventory = new FakeInventory();
            _audio = new FakeAudioService();
            _view = new FakeFlashlightView
            {
                RequiredItem = ScriptableObject.CreateInstance<ItemDefinition>(),
                SwitchCue = ScriptableObject.CreateInstance<AudioCue>()
            };
            _flashlight = new FlashlightController(_view, _input, _inventory, _audio);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_view.RequiredItem);
            Object.DestroyImmediate(_view.SwitchCue);
        }

        [Test]
        public void WithoutItem_PressDoesNothing()
        {
            Press();

            Assert.That(_flashlight.IsOn, Is.False);
            Assert.That(_view.Calls, Is.Empty);
            Assert.That(_audio.Plays, Is.Empty);
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
            Assert.That(_audio.Plays, Has.Count.EqualTo(2));
            Assert.That(_audio.Plays[0].Cue, Is.SameAs(_view.SwitchCue));
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

        [Test]
        public void PickingUpItem_ShowsModel()
        {
            _flashlight.Tick();
            Assert.That(_view.IsHeld, Is.False);

            _inventory.Owned.Add(_view.RequiredItem);
            _flashlight.Tick();

            Assert.That(_flashlight.IsHeld, Is.True);
            Assert.That(_view.HeldCalls, Is.EqualTo(new[] { true }));
        }

        [Test]
        public void LosingItem_HidesModelAndTurnsOff()
        {
            _inventory.Owned.Add(_view.RequiredItem);
            Press();

            _inventory.Owned.Remove(_view.RequiredItem);
            _input.FlashlightPressed = false;
            _flashlight.Tick();

            Assert.That(_view.IsHeld, Is.False);
            Assert.That(_flashlight.IsOn, Is.False);
            Assert.That(_view.IsOn, Is.False);
            Assert.That(_audio.Plays, Has.Count.EqualTo(1));
        }

        private void Press()
        {
            _input.FlashlightPressed = true;
            _flashlight.Tick();
        }

        private sealed class FakeFlashlightView : IFlashlightView
        {
            public ItemDefinition RequiredItem { get; set; }
            public AudioCue SwitchCue { get; set; }
            public bool IsHeld { get; private set; }
            public bool IsOn { get; private set; }
            public List<bool> Calls { get; } = new List<bool>();
            public List<bool> HeldCalls { get; } = new List<bool>();

            public void SetHeld(bool held)
            {
                IsHeld = held;
                HeldCalls.Add(held);
            }

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
