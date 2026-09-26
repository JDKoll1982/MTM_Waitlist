namespace MTM_Waitlist.Module_Logging;

/// <summary>
/// The single severity vocabulary of the log store (FR-032, `contracts/logging-contract.md` §1).
/// </summary>
/// <remarks>
/// <para>
/// One vocabulary rather than several, so a fault the launch surface shows on the splash and the entry it wrote
/// are filterable together and cannot drift into two spellings. The value names map to the store's
/// <c>ops_startup_logs.level</c> column, which is what the developer panel filters on.
/// </para>
/// <para>
/// The member order is deliberate and must not be reordered: it is descending criticality, so a comparison is a
/// threshold. <see cref="Debug"/> is the least severe value the store accepts and <see cref="Critical"/> the
/// most.
/// </para>
/// </remarks>
public enum LogSeverity
{
    /// <summary>Diagnostic detail: useful while reading a sequence, never a fault.</summary>
    Debug,

    /// <summary>Something happened that the caller expected.</summary>
    Info,

    /// <summary>Something happened that did not stop the caller but is worth a reader's attention.</summary>
    Warning,

    /// <summary>The operation under way failed.</summary>
    Error,

    /// <summary>The operation under way failed and the application could not continue as it was.</summary>
    Critical,
}
