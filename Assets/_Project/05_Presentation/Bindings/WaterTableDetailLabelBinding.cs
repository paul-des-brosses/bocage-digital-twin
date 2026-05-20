using System.Globalization;
using Bocage.Data.RuntimeContainers;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Mirrors the water table depth value into a secondary Label located
    /// inside the Niveau B "Climat &amp; ressources" panel (the
    /// <c>water-table-detail-value</c> element of the UXML). Same
    /// observable as the hero card binding, different label name —
    /// allows the dashboard to surface the same honest KPI in both
    /// reading layers (hero glance + Niveau B detail) without
    /// duplicating the data path.
    /// <para>
    /// Two-decimals formatting here vs one decimal on the hero card,
    /// because the detail panel is the reading layer for "looking at the
    /// number carefully" while the hero is the glance layer.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class WaterTableDetailLabelBinding : MonoBehaviour
    {
        [SerializeField] private RC_WaterTableDepth container;

        [SerializeField, Tooltip("Name of the UXML Label inside the Niveau B 'Climat & ressources' panel.")]
        private string labelName = "water-table-detail-value";

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
                SimLogger.DebugLog("[WaterTableDetailLabelBinding] container not assigned on " + name);
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
                SimLogger.DebugLog("[WaterTableDetailLabelBinding] label '" + labelName + "' not found in UXML root");
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
