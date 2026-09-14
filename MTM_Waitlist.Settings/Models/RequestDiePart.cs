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
    /// The die's two-part identifier as one string: the die's number and where the die is, e.g.
    /// <c>FGT0002000-DIE SHOP</c>. With no known location it is the number alone, so the separator never dangles
    /// (FR-056). The composition lives here so a card built from a stored request and one built from the live job
    /// cannot format the same die two different ways.
    /// </summary>
    public string Label => ComposeLabel(PartNumber, Location);

    /// <summary>Composes a die's two-part identifier, tolerating either half being absent.</summary>
    public static string ComposeLabel(string? partNumber, string? location)
    {
        var number = partNumber?.Trim() ?? string.Empty;
        if (number.Length == 0)
        {
            return string.Empty;
        }

        var where = location?.Trim() ?? string.Empty;
        return where.Length == 0 ? number : $"{number}-{where}";
    }

    /// <summary>The secondary line under the die's number on a card — the part the die is assigned to.</summary>
    public string Summary => string.IsNullOrWhiteSpace(Description)
        ? (HasLocation ? Location : "Die")
        : Description;
}
