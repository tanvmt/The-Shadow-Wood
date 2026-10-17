using System.Threading;
using Cysharp.Threading.Tasks;

namespace TheShadowWood.Core.UI
{
    // Full-screen black overlay: 0 = clear, 1 = black. Runs on unscaled time.
    public interface IScreenFader
    {
        float Alpha { get; }

        void SetAlpha(float alpha);
        UniTask FadeToAsync(float alpha, float duration, CancellationToken cancellationToken = default);
    }
}
