using System.Globalization;
using Bocage.Data.RuntimeContainers;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Listens to <see cref="RC_CropYield"/> and writes its value into the
    /// <c>crop-yield-value</c> Label of the Hero strip. F1 formatting (t/ha
    /// reads naturally with one decimal, e.g. "5.5"). Read-only consumer of
    /// an observable container — the canonical Hero-label pattern, identical
    /// in shape to <see cref="SoilCarbonLabelBinding"/> and the others.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CropYieldLabelBinding : MonoBehaviour
    {
        [SerializeField] private RC_CropYield container;

        [SerializeField, Tooltip("Name of the UXML Label that receives the formatted value.")]
        private string labelName = "crop-yield-value";

        private UIDocument _document;
        private Label _label;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            ResolveLabel();
            if (container != null)
            {
                container.OnChanged += HandleChanged;
                HandleChanged(container.TonnesPerHectare);
            }
            else
            {
                SimLogger.DebugLog("[CropYieldLabelBinding] container not assigned on " + name);
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
                SimLogger.DebugLog("[CropYieldLabelBinding] label '" + labelName + "' not found in UXML root");
            }
        }

        private void HandleChanged(float tonnesPerHectare)
        {
            if (_label == null)
            {
                ResolveLabel();
                if (_label == null) return;
            }
            _label.text = tonnesPerHectare.ToString("F1", CultureInfo.InvariantCulture);
        }
    }
}
