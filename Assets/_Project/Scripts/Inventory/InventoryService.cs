using System;
using System.Collections.Generic;
using MessagePipe;
using TheShadowWood.Core.Items;

namespace TheShadowWood.Inventory
{
    public sealed class InventoryService : IInventoryService
    {
        private readonly List<ItemStack> _items = new List<ItemStack>();
        private readonly IPublisher<ItemAddedMessage> _added;
        private readonly IPublisher<ItemRemovedMessage> _removed;

        public InventoryService(IPublisher<ItemAddedMessage> added, IPublisher<ItemRemovedMessage> removed)
        {
            _added = added;
            _removed = removed;
        }

        public IReadOnlyList<ItemStack> Items => _items;

        public int Count(ItemDefinition item)
        {
            int index = IndexOf(item);
            return index < 0 ? 0 : _items[index].Count;
        }

        public bool Has(ItemDefinition item, int count = 1)
        {
            return item != null && Count(item) >= count;
        }

        public bool TryAdd(ItemDefinition item, int count = 1)
        {
            Validate(item, count);

            int index = IndexOf(item);
            int total = (index < 0 ? 0 : _items[index].Count) + count;
            if (total > item.MaxCount)
            {
                return false;
            }

            if (index < 0)
            {
                _items.Add(new ItemStack(item, total));
            }
            else
            {
                _items[index] = new ItemStack(item, total);
            }

            _added.Publish(new ItemAddedMessage(item, count, total));
            return true;
        }

        public bool TryRemove(ItemDefinition item, int count = 1)
        {
            Validate(item, count);

            int index = IndexOf(item);
            if (index < 0 || _items[index].Count < count)
            {
                return false;
            }

            int total = _items[index].Count - count;
            if (total == 0)
            {
                _items.RemoveAt(index);
            }
            else
            {
                _items[index] = new ItemStack(item, total);
            }

            _removed.Publish(new ItemRemovedMessage(item, count, total));
            return true;
        }

        private int IndexOf(ItemDefinition item)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Item == item)
                {
                    return i;
                }
            }

            return -1;
        }

        // Unassigned item or bad count is a setup bug, same rule as FlagService.
        private static void Validate(ItemDefinition item, int count)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), "Item is not assigned.");
            }

            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be at least 1.");
            }
        }
    }
}
