using System.Collections.Generic;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Activity console: mirrors the <see cref="SimLogger"/> UserAction channel
    /// into a small log panel in the dashboard's bottom strip, so manual
    /// interventions and other user actions produce visible, textual feedback.
    /// (UI Toolkit runtime tooltips do not render in Play Mode / WebGL, so this
    /// is the user-facing trace of "what just happened".) Subscribes the same
    /// way <c>BootstrapEntryPoint</c> does.
    /// <para>
    /// Scene wiring: attach to the GameObject carrying the dashboard
    /// <see cref="UIDocument"/> (the one that also holds the other dashboard
    /// bindings, e.g. <c>_UI_Canvas</c>). No Inspector reference is required —
    /// it reads the static SimLogger event and resolves its container by name.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ConsoleBinding : MonoBehaviour
    {
        private const string LineClass = "console-line";
        private const string LatestModifier = "console-line--latest";
        private const string EmptyText = "En attente d'une action…";

        [SerializeField, Tooltip("Name of the VisualElement that receives the log lines.")]
        private string logContainerName = "console-log-lines";

        [SerializeField, Min(1), Tooltip("Maximum lines kept on screen (oldest drop off).")]
        private int maxLines = 8;

        private UIDocument _document;
        private VisualElement _container;
        private ConsoleLogBuffer _buffer;
        private readonly List<Label> _labels = new List<Label>();
        private bool _subscribed;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _buffer = new ConsoleLogBuffer(maxLines);
        }

        private void OnEnable()
        {
            // Robuste au domain reload de l'éditeur (recompilation en Play Mode) :
            // Awake n'est pas rappelé et les champs non-sérialisés repassent à null,
            // alors qu'OnEnable, lui, est rappelé. On ré-initialise donc ici.
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_buffer == null) _buffer = new ConsoleLogBuffer(maxLines);
            ResolveContainer();
            BuildLabels();
            if (!_subscribed)
            {
                SimLogger.OnUserAction += HandleUserAction;
                _subscribed = true;
            }
            Render();
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                SimLogger.OnUserAction -= HandleUserAction;
                _subscribed = false;
            }
        }

        private void ResolveContainer()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            _container = _document.rootVisualElement.Q<VisualElement>(logContainerName);
            if (_container == null)
            {
                SimLogger.DebugLog("[ConsoleBinding] log container '" + logContainerName + "' not found in UXML root");
            }
        }

        private void BuildLabels()
        {
            if (_container == null) return;
            _container.Clear();
            _labels.Clear();
            // Pre-create the fixed pool of line labels once; HandleUserAction
            // then only updates their text — no per-event element allocation.
            for (int i = 0; i < maxLines; i++)
            {
                var label = new Label();
                label.AddToClassList(LineClass);
                if (i == 0) label.AddToClassList(LatestModifier);
                _container.Add(label);
                _labels.Add(label);
            }
        }

        private void HandleUserAction(string message)
        {
            _buffer.Add(message);
            Render();
        }

        private void Render()
        {
            if (_container == null || _labels.Count == 0) return;

            var lines = _buffer.Lines;
            if (lines.Count == 0)
            {
                // Never look empty: a single muted "waiting" line (cf CLAUDE.md §17).
                for (int i = 0; i < _labels.Count; i++)
                {
                    _labels[i].text = i == 0 ? EmptyText : string.Empty;
                    _labels[i].style.display = i == 0 ? DisplayStyle.Flex : DisplayStyle.None;
                }
                return;
            }

            for (int i = 0; i < _labels.Count; i++)
            {
                if (i < lines.Count)
                {
                    _labels[i].text = lines[i];
                    _labels[i].style.display = DisplayStyle.Flex;
                }
                else
                {
                    _labels[i].text = string.Empty;
                    _labels[i].style.display = DisplayStyle.None;
                }
            }
        }
    }
}
