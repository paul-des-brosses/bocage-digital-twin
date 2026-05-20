using System.Globalization;
using Bocage.Data.RuntimeContainers;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Listens to <see cref="RC_HedgerowDensity"/> and writes its value
    /// into the <c>hedgerow-density-value</c> Label of the UI Toolkit
    /// dashboard. This is the canonical example of a Couche-5 binding:
    /// pure read-only consumer of an observable container, no model
    /// access, no logic beyond formatting.
    /// <para>
    /// Single-precision <c>F1</c> formatting is used so the digit
    /// dance is readable at x1 cadence. The value is allocated once
    /// per tick (the container's <see cref="RC_HedgerowDensity.Set"/>
    /// short-circuits when the value is unchanged, so this is bounded).
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HedgerowDensityLabelBinding : MonoBehaviour
    {
        [SerializeField] private RC_HedgerowDensity container;

        [SerializeField, Tooltip("Name of the UXML Label that receives the formatted value.")]
        private string labelName = "hedgerow-density-value";

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
                // Push the current value immediately so the UI is not blank
                // on first frame (the runner publishes at Awake but our
                // subscribe happens at OnEnable, which fires after).
                HandleChanged(container.MetersPerHectare);
            }
            else
            {
                SimLogger.DebugLog("[HedgerowDensityLabelBinding] container not assigned on " + name);
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
                SimLogger.DebugLog("[HedgerowDensityLabelBinding] label '" + labelName + "' not found in UXML root");
            }
        }

        private void HandleChanged(float metersPerHectare)
        {
            if (_label == null)
            {
                ResolveLabel();
                if (_label == null) return;
            }
            _label.text = metersPerHectare.ToString("F1", CultureInfo.InvariantCulture);
        }
    }
}
