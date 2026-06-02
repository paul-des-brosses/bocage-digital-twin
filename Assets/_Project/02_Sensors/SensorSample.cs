namespace Bocage.Sensors
{
    /// <summary>
    /// Paired snapshot of one sensor read: the noisy <see cref="Measured"/>
    /// value the sensor returns and the corresponding <see cref="Truth"/>
    /// value of the underlying model state at the same simulated day.
    /// Storing both inside the rolling history lets the inspection panel
    /// (chantier E6 / ADR #53) plot the two as overlaid series so the user
    /// SEES the measurement uncertainty — a core pedagogical goal of the
    /// panel (acoustic/camera reading vs true fauna abundance, piezometer
    /// reading vs true water-table depth).
    /// </summary>
    /// <typeparam name="T">The sample type (typically <c>double</c>).</typeparam>
    public readonly struct SensorSample<T>
    {
        public T Measured { get; }
        public T Truth { get; }

        public SensorSample(T measured, T truth)
        {
            Measured = measured;
            Truth = truth;
        }
    }
}
