using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheShadowWood.Core.Cameras;
using TheShadowWood.Core.Input;
using Unity.Cinemachine;
using UnityEngine;

namespace TheShadowWood.Player
{
    public sealed class CameraFocusService : ICameraFocusService, IDisposable
    {
        private readonly CameraRig _rig;
        private readonly IInputService _input;
        private IDisposable _focusContext;

        public CameraFocusService(CameraRig rig, IInputService input)
        {
            _rig = rig;
            _input = input;
        }

        public bool IsFocused { get; private set; }

        public async UniTask FocusAsync(Transform pose, float blendSeconds = -1f, CancellationToken cancellationToken = default)
        {
            if (pose == null)
            {
                throw new ArgumentNullException(nameof(pose));
            }

            _focusContext ??= _input.PushContext(InputContext.Focus);
            IsFocused = true;

            CinemachineCamera focusCamera = _rig.FocusCamera;
            focusCamera.transform.SetPositionAndRotation(pose.position, pose.rotation);
            SetBlend(blendSeconds);
            SetFocusPriority(above: true);

            await WaitForBlendAsync(cancellationToken);
        }

        public async UniTask ReleaseAsync(float blendSeconds = -1f, CancellationToken cancellationToken = default)
        {
            if (!IsFocused)
            {
                return;
            }

            IsFocused = false;
            SetBlend(blendSeconds);
            SetFocusPriority(above: false);

            try
            {
                await WaitForBlendAsync(cancellationToken);
            }
            finally
            {
                // Release even when cancelled.
                ReleaseContext();
            }
        }

        public void Dispose()
        {
            ReleaseContext();
        }

        private void SetFocusPriority(bool above)
        {
            _rig.FocusCamera.Priority = _rig.FpsCamera.Priority.Value + (above ? 1 : -1);
        }

        private void SetBlend(float blendSeconds)
        {
            float seconds = blendSeconds < 0f ? _rig.DefaultFocusBlend : blendSeconds;
            CinemachineBlendDefinition.Styles style = seconds > 0f
                ? CinemachineBlendDefinition.Styles.EaseInOut
                : CinemachineBlendDefinition.Styles.Cut;

            _rig.Brain.DefaultBlend = new CinemachineBlendDefinition(style, seconds);
        }

        private async UniTask WaitForBlendAsync(CancellationToken cancellationToken)
        {
            CinemachineBrain brain = _rig.Brain;

            // Brain applies the priority change on its next LateUpdate.
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cancellationToken);

            while (brain != null && brain.IsBlending)
            {
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cancellationToken);
            }
        }

        private void ReleaseContext()
        {
            _focusContext?.Dispose();
            _focusContext = null;
        }
    }
}
