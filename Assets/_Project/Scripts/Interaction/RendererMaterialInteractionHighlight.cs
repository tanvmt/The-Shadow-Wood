using System;
using UnityEngine;

namespace TheShadowWood.Interaction
{
    /// <summary>
    /// Outline-package-free fallback. Appends a shared highlight material while focused
    /// and restores the exact original material arrays afterwards.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RendererMaterialInteractionHighlight : InteractionHighlight
    {
        [SerializeField] private Renderer[] targetRenderers = new Renderer[0];
        [SerializeField] private Material highlightMaterial;

        private Material[][] _originalMaterials;
        private bool _isHighlighted;

        private void Awake()
        {
            CacheOriginalMaterials();
        }

        public override void SetHighlighted(bool highlighted)
        {
            if (_isHighlighted == highlighted || _originalMaterials == null)
            {
                return;
            }

            if (highlighted && highlightMaterial == null)
            {
                return;
            }

            _isHighlighted = highlighted;

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer targetRenderer = targetRenderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.sharedMaterials = highlighted
                    ? AppendMaterial(_originalMaterials[i], highlightMaterial)
                    : _originalMaterials[i];
            }
        }

        private void OnDisable()
        {
            if (_isHighlighted)
            {
                SetHighlighted(false);
            }
        }

        private void CacheOriginalMaterials()
        {
            _originalMaterials = new Material[targetRenderers.Length][];
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                _originalMaterials[i] = targetRenderers[i] != null
                    ? targetRenderers[i].sharedMaterials
                    : Array.Empty<Material>();
            }
        }

        private static Material[] AppendMaterial(Material[] source, Material material)
        {
            Material[] result = new Material[source.Length + 1];
            Array.Copy(source, result, source.Length);
            result[result.Length - 1] = material;
            return result;
        }
    }
}
