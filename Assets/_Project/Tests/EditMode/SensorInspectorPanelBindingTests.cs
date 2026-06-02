using System.Collections.Generic;
using Bocage.Presentation.Bindings;
using Bocage.Sensors;
using NUnit.Framework;
using Weather = Bocage.SimulationCore.Model.Weather;
using MonthlyClimate = Bocage.SimulationCore.Model.MonthlyClimate;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for the pure helpers behind
    /// <see cref="SensorInspectorPanelBinding"/> (chantier E6 / ADR #53).
    /// The MonoBehaviour wiring (Q&lt;Label&gt; lookups, OnClick, Escape,
    /// .hidden toggling) is validated in Play Mode; here we exercise the
    /// allocation-free buffer extractors, the « jours consécutifs » trailing
    /// run counter, and the monthly-normal precipitation reconstruction.
    /// </summary>
    public sealed class SensorInspectorPanelBindingTests
    {
        [Test]
        public void ExtractMeasuredFloats_CopiesMeasuredFieldOnly()
        {
            var source = new List<SensorSample<double>>
            {
                new SensorSample<double>(1.5, 2.0),
                new SensorSample<double>(3.5, 4.0),
            };
            var dest = new List<float> { 99f, 99f, 99f }; // pre-populated, must be cleared
            SensorInspectorPanelBinding.ExtractMeasuredFloats(source, dest);

            Assert.AreEqual(2, dest.Count);
            Assert.AreEqual(1.5f, dest[0]);
            Assert.AreEqual(3.5f, dest[1]);
        }

        [Test]
        public void ExtractTruthFloats_CopiesTruthFieldOnly()
        {
            var source = new List<SensorSample<double>>
            {
                new SensorSample<double>(1.5, 2.0),
                new SensorSample<double>(3.5, 4.0),
            };
            var dest = new List<float>();
            SensorInspectorPanelBinding.ExtractTruthFloats(source, dest);

            Assert.AreEqual(2, dest.Count);
            Assert.AreEqual(2.0f, dest[0]);
            Assert.AreEqual(4.0f, dest[1]);
        }

        [Test]
        public void ExtractScalarFloats_CastsDoubleToFloat()
        {
            var source = new List<double> { 10.0, 20.5, -3.25 };
            var dest = new List<float>();
            SensorInspectorPanelBinding.ExtractScalarFloats(source, dest);

            Assert.AreEqual(3, dest.Count);
            Assert.AreEqual(10.0f, dest[0]);
            Assert.AreEqual(20.5f, dest[1]);
            Assert.AreEqual(-3.25f, dest[2]);
        }

        [Test]
        public void ExtractTemperatureFloats_ReadsTemperatureChannel()
        {
            var source = new List<Weather>
            {
                new Weather(12.0, 1.0),
                new Weather(22.5, 3.0),
            };
            var dest = new List<float>();
            SensorInspectorPanelBinding.ExtractTemperatureFloats(source, dest);

            Assert.AreEqual(2, dest.Count);
            Assert.AreEqual(12.0f, dest[0]);
            Assert.AreEqual(22.5f, dest[1]);
        }

        [Test]
        public void ExtractPrecipitationFloats_ReadsPrecipitationChannel()
        {
            var source = new List<Weather>
            {
                new Weather(12.0, 1.0),
                new Weather(22.5, 3.0),
            };
            var dest = new List<float>();
            SensorInspectorPanelBinding.ExtractPrecipitationFloats(source, dest);

            Assert.AreEqual(2, dest.Count);
            Assert.AreEqual(1.0f, dest[0]);
            Assert.AreEqual(3.0f, dest[1]);
        }

        [Test]
        public void ExtractAnything_NullOrEmptySource_LeavesDestEmpty()
        {
            var dest = new List<float> { 1f, 2f };
            SensorInspectorPanelBinding.ExtractScalarFloats(null, dest);
            Assert.AreEqual(0, dest.Count);

            dest.Add(1f);
            SensorInspectorPanelBinding.ExtractMeasuredFloats(null, dest);
            Assert.AreEqual(0, dest.Count);
        }

        [Test]
        public void TrailingDaysAboveThreshold_CountsTheMostRecentRun()
        {
            // Tail of 3 above threshold (4.0, 4.5, 5.0) preceded by a sub-threshold day.
            var hist = new List<SensorSample<double>>
            {
                new SensorSample<double>(2.0, 2.0),
                new SensorSample<double>(4.0, 4.0),
                new SensorSample<double>(3.0, 3.0),  // dip below threshold breaks the prior tail
                new SensorSample<double>(4.0, 4.0),
                new SensorSample<double>(4.5, 4.5),
                new SensorSample<double>(5.0, 5.0),
            };
            int n = SensorInspectorPanelBinding.ComputeTrailingDaysAboveThreshold(hist, 3.5);
            Assert.AreEqual(3, n);
        }

        [Test]
        public void TrailingDaysAboveThreshold_NoTrailingExceedance_ReturnsZero()
        {
            var hist = new List<SensorSample<double>>
            {
                new SensorSample<double>(4.0, 4.0),
                new SensorSample<double>(2.0, 2.0),
            };
            Assert.AreEqual(0, SensorInspectorPanelBinding.ComputeTrailingDaysAboveThreshold(hist, 3.5));
        }

        [Test]
        public void TrailingDaysAboveThreshold_EmptyOrNullHistory_ReturnsZero()
        {
            Assert.AreEqual(0, SensorInspectorPanelBinding.ComputeTrailingDaysAboveThreshold(new List<SensorSample<double>>(), 3.5));
            Assert.AreEqual(0, SensorInspectorPanelBinding.ComputeTrailingDaysAboveThreshold(null, 3.5));
        }

        [Test]
        public void MonthlyExpectedPrecipitationMm_MatchesAnalyticIdentity()
        {
            // Identity: daysInMonth × p_wet × exp(mu + sigma²/2).
            // January: 31 days × 0.40 × exp(1.25 + 0.5 × 0.80²) = 31 × 0.40 × exp(1.57) ≈ 31 × 0.40 × 4.81 ≈ 59.6 mm.
            var climate = new MonthlyClimate(10.0, 0.40, 1.25, 0.80);
            double mm = SensorInspectorPanelBinding.MonthlyExpectedPrecipitationMm(climate, monthIndex: 0);
            Assert.That(mm, Is.EqualTo(59.6).Within(0.5));
        }
    }
}
