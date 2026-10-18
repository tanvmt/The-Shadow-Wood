namespace TheShadowWood.Core.Items
{
    public readonly struct ItemRemovedMessage
    {
        public ItemDefinition Item { get; }
        public int Count { get; }
        public int Total { get; }

        public ItemRemovedMessage(ItemDefinition item, int count, int total)
        {
            Item = item;
            Count = count;
            Total = total;
        }
    }
}
