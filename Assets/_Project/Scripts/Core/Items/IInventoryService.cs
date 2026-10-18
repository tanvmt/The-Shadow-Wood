using System.Collections.Generic;

namespace TheShadowWood.Core.Items
{
    public interface IInventoryService
    {
        // In pickup order.
        IReadOnlyList<ItemStack> Items { get; }

        int Count(ItemDefinition item);
        bool Has(ItemDefinition item, int count = 1);

        // False leaves the inventory unchanged: over MaxCount, or not enough to remove.
        bool TryAdd(ItemDefinition item, int count = 1);
        bool TryRemove(ItemDefinition item, int count = 1);
    }
}
