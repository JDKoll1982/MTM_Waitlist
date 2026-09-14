namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One dunnage part the requesting job has assigned, as the New Request wizard reads it.
/// <para>
/// The wizard cannot see <c>Module_Setup</c>'s own part type, so the composition root maps the active-job
/// snapshot onto this value — the same arrangement that carries the job's component part numbers
/// (<see cref="RequestJobPartAvailability.ComponentPartNumbers"/>). It is a pure value: no connection, no read
/// of its own.
/// </para>
/// </summary>
public sealed class RequestDunnagePart
{
    /// <summary>The receiving store's dunnage part identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The dunnage type the part belongs to.</summary>
    public string TypeId { get; init; } = string.Empty;

    /// <summary>The part number — the value the card's second line carries.</summary>
    public string PartNumber { get; init; } = string.Empty;

    /// <summary>The name a person reads on the part's card.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// The part's picture as an absolute path, or empty when it has none. Resolved by the composition root, so
    /// the wizard never has to know where the shared dunnage image root lives.
    /// </summary>
    public string ImagePath { get; init; } = string.Empty;

    /// <summary>Whether the part has a picture — drives the card's image and its no-image placeholder.</summary>
    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

    /// <summary>Whether this part is one the job has assigned, rather than the operator's substitute.</summary>
    public bool IsAssignedToJob { get; init; }

    /// <summary>The secondary line under the name — the part number, and whether it came from the job.</summary>
    public string Summary => IsAssignedToJob
        ? PartNumber
        : string.IsNullOrWhiteSpace(PartNumber) ? "Substitute" : $"{PartNumber} · substitute";
}
