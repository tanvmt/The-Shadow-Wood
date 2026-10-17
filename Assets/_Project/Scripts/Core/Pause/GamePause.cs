using System;
using UnityEngine;

namespace TheShadowWood.Core.Pause
{
    public sealed class GamePause : IGamePause, IDisposable
    {
        private int _count;
        private float _resumeTimeScale = 1f;

        public bool IsPaused => _count > 0;

        public IDisposable Pause()
        {
            if (_count == 0)
            {
                _resumeTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }

            _count++;
            return new Handle(this);
        }

        // Scene unload while paused must not leave the next scene frozen.
        public void Dispose()
        {
            if (_count > 0)
            {
                _count = 0;
                Resume();
            }
        }

        private void Release()
        {
            if (_count == 0)
            {
                return;
            }

            _count--;
            if (_count == 0)
            {
                Resume();
            }
        }

        private void Resume()
        {
            Time.timeScale = _resumeTimeScale;
            AudioListener.pause = false;
        }

        private sealed class Handle : IDisposable
        {
            private GamePause _owner;

            public Handle(GamePause owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                GamePause owner = _owner;
                _owner = null;
                owner?.Release();
            }
        }
    }
}
