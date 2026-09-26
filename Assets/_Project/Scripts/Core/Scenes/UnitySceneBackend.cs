using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheShadowWood.Core.Scenes
{
    public sealed class UnitySceneBackend : ISceneBackend
    {
        public bool CanLoad(string sceneName)
        {
            // False when the scene is missing or disabled in Build Settings.
            return Application.CanStreamedLevelBeLoaded(sceneName);
        }

        public UniTask LoadSingleAsync(string sceneName, IProgress<float> progress, CancellationToken cancellationToken)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                throw new InvalidOperationException($"Unity could not start loading scene '{sceneName}'.");
            }

            return operation.ToUniTask(progress, cancellationToken: cancellationToken);
        }
    }
}
