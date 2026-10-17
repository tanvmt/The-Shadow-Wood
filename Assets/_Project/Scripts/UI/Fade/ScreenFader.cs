using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheShadowWood.Core.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TheShadowWood.UI
{
    // Builds its own overlay canvas on first use so it survives scene loads without a prefab.
    public sealed class ScreenFader : IScreenFader, IDisposable
    {
        private const int SortingOrder = 1000;

        // A hitch frame (e.g. right after a scene load) slows the fade instead of skipping it.
        private const float MaxFadeStep = 1f / 30f;

        private GameObject _root;
        private CanvasGroup _group;

        public float Alpha => _group != null ? _group.alpha : 0f;

        public void SetAlpha(float alpha)
        {
            CanvasGroup group = GetGroup();
            group.alpha = Mathf.Clamp01(alpha);

            // Swallow clicks while any black is on screen, e.g. a second Start click mid-transition.
            group.blocksRaycasts = group.alpha > 0f;
        }

        public async UniTask FadeToAsync(float alpha, float duration, CancellationToken cancellationToken = default)
        {
            float target = Mathf.Clamp01(alpha);
            float start = Alpha;
            if (Mathf.Approximately(start, target) || duration <= 0f)
            {
                SetAlpha(target);
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFadeStep);
                SetAlpha(Mathf.Lerp(start, target, elapsed / duration));
            }

            SetAlpha(target);
        }

        public void Dispose()
        {
            if (_root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(_root);
            }
            else
            {
                Object.DestroyImmediate(_root);
            }

            _root = null;
            _group = null;
        }

        private CanvasGroup GetGroup()
        {
            if (_group == null)
            {
                Build();
            }

            return _group;
        }

        private void Build()
        {
            _root = new GameObject("ScreenFader", typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(_root);
            }

            Canvas canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            _group = _root.GetComponent<CanvasGroup>();
            _group.interactable = false;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            GameObject overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            RectTransform rect = overlay.GetComponent<RectTransform>();
            rect.SetParent(_root.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = Color.black;
        }
    }
}
