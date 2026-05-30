using System.Reflection;
using Bocage.Presentation.Scene.Fauna;
using NUnit.Framework;
using UnityEngine;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for the pure-compute spawn-rate formula exposed
    /// by <see cref="FaunaPoolBinding.ComputeEffectiveSpawnRate"/>: zero
    /// below the species threshold, linear from there up to λ_max at
    /// biodiv = 1. The probabilistic Bernoulli roll itself runs in the
    /// Update loop and is not covered here (would need Play Mode).
    /// </summary>
    public sealed class FaunaPoolBindingTests
    {
        [Test]
        public void EffectiveSpawnRate_BelowOrAtThreshold_IsZero()
        {
            var sp = MakeSpecies(threshold: 0.3f, lambdaMax: 0.1f);

            Assert.AreEqual(0f, FaunaPoolBinding.ComputeEffectiveSpawnRate(sp, 0f), 1e-6f);
            Assert.AreEqual(0f, FaunaPoolBinding.ComputeEffectiveSpawnRate(sp, 0.2f), 1e-6f);
            Assert.AreEqual(0f, FaunaPoolBinding.ComputeEffectiveSpawnRate(sp, 0.3f), 1e-6f);

            Object.DestroyImmediate(sp);
        }

        [Test]
        public void EffectiveSpawnRate_AboveThreshold_LinearUpToLambdaMax()
        {
            var sp = MakeSpecies(threshold: 0.3f, lambdaMax: 0.1f);

            // biodiv = 0.65 → t = (0.65 - 0.3) / 0.7 = 0.5 → 0.05
            Assert.AreEqual(0.05f,
                FaunaPoolBinding.ComputeEffectiveSpawnRate(sp, 0.65f), 1e-4f);

            // biodiv = 1 → t = 1 → λ_max = 0.1
            Assert.AreEqual(0.1f,
                FaunaPoolBinding.ComputeEffectiveSpawnRate(sp, 1f), 1e-4f);

            // biodiv = 0.5 → t ≈ 0.2857 → ~0.02857
            Assert.AreEqual(0.02857f,
                FaunaPoolBinding.ComputeEffectiveSpawnRate(sp, 0.5f), 1e-4f);

            Object.DestroyImmediate(sp);
        }

        private static FaunaSpeciesDefinition MakeSpecies(float threshold, float lambdaMax)
        {
            var sp = ScriptableObject.CreateInstance<FaunaSpeciesDefinition>();
            SetPrivateField(sp, "appearanceThreshold", threshold);
            SetPrivateField(sp, "spawnRateAtMaxBiodiv", lambdaMax);
            return sp;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var fi = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }
    }
}
