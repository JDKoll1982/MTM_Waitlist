namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// The two numbers a screen shows side by side, and must never conflate (FR-017, FR-018):
/// <list type="bullet">
/// <item>the Item's <b>configured</b> allotted minutes — how long it is <i>allowed</i> to take; and</item>
/// <item>the <b>observed</b> average of its completed requests — how long it has <i>actually</i> taken.</item>
/// </list>
/// The observed half is <b>derived and never stored</b>, and is never written back (FR-019). It carries no
/// value at all when no request for the Item has completed — an absence, never a fabricated zero (FR-026).
/// </summary>
public sealed class RequestItemObservedTime
{
    /// <summary>The Item code these figures belong to.</summary>
    public string Item { get; init; } = string.Empty;

    /// <summary>How many of the Item's requests count towards the observed average.</summary>
    public int CompletedRequestCount { get; init; }

    /// <summary>The Item's configured allotment, or the labelled default when none is configured.</summary>
    public TimeSpan ConfiguredMinutes { get; init; }

    /// <summary>
    /// Whether <see cref="ConfiguredMinutes"/> is the fallback <b>default</b> rather than a value someone
    /// configured for this Item. The screen labels it as a default (FR-017).
    /// </summary>
    public bool IsConfiguredValueDefault { get; init; }

    /// <summary>
    /// The observed average of <c>completed_utc - accepted_utc</c> over the Item's completed requests, or
    /// <b>null</b> when the Item has none. Null means "no value", not "zero minutes".
    /// </summary>
    public TimeSpan? ObservedAverage { get; init; }

    /// <summary>Whether the Item has an observed average to show at all.</summary>
    public bool HasObservedAverage => ObservedAverage.HasValue;
}
