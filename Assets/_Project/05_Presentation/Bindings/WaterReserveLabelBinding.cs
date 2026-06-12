using System.Globalization;
using Bocage.Data.RuntimeContainers;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Listens to <see cref="RC_SoilMoisture"/> and writes the soil water
    /// reserve into the <c>water-reserve-value</c> Label of the Hero strip,
    /// as a whole percentage of usable reserve (% RU). The container's channel
    /// is unit-range by construction (0 = point de flétrissement, 1 = capacité
    /// au champ); we display it ×100 with no decimals, matching the
    /// <see cref="BiodiversityLabelBinding"/> convention. Read-only consumer —
    /// the meadow shader reads the same container's raw channel, so nothing
    /// here changes the published value.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class WaterReserveLabelBinding : MonoBehaviour
    {
        [SerializeField] private RC_SoilMoisture container;

        [SerializeField, Tooltip("Name of the UXML Label that receives the formatted value.")]
        private string labelName = "water-reserve-value";

        private UIDocument _document;
        private Label _label;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            ResolveLabel();
            if (container != null)
            {
                container.OnChanged += HandleChanged;
                HandleChanged(container.Moisture01);
            }
            else
            {
                SimLogger.DebugLog("[WaterReserveLabelBinding] container not assigned on " + name);
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
                SimLogger.DebugLog("[WaterReserveLabelBinding] label '" + labelName + "' not found in UXML root");
            }
        }

        private void HandleChanged(float moisture01)
        {
            if (_label == null)
            {
                ResolveLabel();
                if (_label == null) return;
            }
            int percent = Mathf.RoundToInt(moisture01 * 100f);
            _label.text = percent.ToString(CultureInfo.InvariantCulture);
        }
    }
}
