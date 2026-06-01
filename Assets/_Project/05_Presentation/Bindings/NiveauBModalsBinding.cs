using System;
using System.Collections;
using System.Collections.Generic;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Opens and closes the three Niveau B modal popups (Biodiversité,
    /// Climat &amp; ressources, Économie). Each modal lives in
    /// <c>Dashboard.uxml</c> as a <c>.level-b-modal-overlay</c> sibling of
    /// <c>body-row</c>, hidden by default via the proven shared
    /// <c>.hidden</c> utility class — same pattern as
    /// <see cref="DecisionPopupBinding"/>, which has been stable in this
    /// project for months. Clicking a trigger button shows its modal;
    /// closing happens via the X button, clicking outside the card, or
    /// the Escape key.
    /// <para>
    /// Drop this on <c>_UI_Canvas</c>; the three modal configurations
    /// match the UXML names by default, so no Inspector field needs to be
    /// set. The three OngletXxxBindings continue to find their labels via
    /// <c>Q&lt;Label&gt;</c> against the still-present UXML tree, so
    /// values update silently while the modal is hidden and are correct
    /// the instant it opens.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class NiveauBModalsBinding : MonoBehaviour
    {
        /// <summary>One modal descriptor — names of the open button, close button, and overlay in the UXML.</summary>
        [Serializable]
        public struct ModalConfig
        {
            public string openButtonName;
            public string closeButtonName;
            public string overlayName;
        }

        [SerializeField, Tooltip("One entry per Niveau B modal — UXML names of its open/close buttons and overlay.")]
        private ModalConfig[] modals = new[]
        {
            new ModalConfig { openButtonName = "biodiv-open",  closeButtonName = "biodiv-modal-close",  overlayName = "biodiv-modal-overlay"  },
            new ModalConfig { openButtonName = "climat-open",  closeButtonName = "climat-modal-close",  overlayName = "climat-modal-overlay"  },
            new ModalConfig { openButtonName = "economy-open", closeButtonName = "economy-modal-close", overlayName = "economy-modal-overlay" },
        };

        /// <summary>USS class that toggles <c>display: none</c>; already proven on decision-popup-overlay.</summary>
        public const string HiddenClass = "hidden";

        private sealed class WiredModal
        {
            public Button OpenButton;
            public Button CloseButton;
            public VisualElement Overlay;
            public Action OpenHandler;
            public Action CloseHandler;
            public EventCallback<ClickEvent> OverlayClickHandler;
        }

        private UIDocument _document;
        private readonly List<WiredModal> _wired = new List<WiredModal>();
        private Coroutine _initRoutine;
        private bool _escapeRegistered;
        private EventCallback<KeyDownEvent> _escapeHandler;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable() => _initRoutine = StartCoroutine(InitRoutine());

        private void OnDisable()
        {
            if (_initRoutine != null)
            {
                StopCoroutine(_initRoutine);
                _initRoutine = null;
            }
            for (int i = 0; i < _wired.Count; i++)
            {
                WiredModal w = _wired[i];
                if (w.OpenButton != null && w.OpenHandler != null) w.OpenButton.clicked -= w.OpenHandler;
                if (w.CloseButton != null && w.CloseHandler != null) w.CloseButton.clicked -= w.CloseHandler;
                if (w.Overlay != null && w.OverlayClickHandler != null) w.Overlay.UnregisterCallback(w.OverlayClickHandler);
            }
            _wired.Clear();
            if (_escapeRegistered && _document != null && _document.rootVisualElement != null && _escapeHandler != null)
            {
                _document.rootVisualElement.UnregisterCallback(_escapeHandler, TrickleDown.TrickleDown);
                _escapeRegistered = false;
                _escapeHandler = null;
            }
        }

        private IEnumerator InitRoutine()
        {
            const int maxAttempts = 300;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (TryWire())
                {
                    _initRoutine = null;
                    yield break;
                }
                yield return null;
            }
            SimLogger.DebugLog("[NiveauBModalsBinding] no modal could be wired after "
                + maxAttempts + " frames on " + name);
            _initRoutine = null;
        }

        private bool TryWire()
        {
            VisualElement root = _document != null ? _document.rootVisualElement : null;
            if (root == null) return false;
            bool anyWired = false;
            for (int i = 0; i < modals.Length; i++)
            {
                ModalConfig cfg = modals[i];
                Button open = root.Q<Button>(cfg.openButtonName);
                Button close = root.Q<Button>(cfg.closeButtonName);
                VisualElement overlay = root.Q<VisualElement>(cfg.overlayName);
                if (open == null || close == null || overlay == null) continue;

                VisualElement capturedOverlay = overlay;
                var w = new WiredModal
                {
                    OpenButton = open,
                    CloseButton = close,
                    Overlay = overlay,
                    OpenHandler = () => SetVisible(capturedOverlay, true),
                    CloseHandler = () => SetVisible(capturedOverlay, false),
                    // Click-outside dismissal: only fires when the click target IS the overlay itself
                    // (not bubbled up from the card or any child), avoiding accidental close while
                    // interacting with the modal content.
                    OverlayClickHandler = evt =>
                    {
                        if (evt.target == capturedOverlay) SetVisible(capturedOverlay, false);
                    },
                };
                open.clicked += w.OpenHandler;
                close.clicked += w.CloseHandler;
                overlay.RegisterCallback(w.OverlayClickHandler);
                _wired.Add(w);
                anyWired = true;
            }
            if (anyWired && !_escapeRegistered)
            {
                _escapeHandler = OnKeyDown;
                // TrickleDown so we catch the event before any focused child consumes it.
                root.RegisterCallback(_escapeHandler, TrickleDown.TrickleDown);
                _escapeRegistered = true;
            }
            return anyWired;
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape) return;
            for (int i = 0; i < _wired.Count; i++) SetVisible(_wired[i].Overlay, false);
        }

        /// <summary>
        /// Toggles the <c>.hidden</c> class on <paramref name="overlay"/>:
        /// <paramref name="visible"/> = true removes it (the overlay shows),
        /// false adds it (the overlay disappears via <c>display: none</c>).
        /// Pure — covered by EditMode tests.
        /// </summary>
        public static void SetVisible(VisualElement overlay, bool visible)
        {
            if (overlay == null) return;
            if (visible) overlay.RemoveFromClassList(HiddenClass);
            else overlay.AddToClassList(HiddenClass);
        }
    }
}
