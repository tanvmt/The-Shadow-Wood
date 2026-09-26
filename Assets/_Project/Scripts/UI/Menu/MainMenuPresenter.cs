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
        private readonly ISceneLoader _sceneLoader;

        public MainMenuPresenter(MainMenuView view, ISceneLoader sceneLoader)
        {
            _view = view;
            _sceneLoader = sceneLoader;
        }

        public void Start()
        {
            _view.StartButton.onClick.AddListener(OnStartClicked);
            _view.QuitButton.onClick.AddListener(OnQuitClicked);
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
