namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One die (an <c>FGT</c> part) assigned to the requesting job, as the New Request wizard reads it.
/// <para>
/// The wizard cannot see <c>Module_Setup</c>'s own part type, so the composition root maps the active-job
/// snapshot onto this value — the same arrangement that carries the job's components and its dunnage parts. It is
/// a pure value: no connection, no read of its own.
/// </para>
/// </summary>
public sealed class RequestDiePart
{
    /// <summary>The die's part number — the <c>FGT</c> identifier (FR-054).</summary>
    public string PartNumber { get; init; } = string.Empty;

    /// <summary>The die's location, or empty when the job's die row carries none.</summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>The die's own description, or empty.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Whether the die's location is known — the card shows only what exists.</summary>
    public bool HasLocation => !string.IsNullOrWhiteSpace(Location);

    /// <summary>
    /// The die's identifier: <b>its own number alone</b>, e.g. <c>FGT0002000</c>. Where the die lives is
    /// deliberately <b>not</b> part of the name — the request page lists every die location the job carries
    /// (FR-057), so folding one location into the identifier duplicated it on the card and made two halves of one
    /// fact look like one string. The composition lives here so a card built from a stored request and one built
    /// from the live job cannot format the same die two different ways.
    /// </summary>
    public string Label => ComposeLabel(PartNumber);

    /// <summary>
    /// Composes a die's identifier from the die's number, which is the whole of it. A row with no number has no
    /// identifier to show.
    /// </summary>
    public static string ComposeLabel(string? partNumber) => partNumber?.Trim() ?? string.Empty;

    /// <summary>The secondary line under the die's number on a card — the part the die is assigned to.</summary>
    public string Summary => string.IsNullOrWhiteSpace(Description)
        ? (HasLocation ? Location : "Die")
        : Description;
}
