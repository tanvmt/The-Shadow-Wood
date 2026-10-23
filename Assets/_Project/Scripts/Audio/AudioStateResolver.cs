using System;
using System.Collections.Generic;
using TheShadowWood.Core.Flags;

namespace TheShadowWood.Audio
{
    public static class AudioStateResolver
    {
        // The lowest listed state whose flag is set wins.
        public static AudioState Resolve(IReadOnlyList<AudioState> states, AudioState fallback, Func<FlagId, bool> isSet)
        {
            for (int i = states.Count - 1; i >= 0; i--)
            {
                AudioState state = states[i];
                if (state != null && state.EnterFlag != null && isSet(state.EnterFlag))
                {
                    return state;
                }
            }

            return fallback;
        }
    }
}
