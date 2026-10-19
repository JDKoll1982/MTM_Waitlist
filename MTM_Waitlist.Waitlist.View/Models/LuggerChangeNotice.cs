using System.Globalization;

using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// One change to the lugger arrangement as the waitlist shows it: what changed, against which lugger and scrap
/// type, the value before and after, the reason, who made the change and when (FR-035).
/// </summary>
/// <remarks>
/// <para>
/// The record keeps the stored shape it was read from rather than a pre-rendered string, and carries the wording
/// it was turned into as a resource template plus that template's arguments. That is what lets the sentence be
/// read in the application's own language and lets the words be proved without a resource file: the text is the
/// localized template with these arguments, never an English sentence assembled here.
/// </para>
/// <para>
/// <see cref="Id"/> is the change row's number and the poll's watermark, so one notice is one change and the strip
/// can never show the same change twice.
/// </para>
/// </remarks>
public sealed record LuggerChangeNotice
{
    /// <summary><c>id</c>: the change row's number, and the watermark the poll advances to.</summary>
    public long Id { get; init; }

    /// <summary><c>change_kind</c>: one of the pinned kind strings, held in <c>ArrangementChange.Kinds</c>.</summary>
    public string ChangeKind { get; init; } = string.Empty;

    /// <summary><c>lugger_name</c>: the lugger the change was made against.</summary>
    public string LuggerName { get; init; } = string.Empty;

    /// <summary><c>scrap_type</c>: the affected scrap type, or <see langword="null"/> when no assignment was touched.</summary>
    public string? ScrapType { get; init; }

    /// <summary><c>previous_value</c>: the value before the change, read according to <see cref="ChangeKind"/>.</summary>
    public string? PreviousValue { get; init; }

    /// <summary><c>new_value</c>: the value after the change, read according to <see cref="ChangeKind"/>.</summary>
    public string? NewValue { get; init; }

    /// <summary><c>change_reason</c>: the reason recorded with a <c>Do Not Use</c> status.</summary>
    public string? ChangeReason { get; init; }

    /// <summary><c>changed_by_employee_name</c>: who made the change.</summary>
    public string ChangedByEmployeeName { get; init; } = string.Empty;

    /// <summary><c>changed_utc</c>: when the change was made.</summary>
    public DateTime ChangedUtc { get; init; }

    /// <summary>The resource key whose template states this change in plain words.</summary>
    public string MessageTemplate { get; init; } = string.Empty;

    /// <summary>The values the template is formatted with, in the order the template names them.</summary>
    public IReadOnlyList<string> MessageArguments { get; init; } = Array.Empty<string>();

    /// <summary>
    /// The sentence the bar shows: the localized template with <see cref="MessageArguments"/>. A missing resource
    /// falls back to its own key, as everywhere else in the application.
    /// </summary>
    public string Text => string.Format(CultureInfo.CurrentCulture, MessageTemplate.GetLocalized(), [.. MessageArguments]);
}
