using System.Globalization;

namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// One row of the launch window's feed: a line the launch wrote, shaped for the surface that draws it
/// (`contracts/launch-step-contract.md` §3, FR-002).
/// </summary>
/// <remarks>
/// <para>
/// <b>The surface draws what the launch wrote; it decides nothing.</b> A row holds the line's own text, the
/// target it is about and a time a person can read at a glance. Nothing here is derived from the launch's state,
/// so a row cannot say something the feed never said.
/// </para>
/// <para>
/// <b>The diagnosis is carried on the row that caused it.</b> A step that failed writes its diagnosis as the
/// line's own text, so the row naming the failure is the row saying why, and the strip at the bottom of the
/// window repeats that text rather than summarising it into a second account of the same event (FR-004).
/// </para>
/// </remarks>
/// <param name="TimestampUtc">When the launch wrote the line.</param>
/// <param name="Text">What the line says, in plain language.</param>
/// <param name="Target">
/// The store, share or server the line is about, or <c>null</c> when the line is about nothing a person could
/// name.
/// </param>
public sealed record SplashFeedLine(
    DateTimeOffset TimestampUtc,
    string Text,
    string? Target)
{
    /// <summary>
    /// The time the line was written, on this computer's own clock, for the row's leading column. Only the time of
    /// day is shown: a launch lasts seconds, and the window is read while it is happening.
    /// </summary>
    public string TimeText => TimestampUtc.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>Builds the row for one line the launch wrote.</summary>
    /// <param name="entry">The line, as the launch appended it.</param>
    /// <returns>The row the surface draws.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <c>null</c>.</exception>
    public static SplashFeedLine From(LaunchFeedEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new SplashFeedLine(entry.TimestampUtc, entry.Text, entry.Target);
    }
}
