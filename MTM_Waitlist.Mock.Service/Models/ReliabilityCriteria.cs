namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The numbers SC-007 and SC-008 are measured over, in one place.
/// </summary>
/// <remarks>
/// <para>
/// These constants exist so the criteria cannot be made <b>unmeasurable by construction</b> — the failure
/// T230 exists to prevent. A 30-day criterion is only measurable if the evidence outlives the 30 days, so the
/// window length and everything sized against it are tied together here rather than restated at each site. An
/// edit to <see cref="MeasuredWindowDays"/> moves the retention with it, and a reader cannot raise the window
/// past the retention without the compiler pointing at the contradiction.
/// </para>
/// <para>
/// The same number appears as the <c>-Days</c> default of <c>tools/measure-reliability-window.ps1</c>, which is
/// a separate language and cannot reference this type. The tool asserts it found records, so a divergence shows
/// up as a failed measurement rather than as a quietly perfect score.
/// </para>
/// </remarks>
public static class ReliabilityCriteria
{
    /// <summary>How many days SC-007 and SC-008 are measured over.</summary>
    public const int MeasuredWindowDays = 30;

    /// <summary>
    /// How many days of run history are kept. <b>Deliberately longer than the window</b>: retention equal to
    /// the window would prune the first day's evidence at the moment the measurement needs it, and a service
    /// that has been running for exactly the measured period would always look like it had only just started.
    /// </summary>
    public const int HistoryRetentionDays = MeasuredWindowDays * 2;

    /// <summary>
    /// How many backup artifacts one store keeps. Must be at least <see cref="MeasuredWindowDays"/>: with one
    /// scheduled backup per store per day, a smaller count prunes the artifact SC-008 asks about before the
    /// question is asked. The extra fortnight is headroom for runs that were not scheduled — manual backups and
    /// the pre-restore safety snapshot — which count against the same retention.
    /// </summary>
    public const int BackupRetentionCount = MeasuredWindowDays + 14;
}
