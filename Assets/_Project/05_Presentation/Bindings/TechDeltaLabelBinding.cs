using System.Globalization;
using Bocage.Data.RuntimeContainers;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Listens to <see cref="RC_TechDelta"/> and writes its percent value
    /// into the <c>tech-delta-value</c> Label of the dashboard. Format:
    /// signed integer percent (e.g. "+12", "-5", "0"). At sub-étape 8b
    /// the value is 0 by construction (shadow == real) — display will
    /// read "0" until decisions land at 8c.
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
                HandleChanged(container.DeltaPercent);
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

        private void HandleChanged(float deltaPercent)
        {
            if (_label == null)
            {
                ResolveLabel();
                if (_label == null) return;
            }
            // F0 with explicit sign so "+12 %" reads better than "12".
            _label.text = deltaPercent.ToString("+0;-0;0", CultureInfo.InvariantCulture);
        }
    }
}
