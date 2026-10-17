using System;

namespace TheShadowWood.Core.Pause
{
    public interface IGamePause
    {
        bool IsPaused { get; }

        // Game stays paused until every returned handle is disposed.
        IDisposable Pause();
    }
}
