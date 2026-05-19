using Bocage.SimulationCore.Logging;
using UnityEngine;

namespace Bocage.Presentation.Scene.CameraRig
{
    /// <summary>
    /// Applies a deterministic orthographic camera configuration at
    /// startup. Per DECISIONS.md #16 the camera is strictly fixed: no
    /// zoom, no parallax, no follow. The values authored in the
    /// inspector are the canonical framing; runtime never overrides them.
    /// </summary>
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

        private void Awake()
        {
            var cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
            cam.backgroundColor = backgroundColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
            transform.position = cameraPosition;

            SimLogger.DebugLog("[Camera] orthographic ok size=" + orthographicSize);
        }
    }
}
