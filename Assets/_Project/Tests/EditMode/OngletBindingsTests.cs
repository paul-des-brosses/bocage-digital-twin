using System.Collections.Generic;
using Bocage.Indicators.Hero;
using Bocage.Presentation.Bindings;
using Bocage.Presentation.Scene.Fauna;
using Bocage.SimulationCore.Model;
using NUnit.Framework;
using UnityEngine;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for the pure-compute helpers behind the three Niveau
    /// B panels (chantier E6 / ADR #54). The UI plumbing (UIDocument label
    /// queries, OnChanged wiring) is not unit-tested — same boundary as the
    /// other label bindings — only the non-trivial aggregation/formatting
    /// logic each panel relies on.
    /// </summary>
    public sealed class OngletClimatBindingTests
    {
        [Test]
        public void MeanTemperatureCelsius_EmptyOrNull_ReturnsZero()
        {
            Assert.AreEqual(0.0, OngletClimatBinding.MeanTemperatureCelsius(new List<Weather>()), 1e-9);
            Assert.AreEqual(0.0, OngletClimatBinding.MeanTemperatureCelsius(null), 1e-9);
        }

        [Test]
        public void MeanTemperatureCelsius_AveragesTemperatureChannel()
        {
            var history = new List<Weather>
            {
                new Weather(10.0, 1.0),
                new Weather(20.0, 2.0),
                new Weather(30.0, 3.0),
            };
            Assert.AreEqual(20.0, OngletClimatBinding.MeanTemperatureCelsius(history), 1e-9);
        }

        [Test]
        public void CumulativePrecipitationMm_SumsPrecipitationChannel()
        {
            var history = new List<Weather>
            {
                new Weather(10.0, 1.5),
                new Weather(20.0, 2.5),
                new Weather(30.0, 4.0),
            };
            Assert.AreEqual(8.0, OngletClimatBinding.CumulativePrecipitationMm(history), 1e-9);
        }
    }

    public sealed class OngletEconomieBindingTests
    {
        [Test]
        public void ComputePse_IsHedgerowDensityTimesRate()
        {
            Assert.AreEqual(90.0, OngletEconomieBinding.ComputePseEurosPerHectare(90.0, 1.0), 1e-9);
            Assert.AreEqual(0.0, OngletEconomieBinding.ComputePseEurosPerHectare(90.0, 0.0), 1e-9);
        }

        [Test]
        public void ComputePac_WithoutHedges_IsBasicSupportOnly()
        {
            Assert.AreEqual(
                IntegratedProfitabilityIndicator.BasicCapPaymentEurosPerHectare,
                OngletEconomieBinding.ComputePacEurosPerHectare(0.0), 1e-9);
        }

        [Test]
        public void ComputePac_WithHedges_AddsTheHedgeBonus()
        {
            double expected = IntegratedProfitabilityIndicator.BasicCapPaymentEurosPerHectare
                              + IntegratedProfitabilityIndicator.PacHedgeBonusEurosPerHectare;
            Assert.AreEqual(expected, OngletEconomieBinding.ComputePacEurosPerHectare(75.0), 1e-9);
        }
    }

    public sealed class OngletBiodivBindingTests
    {
        [Test]
        public void CountDistinctSpecies_CountsUniqueIgnoringDuplicatesAndNulls()
        {
            var a = ScriptableObject.CreateInstance<FaunaSpeciesDefinition>();
            var b = ScriptableObject.CreateInstance<FaunaSpeciesDefinition>();
            var buffer = new List<FaunaSpeciesDefinition>();
            var visible = new List<FaunaSpeciesDefinition> { a, b, a, null, b };

            Assert.AreEqual(2, OngletBiodivBinding.CountDistinctSpecies(visible, buffer));

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void CountDistinctSpecies_EmptyOrNull_ReturnsZero()
        {
            var buffer = new List<FaunaSpeciesDefinition>();
            Assert.AreEqual(0, OngletBiodivBinding.CountDistinctSpecies(new List<FaunaSpeciesDefinition>(), buffer));
            Assert.AreEqual(0, OngletBiodivBinding.CountDistinctSpecies(null, buffer));
        }
    }
}
