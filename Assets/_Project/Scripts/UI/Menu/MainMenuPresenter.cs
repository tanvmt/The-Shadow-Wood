using System;
using Cysharp.Threading.Tasks;
using TheShadowWood.Core.Input;
using TheShadowWood.Core.Scenes;
using UnityEngine;
using VContainer.Unity;

namespace TheShadowWood.UI
{
    public sealed class MainMenuPresenter : IStartable, IDisposable
    {
        private readonly MainMenuView _view;
        private readonly ISceneLoader _sceneLoader;
        private readonly IInputService _input;
        private IDisposable _uiContext;

        public MainMenuPresenter(MainMenuView view, ISceneLoader sceneLoader, IInputService input)
        {
            _view = view;
            _sceneLoader = sceneLoader;
            _input = input;
        }

        public void Start()
        {
            // Free the cursor and block gameplay actions while the menu is open.
            _uiContext = _input.PushContext(InputContext.UI);

            _view.StartButton.onClick.AddListener(OnStartClicked);
            _view.QuitButton.onClick.AddListener(OnQuitClicked);
        }

        public void Dispose()
        {
            _uiContext?.Dispose();
            _uiContext = null;

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

            bool loaded = await _sceneLoader.LoadAsync(SceneId.Gameplay);
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
