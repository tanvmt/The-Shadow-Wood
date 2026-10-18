using System;
using MessagePipe;
using TheShadowWood.Core.Items;
using TheShadowWood.Core.UI;
using VContainer.Unity;

namespace TheShadowWood.UI
{
    public sealed class ItemPickupNotifier : IStartable, IDisposable
    {
        private const float Duration = 2.5f;

        private readonly ISubscriber<ItemAddedMessage> _itemAdded;
        private readonly ISubtitleService _subtitles;
        private IDisposable _subscription;

        public ItemPickupNotifier(ISubscriber<ItemAddedMessage> itemAdded, ISubtitleService subtitles)
        {
            _itemAdded = itemAdded;
            _subtitles = subtitles;
        }

        public void Start()
        {
            _subscription = _itemAdded.Subscribe(OnItemAdded);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }

        private void OnItemAdded(ItemAddedMessage message)
        {
            _subtitles.Show(Format(message), Duration);
        }

        public static string Format(ItemAddedMessage message)
        {
            string name = message.Item.DisplayName;
            return message.Count > 1 ? $"Picked up: {name} x{message.Count}" : $"Picked up: {name}";
        }
    }
}
