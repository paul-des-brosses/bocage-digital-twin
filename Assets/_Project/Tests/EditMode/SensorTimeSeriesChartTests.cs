using Bocage.Presentation.UI;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for the pure coordinate helpers behind
    /// <see cref="SensorTimeSeriesChart"/> (chantier E6 / ADR #53). The
    /// custom Painter2D rendering itself can only be validated in Play
    /// Mode (visual check) — these tests pin down the index→X and
    /// value→Y math the renderer depends on, so a regression in the
    /// mapping fails loudly here rather than mis-plotting silently in
    /// the inspection panel.
    /// </summary>
    public sealed class SensorTimeSeriesChartTests
    {
        [Test]
        public void XForIndex_FirstSample_LandsAtChartLeft()
        {
            Assert.AreEqual(100f, SensorTimeSeriesChart.XForIndex(0, 10, 100f, 200f));
        }

        [Test]
        public void XForIndex_LastSample_LandsAtChartRight()
        {
            Assert.AreEqual(300f, SensorTimeSeriesChart.XForIndex(9, 10, 100f, 200f));
        }

        [Test]
        public void XForIndex_MiddleSample_LandsAtChartMiddle()
        {
            // index 5 of 11 samples → fraction 5/10 = 0.5 → 100 + 0.5*200 = 200.
            Assert.AreEqual(200f, SensorTimeSeriesChart.XForIndex(5, 11, 100f, 200f), 1e-5f);
        }

        [Test]
        public void XForIndex_SingleSample_CollapsesToLeftEdge()
        {
            // With only 1 sample, no horizontal range — clamp at left.
            Assert.AreEqual(100f, SensorTimeSeriesChart.XForIndex(0, 1, 100f, 200f));
            Assert.AreEqual(100f, SensorTimeSeriesChart.XForIndex(0, 0, 100f, 200f));
        }

        [Test]
        public void YForValue_AtYMax_LandsAtChartTop()
        {
            // UI Toolkit Y is inverted: high value → top of the area.
            Assert.AreEqual(50f, SensorTimeSeriesChart.YForValue(10f, 0f, 10f, 50f, 100f), 1e-5f);
        }

        [Test]
        public void YForValue_AtYMin_LandsAtChartBottom()
        {
            Assert.AreEqual(150f, SensorTimeSeriesChart.YForValue(0f, 0f, 10f, 50f, 100f), 1e-5f);
        }

        [Test]
        public void YForValue_AtMidpoint_LandsAtChartMiddle()
        {
            Assert.AreEqual(100f, SensorTimeSeriesChart.YForValue(5f, 0f, 10f, 50f, 100f), 1e-5f);
        }

        [Test]
        public void YForValue_DegenerateBounds_CollapsesToMidline()
        {
            // yMax == yMin → fallback to the mid of the chart area so the
            // line is at least visible rather than NaN/divide-by-zero.
            Assert.AreEqual(100f, SensorTimeSeriesChart.YForValue(42f, 5f, 5f, 50f, 100f), 1e-5f);
            Assert.AreEqual(100f, SensorTimeSeriesChart.YForValue(42f, 5f, 3f, 50f, 100f), 1e-5f);
        }
    }
}
