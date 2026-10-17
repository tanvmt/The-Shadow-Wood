using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheShadowWood.Core.Cameras;
using TheShadowWood.Core.Input;
using UnityEngine;
using UnityEngine.Video;
using VContainer;

namespace TheShadowWood.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VideoPlayer))]
    public sealed class TVInteractable : InteractableBehaviour
    {
        [Header("TV Screen")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Renderer screenRenderer;
        [Tooltip("Zero-based material slot used by the TV screen (for example, 1 is the second material).")]
        [SerializeField, Min(0)] private int screenMaterialIndex;
        [SerializeField] private string screenTextureProperty = "_BaseMap";
        [Tooltip("Optional RenderTexture asset for the video. A temporary one is created if this is empty.")]
        [SerializeField] private RenderTexture videoRenderTexture;
        [Tooltip("Scale for the video texture (Tiling).")]
        [SerializeField] private Vector2 videoScale = Vector2.one;
        [Tooltip("Offset for the video texture.")]
        [SerializeField] private Vector2 videoOffset = Vector2.zero;
        [Tooltip("Enable to flip the video texture horizontally.")]
        [SerializeField] private bool flipVideoHorizontally = false;
        [Tooltip("Enable when the platform renders this video's RenderTexture upside down.")]
        [SerializeField] private bool flipVideoVertically = true;
        [Tooltip("Texture displayed when the TV is off. Leave empty to restore the original texture.")]
        [SerializeField] private Texture offScreenTexture;

        [Header("Screen Emission")]
        [Tooltip("Makes the video visible even when the TV screen receives no scene light.")]
        [SerializeField] private bool useEmissionForVideo = true;
        [SerializeField] private string emissionTextureProperty = "_EmissionMap";
        [ColorUsage(true, true)] [SerializeField] private Color emissionColor = Color.white;

        [Header("Camera")]
        [Tooltip("An empty transform in front of the TV, rotated toward its screen.")]
        [SerializeField] private Transform viewingCameraPose;
        [Tooltip("Camera blend in seconds. Negative uses the CameraRig default.")]
        [SerializeField] private float focusBlend = -1f;

        [Header("Flow Delays")]
        [Tooltip("Delay after the camera finishes focusing the TV and before the video starts playing.")]
        [SerializeField, Min(0f)] private float focusToPlayDelay = 0f;
        [Tooltip("Delay after the video finishes and before the camera leaves the TV focus.")]
        [SerializeField, Min(0f)] private float videoEndToUnfocusDelay = 0f;

        private ICameraFocusService _cameraFocus;
        private IInputService _input;

        private Material _screenMaterial;
        private Texture _originalScreenTexture;
        private Texture _originalEmissionTexture;
        private Vector2 _originalScreenTextureScale;
        private Vector2 _originalScreenTextureOffset;
        private Vector2 _originalEmissionTextureScale;
        private Vector2 _originalEmissionTextureOffset;
        private Color _originalEmissionColor;
        private bool _emissionWasEnabled;
        private bool _ownsVideoRenderTexture;
        private CancellationTokenSource _watchCts;
        private bool _isWatching;
        private bool _videoFinished;
        private bool _cancelRequested;

        [Inject]
        public void Construct(ICameraFocusService cameraFocus, IInputService input)
        {
            _cameraFocus = cameraFocus;
            _input = input;
        }

        protected override void Awake()
        {
            base.Awake();

            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
            }

            ConfigureVideoOutput();
            CacheOriginalScreenTexture();
            TurnScreenOff();
        }

        private void OnEnable()
        {
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached += HandleVideoFinished;
            }
        }

        protected override void OnDisable()
        {
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= HandleVideoFinished;
            }

            _watchCts?.Cancel();
            base.OnDisable();
        }

        private void OnDestroy()
        {
            _watchCts?.Cancel();
            _watchCts?.Dispose();
            _watchCts = null;

            if (_ownsVideoRenderTexture && videoRenderTexture != null)
            {
                videoRenderTexture.Release();
                Destroy(videoRenderTexture);
            }
        }

        // Hook for an on-screen Cancel button.
        public void CancelWatching()
        {
            _cancelRequested = true;
        }

        public override bool CanInteract(InteractionContext context)
        {
            return !_isWatching
                   && _cameraFocus != null
                   && videoPlayer != null
                   && viewingCameraPose != null
                   && base.CanInteract(context);
        }

        protected override InteractionResult PerformInteraction(InteractionContext context)
        {
            _watchCts?.Dispose();
            _watchCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            WatchAsync(_watchCts.Token).Forget();
            return InteractionResult.Success();
        }

        private async UniTaskVoid WatchAsync(CancellationToken ct)
        {
            _isWatching = true;
            _videoFinished = false;
            _cancelRequested = false;

            try
            {
                await _cameraFocus.FocusAsync(viewingCameraPose, focusBlend, ct);

                if (focusToPlayDelay > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(focusToPlayDelay), ignoreTimeScale: true, cancellationToken: ct);
                }

                TurnScreenOn();
                if (!videoPlayer.isPrepared)
                {
                    videoPlayer.Prepare();
                    await UniTask.WaitUntil(() => videoPlayer.isPrepared || IsCancelRequested(), cancellationToken: ct);
                }

                if (!_cancelRequested)
                {
                    videoPlayer.Play();
                    await UniTask.WaitUntil(() => _videoFinished || IsCancelRequested(), cancellationToken: ct);
                }

                if (!_cancelRequested && videoEndToUnfocusDelay > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(videoEndToUnfocusDelay), ignoreTimeScale: true, cancellationToken: ct);
                }

                StopVideo();
                await _cameraFocus.ReleaseAsync(focusBlend, ct);
            }
            catch (OperationCanceledException)
            {
                StopVideo();
                // Cut back so the Focus context never outlives the TV.
                _cameraFocus.ReleaseAsync(0f).Forget();
            }
            finally
            {
                _isWatching = false;
            }
        }

        private bool IsCancelRequested()
        {
            if (_input.CancelPressed)
            {
                _cancelRequested = true;
            }

            return _cancelRequested;
        }

        private void HandleVideoFinished(VideoPlayer source)
        {
            _videoFinished = true;
        }

        private void StopVideo()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }

            TurnScreenOff();
        }

        private void ConfigureVideoOutput()
        {
            if (videoPlayer == null)
            {
                return;
            }

            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = GetOrCreateVideoRenderTexture();
        }

        private void CacheOriginalScreenTexture()
        {
            if (screenRenderer == null)
            {
                return;
            }

            Material[] materials = screenRenderer.materials;
            if (screenMaterialIndex >= materials.Length)
            {
                Debug.LogError(
                    $"{nameof(TVInteractable)} on {name} has no material at index {screenMaterialIndex}.",
                    this);
                return;
            }

            _screenMaterial = materials[screenMaterialIndex];
            if (_screenMaterial != null && _screenMaterial.HasProperty(screenTextureProperty))
            {
                _originalScreenTexture = _screenMaterial.GetTexture(screenTextureProperty);
                _originalScreenTextureScale = _screenMaterial.GetTextureScale(screenTextureProperty);
                _originalScreenTextureOffset = _screenMaterial.GetTextureOffset(screenTextureProperty);
            }

            if (_screenMaterial != null && _screenMaterial.HasProperty(emissionTextureProperty))
            {
                _originalEmissionTexture = _screenMaterial.GetTexture(emissionTextureProperty);
                _originalEmissionTextureScale = _screenMaterial.GetTextureScale(emissionTextureProperty);
                _originalEmissionTextureOffset = _screenMaterial.GetTextureOffset(emissionTextureProperty);
                _originalEmissionColor = _screenMaterial.GetColor("_EmissionColor");
                _emissionWasEnabled = _screenMaterial.IsKeywordEnabled("_EMISSION");
            }
        }

        private void TurnScreenOff()
        {
            if (offScreenTexture != null)
            {
                SetScreenTexture(offScreenTexture);
                return;
            }

            SetScreenTexture(_originalScreenTexture);
            RestoreTextureTransforms();
            RestoreEmission();
        }

        private void TurnScreenOn()
        {
            SetScreenTexture(videoRenderTexture);
            ApplyVideoTextureTransforms();
        }

        private void SetScreenTexture(Texture texture)
        {
            if (_screenMaterial == null || !_screenMaterial.HasProperty(screenTextureProperty))
            {
                return;
            }

            _screenMaterial.SetTexture(screenTextureProperty, texture);

            if (useEmissionForVideo && _screenMaterial.HasProperty(emissionTextureProperty))
            {
                _screenMaterial.EnableKeyword("_EMISSION");
                _screenMaterial.SetTexture(emissionTextureProperty, texture);
                _screenMaterial.SetColor("_EmissionColor", emissionColor);
            }
        }

        private void RestoreEmission()
        {
            if (_screenMaterial == null || !_screenMaterial.HasProperty(emissionTextureProperty))
            {
                return;
            }

            _screenMaterial.SetTexture(emissionTextureProperty, _originalEmissionTexture);
            _screenMaterial.SetColor("_EmissionColor", _originalEmissionColor);
            if (_emissionWasEnabled)
            {
                _screenMaterial.EnableKeyword("_EMISSION");
            }
            else
            {
                _screenMaterial.DisableKeyword("_EMISSION");
            }
        }

        private void ApplyVideoTextureTransforms()
        {
            if (_screenMaterial == null)
            {
                return;
            }

            float scaleX = flipVideoHorizontally ? -videoScale.x : videoScale.x;
            float offsetX = flipVideoHorizontally ? videoOffset.x + videoScale.x : videoOffset.x;
            float scaleY = flipVideoVertically ? -videoScale.y : videoScale.y;
            float offsetY = flipVideoVertically ? videoOffset.y + videoScale.y : videoOffset.y;

            Vector2 scale = new Vector2(scaleX, scaleY);
            Vector2 offset = new Vector2(offsetX, offsetY);
            SetTextureTransform(screenTextureProperty, scale, offset);
            if (useEmissionForVideo)
            {
                SetTextureTransform(emissionTextureProperty, scale, offset);
            }
        }

        private void RestoreTextureTransforms()
        {
            SetTextureTransform(screenTextureProperty, _originalScreenTextureScale, _originalScreenTextureOffset);
            SetTextureTransform(emissionTextureProperty, _originalEmissionTextureScale, _originalEmissionTextureOffset);
        }

        private void SetTextureTransform(string propertyName, Vector2 scale, Vector2 offset)
        {
            if (_screenMaterial == null || !_screenMaterial.HasProperty(propertyName))
            {
                return;
            }

            _screenMaterial.SetTextureScale(propertyName, scale);
            _screenMaterial.SetTextureOffset(propertyName, offset);
        }

        private RenderTexture GetOrCreateVideoRenderTexture()
        {
            if (videoRenderTexture != null)
            {
                return videoRenderTexture;
            }

            videoRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32)
            {
                name = $"{name}_TVVideo",
            };
            videoRenderTexture.Create();
            _ownsVideoRenderTexture = true;
            return videoRenderTexture;
        }
    }
}
