using System.Globalization;
using Bocage.Data.RuntimeContainers;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Listens to <see cref="RC_WaterTableDepth"/> and writes its value
    /// into a UI Toolkit Label in the dashboard. Mirrors
    /// <c>HedgerowDensityLabelBinding</c> — pure read-only consumer, no
    /// model access, no logic beyond formatting.
    /// <para>
    /// The label arrives in the UXML at sub-étape 6b. Before then the
    /// binding is safe to add: it fail-soft logs once via SimLogger if
    /// the label is not found, then becomes a no-op.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class WaterTableLabelBinding : MonoBehaviour
    {
        [SerializeField] private RC_WaterTableDepth container;

        [SerializeField, Tooltip("Name of the UXML Label that receives the formatted value.")]
        private string labelName = "water-table-value";

        private UIDocument _document;
        private Label _label;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            ResolveLabel();
            if (container != null)
            {
                container.OnChanged += HandleChanged;
                HandleChanged(container.DepthMeters);
            }
            else
            {
                SimLogger.DebugLog("[WaterTableLabelBinding] container not assigned on " + name);
            }
        }

        private void OnDisable()
        {
            if (container != null)
            {
                container.OnChanged -= HandleChanged;
            }
        }

        private void ResolveLabel()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            _label = _document.rootVisualElement.Q<Label>(labelName);
            if (_label == null)
            {
                SimLogger.DebugLog("[WaterTableLabelBinding] label '" + labelName + "' not found in UXML root (expected before sub-étape 6b)");
            }
        }

        private void HandleChanged(float depthMeters)
        {
            if (_label == null)
            {
                ResolveLabel();
                if (_label == null) return;
            }
            _label.text = depthMeters.ToString("F2", CultureInfo.InvariantCulture);
        }
    }
}
