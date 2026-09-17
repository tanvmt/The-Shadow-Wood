using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TheShadowWood.Interaction
{
    /// <summary>
    /// Plays a TV video while temporarily taking control of the gameplay camera.
    /// Assign a transform placed at the desired viewing position and, when using
    /// Cinemachine, assign its active camera component as Camera Driver To Disable.
    /// </summary>
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
        [Tooltip("For Starter Assets, drag PlayerFollowCamera's CinemachineCamera here.")]
        [SerializeField] private Behaviour cameraDriverToDisable;
        [SerializeField, Min(0f)] private float cameraTransitionDuration = 0.35f;
        [SerializeField] private Ease cameraTransitionEase = Ease.InOutSine;
        [Tooltip("Components such as FirstPersonController to disable while watching.")]
        [SerializeField] private Behaviour[] playerControllersToDisable = new Behaviour[0];

        [Header("Flow Delays")]
        [Tooltip("Delay after the camera finishes focusing the TV and before the video starts playing.")]
        [SerializeField, Min(0f)] private float focusToPlayDelay = 0f;
        [Tooltip("Delay after the video finishes and before the camera leaves the TV focus.")]
        [SerializeField, Min(0f)] private float videoEndToUnfocusDelay = 0f;

        [Header("Cancel")]
        [Tooltip("Escape always exits on keyboard. A UI Button may call CancelWatching.")]
        [SerializeField] private bool allowGamepadCancel = true;

        private Camera _camera;
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
        private Vector3 _cameraPositionBeforeWatching;
        private Quaternion _cameraRotationBeforeWatching;
        private bool _cameraDriverWasEnabled;
        private bool[] _controllersWereEnabled;
        private bool _isWatching;
        private Coroutine _transition;

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

            StopWatchingImmediately();
            base.OnDisable();
        }

        private void Update()
        {
            if (_isWatching && WasCancelPressed())
            {
                CancelWatching();
            }
        }

        /// <summary>Connect this method to an on-screen Cancel button if the game has one.</summary>
        public void CancelWatching()
        {
            StopWatching();
        }

        public override bool CanInteract(InteractionContext context)
        {
            return !_isWatching
                   && videoPlayer != null
                   && viewingCameraPose != null
                   && base.CanInteract(context);
        }

        protected override InteractionResult PerformInteraction(InteractionContext context)
        {
            if (context.Camera == null)
            {
                return InteractionResult.Rejected("No interaction camera is available.");
            }

            _camera = context.Camera;
            _cameraPositionBeforeWatching = _camera.transform.position;
            _cameraRotationBeforeWatching = _camera.transform.rotation;
            _isWatching = true;
            SetFocused(false, context);
            SetPlayerControlEnabled(false);

            if (_transition != null)
            {
                StopCoroutine(_transition);
            }

            _transition = StartCoroutine(MoveCameraThenPlay());
            return InteractionResult.Success();
        }

        private IEnumerator MoveCameraThenPlay()
        {
            SetCameraDriverEnabled(false);
            Tween moveTween = MoveCamera(viewingCameraPose.position, viewingCameraPose.rotation);
            if (moveTween != null)
            {
                yield return moveTween.WaitForCompletion();
            }

            if (focusToPlayDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(focusToPlayDelay);
            }

            TurnScreenOn();
            if (!videoPlayer.isPrepared)
            {
                videoPlayer.Prepare();
                while (!videoPlayer.isPrepared)
                {
                    yield return null;
                }
            }

            videoPlayer.Play();
            _transition = null;
        }

        private void HandleVideoFinished(VideoPlayer source)
        {
            StartCoroutine(DelayedStopWatching());
        }

        private IEnumerator DelayedStopWatching()
        {
            if (videoEndToUnfocusDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(videoEndToUnfocusDelay);
            }

            StopWatching();
        }

        private void StopWatching()
        {
            if (!_isWatching)
            {
                return;
            }

            _isWatching = false;
            videoPlayer.Stop();
            TurnScreenOff();

            if (_transition != null)
            {
                StopCoroutine(_transition);
            }

            _transition = StartCoroutine(ReturnCameraAndPlayerControl());
        }

        private IEnumerator ReturnCameraAndPlayerControl()
        {
            if (_camera != null)
            {
                Tween moveTween = MoveCamera(_cameraPositionBeforeWatching, _cameraRotationBeforeWatching);
                if (moveTween != null)
                {
                    yield return moveTween.WaitForCompletion();
                }
            }

            SetCameraDriverEnabled(_cameraDriverWasEnabled);
            SetPlayerControlEnabled(true);
            _transition = null;
        }

        private void StopWatchingImmediately()
        {
            if (!_isWatching && _transition == null)
            {
                return;
            }

            _isWatching = false;
            if (_transition != null)
            {
                StopCoroutine(_transition);
                _transition = null;
            }

            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }

            TurnScreenOff();

            if (_camera != null)
            {
                _camera.transform.DOKill();
                _camera.transform.SetPositionAndRotation(_cameraPositionBeforeWatching, _cameraRotationBeforeWatching);
            }

            SetCameraDriverEnabled(_cameraDriverWasEnabled);
            SetPlayerControlEnabled(true);
        }

        private void OnDestroy()
        {
            if (_ownsVideoRenderTexture && videoRenderTexture != null)
            {
                videoRenderTexture.Release();
                Destroy(videoRenderTexture);
            }
        }

        /// <summary>Starts (and returns) a tween moving the camera to the given pose, or null if it snapped instantly.</summary>
        private Tween MoveCamera(Vector3 toPosition, Quaternion toRotation)
        {
            if (_camera == null)
            {
                return null;
            }

            _camera.transform.DOKill();

            if (cameraTransitionDuration <= 0f)
            {
                _camera.transform.SetPositionAndRotation(toPosition, toRotation);
                return null;
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(_camera.transform.DOMove(toPosition, cameraTransitionDuration).SetEase(cameraTransitionEase));
            sequence.Join(_camera.transform.DORotateQuaternion(toRotation, cameraTransitionDuration).SetEase(cameraTransitionEase));
            return sequence;
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

        private void SetCameraDriverEnabled(bool enabled)
        {
            if (cameraDriverToDisable == null)
            {
                return;
            }

            if (!enabled)
            {
                _cameraDriverWasEnabled = cameraDriverToDisable.enabled;
            }

            cameraDriverToDisable.enabled = enabled;
        }

        private void SetPlayerControlEnabled(bool enabled)
        {
            if (!enabled)
            {
                _controllersWereEnabled = new bool[playerControllersToDisable.Length];
                for (int i = 0; i < playerControllersToDisable.Length; i++)
                {
                    Behaviour controller = playerControllersToDisable[i];
                    if (controller == null)
                    {
                        continue;
                    }

                    _controllersWereEnabled[i] = controller.enabled;
                    controller.enabled = false;
                }

                return;
            }

            if (_controllersWereEnabled == null)
            {
                return;
            }

            for (int i = 0; i < playerControllersToDisable.Length; i++)
            {
                Behaviour controller = playerControllersToDisable[i];
                if (controller != null)
                {
                    controller.enabled = _controllersWereEnabled[i];
                }
            }

            _controllersWereEnabled = null;
        }

        private bool WasCancelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
            {
                return true;
            }

            return allowGamepadCancel
                   && Gamepad.current != null
                   && Gamepad.current.buttonEast.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private void OnValidate()
        {
            cameraTransitionDuration = Mathf.Max(0f, cameraTransitionDuration);
        }
    }
}
