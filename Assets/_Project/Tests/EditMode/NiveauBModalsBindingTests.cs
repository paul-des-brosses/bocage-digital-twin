using Bocage.Presentation.Bindings;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for the pure visibility toggle in
    /// <see cref="NiveauBModalsBinding.SetVisible"/> (chantier E6). The
    /// click wiring (open button, close button, click-outside, Escape) is
    /// validated visually in Play Mode; here we exercise just the class
    /// list manipulation that drives every visibility transition.
    /// </summary>
    public sealed class NiveauBModalsBindingTests
    {
        [Test]
        public void SetVisible_True_RemovesHiddenClass()
        {
            var overlay = new VisualElement();
            overlay.AddToClassList(NiveauBModalsBinding.HiddenClass);

            NiveauBModalsBinding.SetVisible(overlay, true);

            Assert.IsFalse(overlay.ClassListContains(NiveauBModalsBinding.HiddenClass),
                "Showing the overlay must strip the .hidden class so display: none stops applying.");
        }

        [Test]
        public void SetVisible_False_AddsHiddenClass()
        {
            var overlay = new VisualElement();

            NiveauBModalsBinding.SetVisible(overlay, false);

            Assert.IsTrue(overlay.ClassListContains(NiveauBModalsBinding.HiddenClass),
                "Hiding the overlay must add .hidden so the USS rule kicks in.");
        }

        [Test]
        public void SetVisible_RoundTrip_RestoresInitialHiddenState()
        {
            var overlay = new VisualElement();
            overlay.AddToClassList(NiveauBModalsBinding.HiddenClass);

            NiveauBModalsBinding.SetVisible(overlay, true);
            NiveauBModalsBinding.SetVisible(overlay, false);

            Assert.IsTrue(overlay.ClassListContains(NiveauBModalsBinding.HiddenClass),
                "Open then close must end up exactly where we started (still hidden).");
        }

        [Test]
        public void SetVisible_NullOverlay_IsSafe()
        {
            Assert.DoesNotThrow(() => NiveauBModalsBinding.SetVisible(null, true));
            Assert.DoesNotThrow(() => NiveauBModalsBinding.SetVisible(null, false));
        }
    }
}
