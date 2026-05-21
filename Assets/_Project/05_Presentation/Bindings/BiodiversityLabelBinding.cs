using System.Globalization;
using Bocage.Data.RuntimeContainers;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Listens to <see cref="RC_BiodiversityComposite"/> and writes its
    /// score into the <c>biodiversity-value</c> Label of the dashboard.
    /// The composite score lives in <c>[0, 1]</c> so we display it as a
    /// percentage with no decimals (e.g. "62 %") to match the typical
    /// reading "biodiversity recovery 62 % of the reference state".
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class BiodiversityLabelBinding : MonoBehaviour
    {
        [SerializeField] private RC_BiodiversityComposite container;

        [SerializeField, Tooltip("Name of the UXML Label that receives the formatted value.")]
        private string labelName = "biodiversity-value";

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
                HandleChanged(container.Score);
            }
            else
            {
                SimLogger.DebugLog("[BiodiversityLabelBinding] container not assigned on " + name);
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
                SimLogger.DebugLog("[BiodiversityLabelBinding] label '" + labelName + "' not found in UXML root");
            }
        }

        private void HandleChanged(float score)
        {
            if (_label == null)
            {
                ResolveLabel();
                if (_label == null) return;
            }
            int percent = Mathf.RoundToInt(score * 100f);
            _label.text = percent.ToString(CultureInfo.InvariantCulture);
        }
    }
}
