using UnityEngine;

namespace Bocage.Presentation.Scene.Sky
{
    /// <summary>
    /// Pushes the sky gradient parameters to the assigned renderer's material
    /// via a MaterialPropertyBlock (so the shared material asset is never
    /// mutated and the binding stays cheap).
    /// <para>
    /// The sky is a deliberate static backdrop: its gradient is set once
    /// from these inspector values and never varies at runtime. CLAUDE.md
    /// §9 (primauté du capteur) governs visual *variation* derived from a
    /// measurement; a fixed backdrop carries none, so there is nothing to
    /// drive from the model here.
    /// </para>
    /// <para>
    /// Shader contract: the assigned material's shader must expose three
    /// properties named <c>_TopColor</c>, <c>_BottomColor</c> (Color) and
    /// <c>_Horizon</c> (Float in 0..1). The Shader Graph asset authored
    /// in <c>Assets/_Project/05_Presentation/Scene/Shaders/SG_Sky</c>
    /// follows this naming.
    /// </para>
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SkyController : MonoBehaviour
    {
        private static readonly int TopColorId = Shader.PropertyToID("_TopColor");
        private static readonly int BottomColorId = Shader.PropertyToID("_BottomColor");
        private static readonly int HorizonId = Shader.PropertyToID("_Horizon");

        [SerializeField, Tooltip("Renderer of the sky quad. Receives a MaterialPropertyBlock.")]
        private Renderer targetRenderer;

        [SerializeField, ColorUsage(showAlpha: false, hdr: true)]
        private Color topColor = new Color(0.16f, 0.20f, 0.28f, 1f);

        [SerializeField, ColorUsage(showAlpha: false, hdr: true)]
        private Color bottomColor = new Color(0.85f, 0.75f, 0.55f, 1f);

        [SerializeField, Range(0f, 1f), Tooltip("Vertical position of the horizon transition in UV space.")]
        private float horizon = 0.55f;

        private MaterialPropertyBlock _block;

        private void OnEnable() => Apply();
        private void OnValidate() => Apply();

        private void Apply()
        {
            if (targetRenderer == null) return;
            _block ??= new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(_block);
            _block.SetColor(TopColorId, topColor);
            _block.SetColor(BottomColorId, bottomColor);
            _block.SetFloat(HorizonId, horizon);
            targetRenderer.SetPropertyBlock(_block);
        }
    }
}
