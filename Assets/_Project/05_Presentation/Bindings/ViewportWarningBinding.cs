using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Toggles the <c>viewport-warning</c> banner of the dashboard UXML
    /// based on the current screen width. CLAUDE.md §12 states the
    /// project is desktop-only and that windows narrower than 1280 px
    /// are not optimised; the banner informs the visitor accordingly
    /// without blocking the experience.
    /// <para>
    /// The component runs at default execution order; the check is a
    /// trivial integer comparison every frame, with allocations only
    /// when the state actually flips (rare). Subtler approaches
    /// (debouncing via <c>OnRectGeometryChanged</c>) were considered
    /// but the cost of polling is negligible and the implementation
    /// stays trivially testable.
    /// </para>
    /// <para>
    /// The banner element is found by name in the bound
    /// <see cref="UIDocument"/>'s root and toggled via the
    /// <c>hidden</c> USS class. Hard-coding the element name in code
    /// rather than letting Unity bind via SerializedField keeps the
    /// UXML the single source of structure: rename the element and
    /// you fix the binding by changing the <see cref="warningElementName"/>
    /// inspector field, no recompilation needed.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ViewportWarningBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Width threshold below which the warning banner is shown. CLAUDE.md §12 fixes 1280 as the desktop minimum.")]
        private int minRecommendedWidth = 1280;

        [SerializeField, Tooltip("Name of the VisualElement in the UXML that holds the warning banner.")]
        private string warningElementName = "viewport-warning";

        [SerializeField, Tooltip("Name of the USS class used to hide the banner. Must match the .hidden rule in Dashboard.uss.")]
        private string hiddenClass = "hidden";

        private UIDocument _document;
        private VisualElement _banner;
        private bool? _lastHiddenState; // tri-state: null = unknown, true/false = last applied

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            ResolveBanner();
            // Force an evaluation at OnEnable so the banner state is correct
            // on the first frame even before Update runs.
            ApplyState(force: true);
        }

        private void Update()
        {
            ApplyState(force: false);
        }

        private void ResolveBanner()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            _banner = _document.rootVisualElement.Q<VisualElement>(warningElementName);
            if (_banner == null)
            {
                SimLogger.DebugLog("[ViewportWarningBinding] banner '" + warningElementName + "' not found in UXML root");
            }
        }

        private void ApplyState(bool force)
        {
            if (_banner == null)
            {
                ResolveBanner();
                if (_banner == null) return;
            }

            bool shouldHide = Screen.width >= minRecommendedWidth;
            if (!force && _lastHiddenState.HasValue && _lastHiddenState.Value == shouldHide)
            {
                return;
            }

            if (shouldHide)
            {
                _banner.AddToClassList(hiddenClass);
            }
            else
            {
                _banner.RemoveFromClassList(hiddenClass);
            }
            _lastHiddenState = shouldHide;
        }
    }
}
