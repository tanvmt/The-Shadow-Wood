using NUnit.Framework;
using TheShadowWood.Core.Items;
using UnityEngine;

namespace TheShadowWood.UI.Tests
{
    public sealed class ItemPickupNotifierTests
    {
        private ItemDefinition _candle;

        [SetUp]
        public void SetUp()
        {
            _candle = ScriptableObject.CreateInstance<ItemDefinition>();
            _candle.name = "Candle";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_candle);
        }

        [Test]
        public void SingleItem_ShowsNameOnly()
        {
            string text = ItemPickupNotifier.Format(new ItemAddedMessage(_candle, 1, 1));

            Assert.That(text, Is.EqualTo("Picked up: Candle"));
        }

        [Test]
        public void SeveralItems_ShowsCount()
        {
            string text = ItemPickupNotifier.Format(new ItemAddedMessage(_candle, 2, 3));

            Assert.That(text, Is.EqualTo("Picked up: Candle x2"));
        }
    }
}
