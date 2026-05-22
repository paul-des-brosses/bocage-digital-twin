namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Final state of a recommendation in the
    /// <see cref="DecisionJournal"/>. Drives the UI (accept / reject
    /// buttons → Accepted / Rejected) and the auto-actions path
    /// (Pending → user arbitration; AutoAccepted → applied without
    /// arbitration on the next tick).
    /// </summary>
    public enum DecisionVerdict
    {
        /// <summary>Awaiting user arbitration.</summary>
        Pending = 0,

        /// <summary>User accepted the recommendation.</summary>
        Accepted = 1,

        /// <summary>User rejected the recommendation.</summary>
        Rejected = 2,

        /// <summary>The recommendation is configured to apply automatically without arbitration.</summary>
        AutoAccepted = 3,
    }
}
