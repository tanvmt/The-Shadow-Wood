using System;
using System.Collections.Generic;
using MessagePipe;

namespace TheShadowWood.Core.Flags
{
    public sealed class FlagService : IFlagService
    {
        private readonly HashSet<FlagId> _flags = new HashSet<FlagId>();
        private readonly IPublisher<FlagChangedMessage> _changed;

        public FlagService(IPublisher<FlagChangedMessage> changed)
        {
            _changed = changed;
        }

        public bool Has(FlagId flag)
        {
            return flag != null && _flags.Contains(flag);
        }

        public void Set(FlagId flag)
        {
            if (_flags.Add(Require(flag)))
            {
                _changed.Publish(new FlagChangedMessage(flag, true));
            }
        }

        public void Clear(FlagId flag)
        {
            if (_flags.Remove(Require(flag)))
            {
                _changed.Publish(new FlagChangedMessage(flag, false));
            }
        }

        // An unassigned flag field is a setup bug; fail loudly instead of silently skipping the beat.
        private static FlagId Require(FlagId flag)
        {
            if (flag == null)
            {
                throw new ArgumentNullException(nameof(flag), "Flag is not assigned.");
            }

            return flag;
        }
    }
}
