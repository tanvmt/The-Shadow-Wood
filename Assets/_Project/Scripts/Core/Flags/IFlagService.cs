namespace TheShadowWood.Core.Flags
{
    public interface IFlagService
    {
        bool Has(FlagId flag);

        // Publishes FlagChangedMessage only when the value actually changes.
        void Set(FlagId flag);
        void Clear(FlagId flag);
    }
}
