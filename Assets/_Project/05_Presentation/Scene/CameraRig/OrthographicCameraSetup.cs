using Bocage.SimulationCore.Logging;
using UnityEngine;

namespace Bocage.Presentation.Scene.CameraRig
{
    /// <summary>
    /// Applies a deterministic orthographic camera configuration at
    /// startup AND in Edit Mode (so the Game view shows the same framing
    /// during authoring as at runtime). Per DECISIONS.md #16 the camera
    /// is strictly fixed: no zoom, no parallax, no follow. The values
    /// authored in the inspector are the canonical framing; runtime
    /// never overrides them.
    /// <para>
    /// The <see cref="viewportRect"/> defines the on-screen rectangle the
    /// camera renders into (normalized 0..1 from bottom-left). The scene
    /// view sits in the centre of the dashboard, surrounded by UI panels
    /// on all four sides. The visible world units height stays
    /// <c>2 × orthographicSize</c>; the rect only crops where on screen
    /// the rendering lands. Width in world units = height × (rect.width /
    /// rect.height) × (screen.width / screen.height).
    /// </para>
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-9500)]
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class OrthographicCameraSetup : MonoBehaviour
    {
        [SerializeField, Tooltip("Half-height of the visible area in world units.")]
        private float orthographicSize = 5f;

        [SerializeField, Tooltip("Camera clear colour, used outside of any sky quad coverage.")]
        private Color backgroundColor = new Color(0.04f, 0.05f, 0.07f, 1f);

        [SerializeField, Tooltip("Fixed camera position. Z must be negative for a 2D scene.")]
        private Vector3 cameraPosition = new Vector3(0f, 0f, -10f);

        [SerializeField, Tooltip("Normalized viewport rect (x, y, width, height in 0..1, origin bottom-left). Defines the on-screen scene window inside the dashboard.")]
        private Rect viewportRect = new Rect(0.1458f, 0.2222f, 0.7083f, 0.7037f);

        private void OnEnable() => Apply(log: Application.isPlaying);
        private void OnValidate() => Apply(log: false);

        private void Apply(bool log)
        {
            var cam = GetComponent<Camera>();
            if (cam == null) return;

            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
            cam.backgroundColor = backgroundColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.rect = viewportRect;
            transform.position = cameraPosition;

            if (log)
            {
                SimLogger.DebugLog("[Camera] orthographic ok size=" + orthographicSize
                    + " rect=(" + viewportRect.x.ToString("F3") + "," + viewportRect.y.ToString("F3")
                    + "," + viewportRect.width.ToString("F3") + "," + viewportRect.height.ToString("F3") + ")");
            }
        }
    }
}
