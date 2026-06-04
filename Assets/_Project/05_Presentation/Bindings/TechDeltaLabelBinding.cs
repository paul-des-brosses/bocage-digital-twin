using System.Globalization;
using Bocage.Data.RuntimeContainers;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Listens to <see cref="RC_TechDelta"/> and writes the NET « apport de
    /// la techno » (€/ha banked by the real run over the shadow run, minus
    /// the cumulative action investment) into the <c>tech-delta-value</c>
    /// Label. Format: signed integer (e.g. "+150", "-40", "0"); the "€/ha"
    /// unit lives in a sibling Label. Reads 0 until a decision diverges the
    /// two runs; can read negative when the capital outlay outruns the gains.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TechDeltaLabelBinding : MonoBehaviour
    {
        [SerializeField] private RC_TechDelta container;

        [SerializeField, Tooltip("Name of the UXML Label that receives the formatted value.")]
        private string labelName = "tech-delta-value";

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
                HandleChanged(container.NetEurosPerHa);
            }
            else
            {
                SimLogger.DebugLog("[TechDeltaLabelBinding] container not assigned on " + name);
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
                SimLogger.DebugLog("[TechDeltaLabelBinding] label '" + labelName + "' not found in UXML root");
            }
        }

        private void HandleChanged(float netEurosPerHa)
        {
            if (_label == null)
            {
                ResolveLabel();
                if (_label == null) return;
            }
            // Signed integer so "+150" reads better than "150"; unit is separate.
            _label.text = netEurosPerHa.ToString("+0;-0;0", CultureInfo.InvariantCulture);
        }
    }
}
