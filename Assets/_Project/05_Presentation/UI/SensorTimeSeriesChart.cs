using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.UI
{
    /// <summary>
    /// Custom UI Toolkit <see cref="VisualElement"/> that draws one or more
    /// time-series as overlaid line charts (chantier E6 / ADR #53 inspection
    /// panel). Rendering goes through <c>generateVisualContent</c> +
    /// <c>Painter2D</c> (Unity 2022+/Unity 6), so the chart lives in the
    /// regular UI Toolkit layout and shares the dark theme without needing
    /// a separate Canvas.
    /// <para>
    /// Designed for the inspection panel use-cases listed in ADR #53:
    /// piezometer depth vs. alert thresholds, weather station T° &amp;
    /// precip vs. monthly normals, acoustic/camera abundance (measured vs.
    /// ground-truth), EddyTower CO2 flux. The chart itself draws only the
    /// curves and the horizontal threshold lines — axis labels are owned
    /// by the surrounding UXML so binding code stays free to format them.
    /// </para>
    /// <para>
    /// All public setters trigger <see cref="VisualElement.MarkDirtyRepaint"/>
    /// internally, so binding code only has to (re-)populate the series and
    /// thresholds — the redraw is automatic. Backing lists are pre-allocated
    /// and reused across redraws to honour CLAUDE.md §6 (no per-frame
    /// allocation in the hot path).
    /// </para>
    /// </summary>
    public sealed class SensorTimeSeriesChart : VisualElement
    {
        /// <summary>One line drawn on the chart.</summary>
        public struct Series
        {
            public Color Color;
            public float LineWidth;
            public IReadOnlyList<float> Values;
        }

        /// <summary>One horizontal reference line (e.g. drought threshold, monthly normal).</summary>
        public struct Threshold
        {
            public Color Color;
            public float LineWidth;
            public float Value;
        }

        private readonly List<Series> _series = new List<Series>();
        private readonly List<Threshold> _thresholds = new List<Threshold>();
        private float _yMin = 0f;
        private float _yMax = 1f;

        public SensorTimeSeriesChart()
        {
            AddToClassList("sensor-chart");
            generateVisualContent += OnGenerateVisualContent;
        }

        public void ClearSeries()
        {
            if (_series.Count == 0) return;
            _series.Clear();
            MarkDirtyRepaint();
        }

        public void AddSeries(Color color, float lineWidth, IReadOnlyList<float> values)
        {
            _series.Add(new Series { Color = color, LineWidth = lineWidth, Values = values });
            MarkDirtyRepaint();
        }

        public void ClearThresholds()
        {
            if (_thresholds.Count == 0) return;
            _thresholds.Clear();
            MarkDirtyRepaint();
        }

        public void AddThreshold(Color color, float lineWidth, float value)
        {
            _thresholds.Add(new Threshold { Color = color, LineWidth = lineWidth, Value = value });
            MarkDirtyRepaint();
        }

        public void SetYBounds(float yMin, float yMax)
        {
            if (yMin == _yMin && yMax == _yMax) return;
            _yMin = yMin;
            _yMax = yMax;
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            Rect r = contentRect;
            if (r.width < 4f || r.height < 4f) return;
            if (_yMax <= _yMin) return;

            var p = ctx.painter2D;

            // Threshold lines first so series strokes draw on top.
            for (int i = 0; i < _thresholds.Count; i++)
            {
                Threshold t = _thresholds[i];
                float y = YForValue(t.Value, _yMin, _yMax, r.y, r.height);
                p.lineWidth = t.LineWidth;
                p.strokeColor = t.Color;
                p.BeginPath();
                p.MoveTo(new Vector2(r.x, y));
                p.LineTo(new Vector2(r.xMax, y));
                p.Stroke();
            }

            // Series lines (broken line, sample-to-sample).
            for (int i = 0; i < _series.Count; i++)
            {
                Series s = _series[i];
                if (s.Values == null || s.Values.Count < 2) continue;
                p.lineWidth = s.LineWidth;
                p.strokeColor = s.Color;
                p.BeginPath();
                int n = s.Values.Count;
                p.MoveTo(new Vector2(
                    XForIndex(0, n, r.x, r.width),
                    YForValue(s.Values[0], _yMin, _yMax, r.y, r.height)));
                for (int j = 1; j < n; j++)
                {
                    p.LineTo(new Vector2(
                        XForIndex(j, n, r.x, r.width),
                        YForValue(s.Values[j], _yMin, _yMax, r.y, r.height)));
                }
                p.Stroke();
            }
        }

        /// <summary>
        /// Maps a sample index to its X coordinate, distributing samples
        /// linearly from <paramref name="chartLeft"/> (index 0) to
        /// <c>chartLeft + chartWidth</c> (last index). Single-sample
        /// edge case (<paramref name="sampleCount"/> ≤ 1) collapses to
        /// the left edge. Pure — covered by EditMode tests.
        /// </summary>
        public static float XForIndex(int index, int sampleCount, float chartLeft, float chartWidth)
        {
            if (sampleCount <= 1) return chartLeft;
            return chartLeft + (chartWidth * index) / (sampleCount - 1);
        }

        /// <summary>
        /// Maps a Y value to its screen coordinate inside a vertical
        /// strip of <paramref name="chartHeight"/> pixels starting at
        /// <paramref name="chartTop"/>. UI Toolkit Y is inverted (0 at
        /// top), so <paramref name="yMax"/> maps to <paramref name="chartTop"/>
        /// and <paramref name="yMin"/> to <c>chartTop + chartHeight</c>.
        /// Degenerate bounds (<paramref name="yMax"/> ≤ <paramref name="yMin"/>)
        /// collapse to the mid-line. Pure — covered by EditMode tests.
        /// </summary>
        public static float YForValue(float value, float yMin, float yMax, float chartTop, float chartHeight)
        {
            if (yMax <= yMin) return chartTop + chartHeight * 0.5f;
            float t = (value - yMin) / (yMax - yMin);
            return chartTop + chartHeight * (1f - t);
        }
    }
}
