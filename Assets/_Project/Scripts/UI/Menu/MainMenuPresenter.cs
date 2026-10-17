using System;
using Cysharp.Threading.Tasks;
using TheShadowWood.Core.Scenes;
using UnityEngine;
using VContainer.Unity;

namespace TheShadowWood.UI
{
    public sealed class MainMenuPresenter : IStartable, IDisposable
    {
        private readonly MainMenuView _view;
        private readonly ISceneTransition _transition;
        private readonly IUIScreenService _screens;

        public MainMenuPresenter(MainMenuView view, ISceneTransition transition, IUIScreenService screens)
        {
            _view = view;
            _transition = transition;
            _screens = screens;
        }

        public void Start()
        {
            _view.StartButton.onClick.AddListener(OnStartClicked);
            _view.QuitButton.onClick.AddListener(OnQuitClicked);

            _screens.Open(_view);
        }

        public void Dispose()
        {
            if (_view == null)
            {
                return;
            }

            _view.StartButton.onClick.RemoveListener(OnStartClicked);
            _view.QuitButton.onClick.RemoveListener(OnQuitClicked);
        }

        private void OnStartClicked()
        {
            StartGameAsync().Forget();
        }

        private async UniTaskVoid StartGameAsync()
        {
            // Block repeated clicks while the next scene loads; the menu is unloaded on success.
            _view.SetInteractable(false);

            bool loaded = await _transition.LoadAsync(SceneId.Gameplay);
            if (!loaded && _view != null)
            {
                _view.SetInteractable(true);
            }
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
