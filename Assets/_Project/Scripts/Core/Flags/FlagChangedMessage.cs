namespace TheShadowWood.Core.Flags
{
    public readonly struct FlagChangedMessage
    {
        public FlagId Flag { get; }
        public bool Value { get; }

        public FlagChangedMessage(FlagId flag, bool value)
        {
            Flag = flag;
            Value = value;
        }
    }
}
