using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TheShadowWood.Core.Cameras
{
    public interface ICameraFocusService
    {
        bool IsFocused { get; }

        UniTask FocusAsync(Transform pose, float blendSeconds = -1f, CancellationToken cancellationToken = default);

        UniTask ReleaseAsync(float blendSeconds = -1f, CancellationToken cancellationToken = default);
    }
}
