using Microsoft.Extensions.Logging;

namespace MTM_Waitlist.Module_Logging;

/// <summary>
/// The one place that decides whether an entry is worth keeping, and the one place an <c>ILogger</c> level
/// becomes the store's severity (FR-032; `contracts/logging-contract.md` §1, §4).
/// </summary>
/// <remarks>
/// <para>
/// <b>One threshold, consulted by both surfaces.</b> The seam behind <see cref="ILogService"/> and the
/// <c>ILogger</c> provider beside it are two doors onto the same store, and a level that one honoured and the
/// other ignored would let entries of the same severity in through one door and not the other. Both ask here.
/// </para>
/// <para>
/// <b>The comparison is a threshold because the severity order says so.</b>
/// <see cref="LogSeverity"/> is declared least-severe-first, so "at least this severe" is the enum comparison and
/// there is no second ordering to keep in step.
/// </para>
/// <para>
/// <b>A level the application cannot read is not a level that silences the store.</b>
/// <see cref="Parse"/> answers the shipped default when the configured text names no severity, because a
/// mistyped setting must not be the reason no fault is recorded anywhere.
/// </para>
/// </remarks>
public sealed class LogStoreLevel
{
    /// <summary>The configuration section the threshold is read from: <c>LogStore</c>.</summary>
    public const string SectionName = "LogStore";

    /// <summary>The key inside <see cref="SectionName"/>: <c>MinimumLevel</c>, a severity name.</summary>
    public const string MinimumLevelKey = "MinimumLevel";

    /// <summary>
    /// The shipped default severity: warnings and faults are kept, and ordinary progress lines are dropped.
    /// </summary>
    public const LogSeverity ShippedMinimum = LogSeverity.Warning;

    /// <summary>
    /// The severities a person may choose between, least severe first, which is also the order the store's own
    /// vocabulary declares. Exposed as data so the settings screen's list and this type cannot disagree about
    /// what may be chosen.
    /// </summary>
    public static readonly IReadOnlyList<LogSeverity> Chooseable = Enum.GetValues<LogSeverity>();

    private LogSeverity _minimum;

    /// <summary>Creates a threshold.</summary>
    /// <param name="minimum">The least severe entry the store keeps.</param>
    public LogStoreLevel(LogSeverity minimum = ShippedMinimum) => _minimum = minimum;

    /// <summary>The least severe entry the store keeps.</summary>
    /// <remarks>
    /// <para>
    /// Read on every write, because the value is adopted again whenever the plant setting changes — a developer
    /// widening the store to <c>Info</c> in Settings must change what is kept from that moment, without a
    /// restart and without a rebuild (FR-032).
    /// </para>
    /// <para>
    /// A plain field rather than a synchronised one: the value is an <c>int</c>-backed enum, so a read or a write
    /// of it is already atomic, and the worst a race can cost is one entry kept or dropped by the value that was
    /// in force a moment earlier. The other adoptable settings in this tree are held the same way.
    /// </para>
    /// </remarks>
    public LogSeverity MinimumLevel => _minimum;

    /// <summary>
    /// Adopts a severity as the new threshold, so the change is in force for the next entry raised.
    /// </summary>
    /// <param name="minimum">The least severe entry the store keeps from now on.</param>
    public void Adopt(LogSeverity minimum) => _minimum = minimum;

    /// <summary>Whether an entry of this severity is kept.</summary>
    /// <param name="severity">The entry's severity, from the store's own vocabulary.</param>
    /// <returns><see langword="true"/> when the entry is at least as severe as the threshold.</returns>
    public bool Keeps(LogSeverity severity) => severity >= MinimumLevel;

    /// <summary>Whether an <c>ILogger</c> call at this level is kept.</summary>
    /// <param name="logLevel">The level the call site used.</param>
    /// <returns>
    /// <see langword="false"/> for <see cref="LogLevel.None"/>, which is not a severity but the absence of one,
    /// and the threshold's answer for every other level.
    /// </returns>
    public bool Keeps(LogLevel logLevel) => logLevel != LogLevel.None && Keeps(SeverityOf(logLevel));

    /// <summary>
    /// Projects an <c>ILogger</c> level onto the severity vocabulary the store's <c>level</c> column holds.
    /// </summary>
    /// <param name="logLevel">The level the call site used.</param>
    /// <returns>The matching severity.</returns>
    /// <remarks>
    /// The pairings are the store's own: the framework's two diagnostic levels are both the store's
    /// <see cref="LogSeverity.Debug"/>, and anything the framework adds later is treated as informational rather
    /// than dropped, so an unrecognised level is recorded as what it most resembles.
    /// </remarks>
    public static LogSeverity SeverityOf(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace or LogLevel.Debug => LogSeverity.Debug,
        LogLevel.Warning => LogSeverity.Warning,
        LogLevel.Error => LogSeverity.Error,
        LogLevel.Critical => LogSeverity.Critical,
        _ => LogSeverity.Info,
    };

    /// <summary>
    /// Reads a severity from the text a deployment or a settings screen supplied, answering the shipped default
    /// when the text names no severity.
    /// </summary>
    /// <param name="text">The configured value, or null or blank when nothing is configured.</param>
    /// <returns>The named severity, or <see cref="ShippedMinimum"/>.</returns>
    /// <remarks>
    /// A mistyped setting answers the default rather than silencing the store, because a value the application
    /// cannot understand must never be the reason no fault is recorded anywhere.
    /// </remarks>
    public static LogSeverity ParseSeverity(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ShippedMinimum;
        }

        return Enum.TryParse<LogSeverity>(text.Trim(), ignoreCase: true, out var severity)
            && Enum.IsDefined(severity)
            ? severity
            : ShippedMinimum;
    }
}
