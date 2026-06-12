using System.Globalization;
using Bocage.Data.RuntimeContainers;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Listens to <see cref="RC_SoilCarbonStock"/> and writes its value into
    /// the <c>soil-carbon-value</c> Label of the Hero strip. F1 formatting
    /// (tC/ha reads naturally with one decimal). The carbon stock was already
    /// published by the runner (and shown in the Climat &amp; ressources onglet);
    /// this surfaces it as a first-class Hero KPI. Read-only consumer of an
    /// observable container — same canonical pattern as the sibling Hero
    /// label bindings.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class SoilCarbonLabelBinding : MonoBehaviour
    {
        [SerializeField] private RC_SoilCarbonStock container;

        [SerializeField, Tooltip("Name of the UXML Label that receives the formatted value.")]
        private string labelName = "soil-carbon-value";

        private UIDocument _document;
        private Label _label;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            ResolveLabel();
            if (container != null)
            {
                container.OnChanged += HandleChanged;
                HandleChanged(container.TonnesCarbonPerHectare);
            }
            else
            {
                SimLogger.DebugLog("[SoilCarbonLabelBinding] container not assigned on " + name);
            }
        }

        private void OnDisable()
        {
            if (container != null) container.OnChanged -= HandleChanged;
        }

        private void ResolveLabel()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            _label = _document.rootVisualElement.Q<Label>(labelName);
            if (_label == null)
            {
                SimLogger.DebugLog("[SoilCarbonLabelBinding] label '" + labelName + "' not found in UXML root");
            }
        }

        private void HandleChanged(float tonnesCarbonPerHectare)
        {
            if (_label == null)
            {
                ResolveLabel();
                if (_label == null) return;
            }
            _label.text = tonnesCarbonPerHectare.ToString("F1", CultureInfo.InvariantCulture);
        }
    }
}
