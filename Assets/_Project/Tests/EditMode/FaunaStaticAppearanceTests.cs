using Bocage.Presentation.Scene.Fauna;
using NUnit.Framework;
using UnityEngine;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for <see cref="FaunaStaticAppearance"/>: the
    /// alpha fade tracks SetVisible toggles at the configured speed.
    /// Uses <see cref="FaunaStaticAppearance.TickFade"/> instead of the
    /// Update loop so deltaTime can be controlled deterministically.
    /// </summary>
    public sealed class FaunaStaticAppearanceTests
    {
        [Test]
        public void Fade_LerpsTowardTargetAtConfiguredDuration()
        {
            var go = new GameObject("test_static_appearance");
            go.AddComponent<SpriteRenderer>();
            var sa = go.AddComponent<FaunaStaticAppearance>();
            sa.Configure(fadeDurationSec: 1.0f);

            // Initial state: invisible.
            Assert.AreEqual(0f, sa.CurrentAlpha, 0.001f);
            Assert.AreEqual(0f, sa.TargetAlpha, 0.001f);

            // Fade in: target = 1.
            sa.SetVisible(true);
            Assert.AreEqual(1f, sa.TargetAlpha, 0.001f);

            sa.TickFade(0.25f);
            Assert.AreEqual(0.25f, sa.CurrentAlpha, 0.001f,
                "After 0.25s with 1s fade, alpha should be 0.25.");

            sa.TickFade(0.50f);
            Assert.AreEqual(0.75f, sa.CurrentAlpha, 0.001f);

            sa.TickFade(1.00f);  // overshoot — clamps at 1.
            Assert.AreEqual(1f, sa.CurrentAlpha, 0.001f);

            // Fade out: target = 0.
            sa.SetVisible(false);
            Assert.AreEqual(0f, sa.TargetAlpha, 0.001f);

            sa.TickFade(0.40f);
            Assert.AreEqual(0.60f, sa.CurrentAlpha, 0.001f);

            sa.TickFade(2.00f);  // overshoot — clamps at 0.
            Assert.AreEqual(0f, sa.CurrentAlpha, 0.001f);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void HeadTurn_TriggerAndRevert_SwapsSprite()
        {
            var rest = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var alert = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);

            var go = new GameObject("test_head_turn");
            var renderer = go.AddComponent<SpriteRenderer>();
            var sa = go.AddComponent<FaunaStaticAppearance>();
            sa.Configure(
                fadeDurationSec: 1.0f,
                restSprite: rest,
                alertSprite: alert,
                meanSecondsBetweenHeadTurns: 10f,
                headTurnHoldSec: 2.0f,
                seed: 42UL);

            // Configure assigned rest sprite immediately.
            Assert.AreSame(rest, renderer.sprite, "Configure should set the rest sprite.");
            Assert.IsFalse(sa.IsAlert);

            // Force-enter alert (bypass Bernoulli).
            sa.TriggerHeadTurnForTest();
            Assert.IsTrue(sa.IsAlert);
            Assert.AreSame(alert, renderer.sprite);

            // Tick less than hold → still alert.
            sa.TickHeadTurn(1.5f);
            Assert.IsTrue(sa.IsAlert);
            Assert.AreSame(alert, renderer.sprite);

            // Tick past hold → revert to rest.
            sa.TickHeadTurn(1.0f);
            Assert.IsFalse(sa.IsAlert);
            Assert.AreSame(rest, renderer.sprite);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(rest);
            Object.DestroyImmediate(alert);
        }
    }
}
