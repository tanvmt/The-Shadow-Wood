namespace TheShadowWood.Core.Items
{
    public readonly struct ItemAddedMessage
    {
        public ItemDefinition Item { get; }
        public int Count { get; }
        public int Total { get; }

        public ItemAddedMessage(ItemDefinition item, int count, int total)
        {
            Item = item;
            Count = count;
            Total = total;
        }
    }
}
