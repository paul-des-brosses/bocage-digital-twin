using System.Collections.Generic;
using Bocage.Presentation.Scene.Sensors;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Builds the "Capteurs déployés" list in the dashboard's bottom-right
    /// panel by scanning the children of <see cref="sensorRoot"/> for
    /// <see cref="SensorMetadataTag"/> components and creating one row per
    /// sensor. Replaces the spatial minimap originally planned for
    /// sub-étape 6c.2 — a list panel carries more information per pixel
    /// and exposes online/deferred status more clearly than dots over a
    /// dummy background.
    /// <para>
    /// Each row is a small visual element with a status dot (green for
    /// Online, ocre for Deferred), a sensor name and a subtitle showing
    /// either the observed model variable (Online) or the deferred-until
    /// step (Deferred). The row also caches the source
    /// <see cref="SensorMetadataTag"/> so the bidirectional hover sync at
    /// 6c.3 can iterate <see cref="RowsByDisplayName"/> without
    /// re-querying the scene.
    /// </para>
    /// <para>
    /// Scan happens at Start (same reasoning as the earlier dot binding
    /// and as <c>HedgerowShaderBinding</c>): the SensorVisualPlacer
    /// destroys and respawns children at Awake (execution order -9000),
    /// so any reference captured in Edit Mode would be stale at Play.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class SensorListBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Parent transform whose children carry SensorMetadataTag. Typically '_Scene_Visual/Sensors'.")]
        private Transform sensorRoot;

        [SerializeField, Tooltip("Name of the VisualElement in the UXML that receives the dynamically-created rows.")]
        private string rowsContainerName = "sensor-list-rows";

        [SerializeField, Tooltip("USS class for each row.")]
        private string rowClass = "sensor-list-row";

        [SerializeField, Tooltip("Base USS class for the status dot.")]
        private string statusDotClass = "sensor-status-dot";

        [SerializeField, Tooltip("USS class for an Online sensor's status dot.")]
        private string onlineModifierClass = "sensor-status-dot--online";

        [SerializeField, Tooltip("USS class for a Deferred sensor's status dot.")]
        private string deferredModifierClass = "sensor-status-dot--deferred";

        [SerializeField, Tooltip("USS class for the row's text-block container.")]
        private string textBlockClass = "sensor-row-text";

        [SerializeField, Tooltip("USS class for the sensor name label.")]
        private string nameLabelClass = "sensor-row-name";

        // Legacy serialized field kept so existing scene wiring doesn't
        // warn on import after the subtitle row was dropped at
        // sub-étape 10a. The class is no longer referenced from code.
        // TODO post-10a: remove this field once the scene asset has
        // been saved without it.
        [SerializeField, HideInInspector]
        private string subtitleLabelClass = "sensor-row-subtitle";

        [SerializeField, Tooltip("USS class applied to a row while it (or its scene sibling) is hovered.")]
        private string highlightedRowClass = "sensor-list-row--highlighted";

        private UIDocument _document;
        private VisualElement _rowsContainer;
        private readonly Dictionary<string, VisualElement> _rowsByDisplayName = new Dictionary<string, VisualElement>(8);
        private readonly Dictionary<string, SensorMetadataTag> _tagsByDisplayName = new Dictionary<string, SensorMetadataTag>(8);
        // Keyed by sensor id so the hover event bus (which uses sensor ids)
        // can find the matching row directly.
        private readonly Dictionary<string, VisualElement> _rowsBySensorId = new Dictionary<string, VisualElement>(8);
        private bool _subscribedToBus;

        /// <summary>
        /// Read-only access to (key → row) used by 6c.3 hover sync.
        /// The key is the sensor display name (falling back to id when
        /// empty), unique across the dashboard.
        /// </summary>
        public IReadOnlyDictionary<string, VisualElement> RowsByDisplayName => _rowsByDisplayName;
        public IReadOnlyDictionary<string, SensorMetadataTag> TagsByDisplayName => _tagsByDisplayName;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void Start()
        {
            ResolveContainer();
            BuildRows();
            SubscribeToBus();
        }

        private void OnDestroy()
        {
            UnsubscribeFromBus();
            ClearRows();
        }

        private void SubscribeToBus()
        {
            if (_subscribedToBus) return;
            SensorHoverEventBus.SensorHoverEnter += HandleBusHoverEnter;
            SensorHoverEventBus.SensorHoverExit += HandleBusHoverExit;
            _subscribedToBus = true;
        }

        private void UnsubscribeFromBus()
        {
            if (!_subscribedToBus) return;
            SensorHoverEventBus.SensorHoverEnter -= HandleBusHoverEnter;
            SensorHoverEventBus.SensorHoverExit -= HandleBusHoverExit;
            _subscribedToBus = false;
        }

        private void HandleBusHoverEnter(string sensorId)
        {
            if (_rowsBySensorId.TryGetValue(sensorId, out var row) && row != null)
            {
                row.AddToClassList(highlightedRowClass);
            }
        }

        private void HandleBusHoverExit(string sensorId)
        {
            if (_rowsBySensorId.TryGetValue(sensorId, out var row) && row != null)
            {
                row.RemoveFromClassList(highlightedRowClass);
            }
        }

        private void ResolveContainer()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            _rowsContainer = _document.rootVisualElement.Q<VisualElement>(rowsContainerName);
            if (_rowsContainer == null)
            {
                SimLogger.DebugLog("[SensorListBinding] rows container '" + rowsContainerName + "' not found in UXML root");
            }
        }

        private void BuildRows()
        {
            ClearRows();
            if (_rowsContainer == null) return;
            if (sensorRoot == null)
            {
                SimLogger.DebugLog("[SensorListBinding] sensorRoot not assigned, no rows will be created");
                return;
            }

            int created = 0;
            for (int i = 0; i < sensorRoot.childCount; i++)
            {
                var child = sensorRoot.GetChild(i);
                var meta = child.GetComponent<SensorMetadataTag>();
                if (meta == null) continue;

                string key = string.IsNullOrEmpty(meta.DisplayName) ? meta.SensorId : meta.DisplayName;
                if (_rowsByDisplayName.ContainsKey(key))
                {
                    // Defensive: two sensors with the same display name would
                    // collide in the hover sync map. Log and skip the second.
                    SimLogger.DebugLog("[SensorListBinding] duplicate sensor key '" + key + "', second occurrence skipped");
                    continue;
                }

                var row = BuildRow(meta);
                // Hook bidirectional hover: hovering this row raises bus
                // events keyed by sensor id, which the scene side and the
                // bus subscriber on this binding both react to.
                string capturedSensorId = meta.SensorId;
                row.RegisterCallback<PointerEnterEvent>(_ => SensorHoverEventBus.RaiseEnter(capturedSensorId));
                row.RegisterCallback<PointerLeaveEvent>(_ => SensorHoverEventBus.RaiseExit(capturedSensorId));

                _rowsContainer.Add(row);
                _rowsByDisplayName[key] = row;
                _tagsByDisplayName[key] = meta;
                if (!string.IsNullOrEmpty(meta.SensorId))
                {
                    _rowsBySensorId[meta.SensorId] = row;
                }
                created++;
            }

            SimLogger.DebugLog("[SensorListBinding] populated " + created + " sensor rows from " + sensorRoot.name);
        }

        private VisualElement BuildRow(SensorMetadataTag meta)
        {
            var row = new VisualElement();
            row.AddToClassList(rowClass);

            var dot = new VisualElement();
            dot.AddToClassList(statusDotClass);
            dot.AddToClassList(meta.OnlineStatus == SensorOnlineStatus.Online
                ? onlineModifierClass
                : deferredModifierClass);
            row.Add(dot);

            var textBlock = new VisualElement();
            textBlock.AddToClassList(textBlockClass);

            var nameLabel = new Label(string.IsNullOrEmpty(meta.DisplayName) ? meta.SensorId : meta.DisplayName);
            nameLabel.AddToClassList(nameLabelClass);
            textBlock.Add(nameLabel);

            // Subtitle row dropped at sub-étape 10a — the variable name
            // (e.g. "Piezometer — mesure WaterTableDepth") was jargony
            // and redundant with the legend at the bottom of the list
            // ("branché" / "en attente"). Keeping only dot + display
            // name gives a denser, cleaner read.

            row.Add(textBlock);
            return row;
        }

        private void ClearRows()
        {
            if (_rowsContainer != null)
            {
                foreach (var kv in _rowsByDisplayName)
                {
                    var row = kv.Value;
                    if (row != null && row.parent != null) row.parent.Remove(row);
                }
            }
            _rowsByDisplayName.Clear();
            _tagsByDisplayName.Clear();
            _rowsBySensorId.Clear();
        }
    }
}
