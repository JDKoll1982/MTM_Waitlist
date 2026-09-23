namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// The answer to "set this part's picture", including the refusals that name the part they collide with.
/// </summary>
/// <remarks>
/// A refusal is a typed answer rather than an exception, because every refusal here is something a person has to
/// be told in their own words: the number is longer than the storage allows, or the name it would be stored under
/// already belongs to a different part. <see cref="CollidingPartNumber"/> carries the second case's part, which is
/// what makes the message actionable instead of merely negative.
/// </remarks>
public sealed class PartPictureSetResult
{
    /// <summary>Whether the picture was stored and recorded.</summary>
    public bool Success { get; init; }

    /// <summary>The system the part belongs to: <c>visual_part</c> or <c>wip_part</c>.</summary>
    public string Scope { get; init; } = string.Empty;

    /// <summary>The part number the save was attempted for.</summary>
    public string PartNumber { get; init; } = string.Empty;

    /// <summary>
    /// A stable code the screen maps to the person's words: <c>PART_NUMBER_TOO_LONG</c>, <c>NAME_COLLISION</c>,
    /// <c>NAME_NOT_USABLE</c>, <c>VALIDATION_FAILED</c>, <c>SHARE_UNREACHABLE</c> or <c>STORE_FAILED</c>.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>A plain statement of what was refused or what failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The part that already holds the file name this save wanted, when the refusal is a name collision.
    /// </summary>
    public string? CollidingPartNumber { get; init; }

    /// <summary>The path the picture was stored at, relative to the configured picture root.</summary>
    public string? StoredRelativePath { get; init; }

    /// <summary>Whether this save replaced a picture that was already there.</summary>
    public bool ReplacedExisting { get; init; }
}

/// <summary>
/// One entry in a part's change record: a set or a replace, who made it, when, and what it replaced.
/// </summary>
/// <remarks>
/// <see cref="PreviousImagePath"/> is null for a first picture, which is the whole reason the record can be read
/// as a history rather than as a list of current values. The actor's name is resolved when the record is read, so
/// a person who changes their display name is shown under the name they have today.
/// </remarks>
public sealed class PartPictureChangeEntry
{
    /// <summary>What the picture was before this change; null when this change is what first set one.</summary>
    public string? PreviousImagePath { get; init; }

    /// <summary>What the picture became.</summary>
    public string NewImagePath { get; init; } = string.Empty;

    /// <summary>The actor's identifier, or null when the change was made without one.</summary>
    public long? ChangedByUserId { get; init; }

    /// <summary>The actor's display name, or an empty string when it could not be resolved.</summary>
    public string ChangedByDisplayName { get; init; } = string.Empty;

    /// <summary>When the change was made, in UTC.</summary>
    public DateTime ChangedUtc { get; init; }

    /// <summary>Whether this entry is the one that first gave the part a picture.</summary>
    public bool IsFirstPicture => string.IsNullOrWhiteSpace(PreviousImagePath);
}
