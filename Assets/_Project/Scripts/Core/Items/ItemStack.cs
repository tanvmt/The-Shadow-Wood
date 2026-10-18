namespace TheShadowWood.Core.Items
{
    public readonly struct ItemStack
    {
        public ItemDefinition Item { get; }
        public int Count { get; }

        public ItemStack(ItemDefinition item, int count)
        {
            Item = item;
            Count = count;
        }
    }
}
