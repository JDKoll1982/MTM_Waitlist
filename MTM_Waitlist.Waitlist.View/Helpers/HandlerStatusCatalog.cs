using System.Collections.ObjectModel;

namespace MTM_Waitlist.Module_Waitlist.Helpers;

/// <summary>
/// The four conditions a material handler can report from a request card, verbatim and in their settled order.
/// </summary>
/// <remarks>
/// <para>
/// The vocabulary is closed (FR-071, FR-102). These four strings are exactly what the store holds, and nothing
/// else is a condition: a stored value that is not one of them, or that is null or blank, is not a condition, so
/// the card's status word falls through to the overdue marking or the lifecycle word rather than claiming a
/// condition nobody reported (<c>data-model.md</c> §2).
/// </para>
/// <para>
/// The list is the one source of truth for the four, so the picker that writes a condition and the card that
/// reads one back cannot drift apart — the same reason <see cref="RequestStatusPalette"/> owns the badge
/// colours.
/// </para>
/// </remarks>
public static class HandlerStatusCatalog
{
    private static readonly ReadOnlyCollection<string> s_conditions = new(
    [
        "Product Buried",
        "Cant Find Product",
        "Changing Forklift",
        "Changing Propane",
    ]);

    /// <summary>
    /// The four conditions, in their settled order — the order the card's picker offers them.
    /// </summary>
    /// <remarks>
    /// Read-only because it is the vocabulary, not a copy of it: a caller that could edit this list could
    /// change what the whole feature accepts.
    /// </remarks>
    public static IReadOnlyList<string> Conditions => s_conditions;

    /// <summary>
    /// Answers whether a stored handler status is one of the four conditions.
    /// </summary>
    /// <param name="handlerStatus">The stored value, or <see langword="null"/> when the row carries none.</param>
    /// <returns>
    /// <see langword="true"/> only for one of the four verbatim strings. The comparison is ordinal and exact, so
    /// a differently cased, padded or otherwise altered value is not a condition.
    /// </returns>
    public static bool IsCondition(string? handlerStatus) =>
        handlerStatus is not null && s_conditions.Contains(handlerStatus, StringComparer.Ordinal);
}
