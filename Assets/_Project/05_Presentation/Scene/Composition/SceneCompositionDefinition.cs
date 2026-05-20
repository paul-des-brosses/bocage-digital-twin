using System.Collections.Generic;
using UnityEngine;

namespace Bocage.Presentation.Scene.Composition
{
    /// <summary>
    /// Data-driven definition of the static visual scene composition.
    /// Read by <see cref="SceneAssembler"/> at boot to instantiate one
    /// SpriteRenderer per element under the scene-visual root. The asset
    /// is pure data: no runtime mutation, no callbacks. Editing the asset
    /// in the inspector reshapes the scene at next play.
    /// <para>
    /// Per DECISIONS.md #16 the camera is fixed; world positions here are
    /// absolute and authored against that fixed framing.
    /// </para>
    /// <para>
    /// This SO does not drive sensor primacy violations on its own: it
    /// describes the static landscape (background hills, hedgerow sprites,
    /// foreground props). Dynamic visual modulation (hedge colour from
    /// healthT, etc.) is layered on top by binding components in a later
    /// step.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Scene/Composition Definition",
        fileName = "SceneComposition_Default")]
    public sealed class SceneCompositionDefinition : ScriptableObject
    {
        [SerializeField] private ScenicElement[] elements = new ScenicElement[0];

        public IReadOnlyList<ScenicElement> Elements => elements;
    }

    /// <summary>
    /// One static scenic element. Authored in the inspector; immutable at
    /// runtime. Pivot, scale and sorting are explicit so the composition
    /// is fully reproducible from this data alone.
    /// </summary>
    [System.Serializable]
    public struct ScenicElement
    {
        [Tooltip("Stable identifier. Used as GameObject name in the hierarchy and in log lines.")]
        public string id;

        [Tooltip("Sprite asset to render. If null the element is skipped.")]
        public Sprite sprite;

        [Tooltip("World-space position in scene units. Z is forced to 0.")]
        public Vector2 worldPosition;

        [Tooltip("Non-uniform scale (X = horizontal stretch, Y = vertical stretch). 0 or negative components are clamped to 1. Use (1,1) for the sprite's native size; use uneven X/Y to fit assets like full-width grass borders without distorting other elements.")]
        public Vector2 scale;

        [Tooltip("Sorting layer name. Must exist in Tags & Layers. Empty falls back to Default.")]
        public string sortingLayerName;

        [Tooltip("Order within the sorting layer (back to front).")]
        public int sortingOrderInLayer;

        [Tooltip("Mirror the sprite horizontally (useful for variant reuse).")]
        public bool flipX;
    }
}
