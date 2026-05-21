using System.Globalization;
using Bocage.Data.RuntimeContainers;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Listens to <see cref="RC_IntegratedProfitability"/> and writes its
    /// value into the <c>profitability-value</c> Label of the dashboard.
    /// F0 formatting (€/ha/yr is a whole-euro figure, no need for
    /// decimals; the F2 used for WaterTable depth would be visually
    /// noisy here).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class IntegratedProfitabilityLabelBinding : MonoBehaviour
    {
        [SerializeField] private RC_IntegratedProfitability container;

        [SerializeField, Tooltip("Name of the UXML Label that receives the formatted value.")]
        private string labelName = "profitability-value";

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
                HandleChanged(container.EurosPerHectare);
            }
            else
            {
                SimLogger.DebugLog("[IntegratedProfitabilityLabelBinding] container not assigned on " + name);
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
                SimLogger.DebugLog("[IntegratedProfitabilityLabelBinding] label '" + labelName + "' not found in UXML root");
            }
        }

        private void HandleChanged(float eurosPerHectare)
        {
            if (_label == null)
            {
                ResolveLabel();
                if (_label == null) return;
            }
            _label.text = eurosPerHectare.ToString("F0", CultureInfo.InvariantCulture);
        }
    }
}
