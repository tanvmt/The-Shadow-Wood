using System;
using Cysharp.Threading.Tasks;
using TheShadowWood.Core.Input;
using TheShadowWood.Core.Pause;
using TheShadowWood.Core.Scenes;
using VContainer.Unity;

namespace TheShadowWood.UI
{
    public sealed class PausePresenter : IStartable, ITickable, IDisposable
    {
        private readonly PauseScreen _view;
        private readonly SettingsScreen _settings;
        private readonly IUIScreenService _screens;
        private readonly IInputService _input;
        private readonly IGamePause _pause;
        private readonly ISceneTransition _transition;

        private IDisposable _pauseHandle;

        public PausePresenter(
            PauseScreen view,
            SettingsScreen settings,
            IUIScreenService screens,
            IInputService input,
            IGamePause pause,
            ISceneTransition transition)
        {
            _view = view;
            _settings = settings;
            _screens = screens;
            _input = input;
            _pause = pause;
            _transition = transition;
        }

        public void Start()
        {
            _view.ResumeButton.onClick.AddListener(OnResumeClicked);
            _view.SettingsButton.onClick.AddListener(OnSettingsClicked);
            _view.MainMenuButton.onClick.AddListener(OnMainMenuClicked);
            _view.Shown += OnShown;
            _view.Hidden += OnHidden;
        }

        public void Tick()
        {
            // Pause is only enabled in Gameplay, so TV, puzzles and open screens never reach here.
            if (_input.PausePressed && !_screens.HasOpenScreen)
            {
                _screens.Open(_view);
            }
        }

        public void Dispose()
        {
            _pauseHandle?.Dispose();
            _pauseHandle = null;

            if (_view == null)
            {
                return;
            }

            _view.ResumeButton.onClick.RemoveListener(OnResumeClicked);
            _view.SettingsButton.onClick.RemoveListener(OnSettingsClicked);
            _view.MainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            _view.Shown -= OnShown;
            _view.Hidden -= OnHidden;
        }

        private void OnShown()
        {
            _pauseHandle ??= _pause.Pause();
        }

        private void OnHidden()
        {
            _pauseHandle?.Dispose();
            _pauseHandle = null;
        }

        private void OnResumeClicked()
        {
            _screens.Close(_view);
        }

        private void OnSettingsClicked()
        {
            _screens.Open(_settings);
        }

        private void OnMainMenuClicked()
        {
            LeaveToMenuAsync().Forget();
        }

        // Stays paused through the fade; GamePause is disposed with this scene and resumes time.
        private async UniTaskVoid LeaveToMenuAsync()
        {
            _view.SetButtonsEnabled(false);

            bool loaded = await _transition.LoadAsync(SceneId.Menu);
            if (!loaded && _view != null)
            {
                _view.SetButtonsEnabled(true);
            }
        }
    }
}
