namespace Bocage.Sensors.Events
{
    /// <summary>
    /// Severity of an <see cref="IEvent"/> emitted by
    /// <see cref="EventDetector"/>. Drives downstream prioritisation
    /// (decision panel ordering, UI colouring) without prescribing a
    /// specific action.
    /// </summary>
    public enum EventSeverity
    {
        /// <summary>Informational signal. No action expected.</summary>
        Info = 0,

        /// <summary>Worth flagging to the user; a deferred decision is acceptable.</summary>
        Warning = 1,

        /// <summary>Demands immediate attention; user or auto-action should respond.</summary>
        Critical = 2,
    }
}
