namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Where the value in force for one permission came from, which is what the permissions page shows on each row
/// (FR-065).
/// </summary>
public enum PermissionProvenance
{
    /// <summary>A choice was made for this person, and their own row carries it.</summary>
    Chosen,

    /// <summary>Nothing was chosen for this person, so their role's baseline supplies the answer.</summary>
    Inherited,

    /// <summary>Neither the person nor their role has a row, so the shipped fallback answers.</summary>
    Fallback,
}

/// <summary>
/// One permission as it stands for one person: the key, the value in force, and where that value came from
/// (FR-065).
/// </summary>
/// <remarks>
/// The person's own row, then their role's baseline, then the shipped fallback — the same order the gate resolves
/// by (FR-049), which is what keeps the page and the control it describes from disagreeing.
/// </remarks>
public sealed record PermissionValueRow(string Key, bool Value, PermissionProvenance Provenance);

/// <summary>
/// One entry of a change set: the key, the value the caller last saw, and the value to write (FR-069, FR-070).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="From"/> is <see langword="null"/> when the person had no stored value of their own for this key, and
/// the store refuses the write if one has appeared since. <see cref="To"/> is <see langword="null"/> to remove the
/// person's own value so the role's baseline applies again, which is what reversing a save restores when there was
/// no row before it.
/// </para>
/// </remarks>
public sealed record PermissionChange(string Key, bool? From, bool? To);

/// <summary>What a change set or a reversal produced (FR-026).</summary>
public enum PermissionChangeOutcomeKind
{
    /// <summary>The write happened.</summary>
    Succeeded,

    /// <summary>The person's role is above the reader's own rung, so nothing about them can be changed.</summary>
    TargetOutranksActor,

    /// <summary>The set named the permission that opens this page, which is fixed (FR-059).</summary>
    GateFixed,

    /// <summary>The set named a key outside the permission namespace.</summary>
    KeyInvalid,

    /// <summary>A value moved since the caller last saw it, and the key that moved is named (FR-070).</summary>
    ValueMoved,

    /// <summary>The store could not be reached, and nothing was written.</summary>
    StoreUnavailable,

    /// <summary>There was nothing to reverse: no permission change is recorded against this person.</summary>
    NothingToReverse,
}

/// <summary>
/// The typed answer from a change-set write, carrying the resource key and the resolved sentence so every caller
/// reports a refusal in the same words (FR-026).
/// </summary>
/// <remarks>
/// <see cref="MovedKey"/> is filled only by <see cref="PermissionChangeOutcomeKind.ValueMoved"/>: which key moved
/// is reported rather than only that something did, so the page can show what the value is now and ask before
/// restoring (FR-070).
/// </remarks>
public sealed record PermissionChangeResult(
    PermissionChangeOutcomeKind Kind,
    string MessageKey,
    string Message,
    string MovedKey = "")
{
    /// <summary>Whether the write happened.</summary>
    public bool IsSuccess => Kind == PermissionChangeOutcomeKind.Succeeded;

    /// <summary>The answer for a write that happened.</summary>
    public static PermissionChangeResult Succeeded() =>
        new(PermissionChangeOutcomeKind.Succeeded, PermissionAdministrationMessages.SavedKey, PermissionAdministrationMessages.Saved);

    /// <summary>The answer for a write that did not happen, in the same words wherever it is shown.</summary>
    public static PermissionChangeResult Failed(PermissionChangeOutcomeKind kind, string messageKey, string message, string movedKey = "") =>
        new(kind, messageKey, message, movedKey);
}

/// <summary>One person who differs from their role's baseline for a feature (FR-076).</summary>
public sealed record PermissionHolder(
    long UserId,
    string DisplayName,
    string EmployeeIdentifier,
    string RoleCode,
    bool IsSwitchedOff,
    bool IsGranted);

/// <summary>
/// The answer to "who holds this feature": the roles whose baselines give it, then only the people who differ
/// from their role (FR-075, FR-076).
/// </summary>
public sealed record PermissionHolders(
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<PermissionHolder> People)
{
    /// <summary>Whether nobody holds it, which the view states in words rather than showing an empty region.</summary>
    public bool NobodyHoldsIt => RoleCodes.Count == 0 && People.Count == 0;
}

/// <summary>
/// The permission side of the administration area: what one person's permissions are, one atomic change set, and
/// the reversal of the last one (FR-063, FR-069, FR-072).
/// </summary>
/// <remarks>
/// <para>
/// One person at a time, and never a role. Nothing here edits a role's baseline: the baselines are seeded rows,
/// and changing what a role may do is a data change rather than a screen (FR-063, FR-048).
/// </para>
/// <para>
/// A save and its reversal are the same call, so a save of several permissions either all lands or none of it does
/// (FR-071). The reversal reads the recorded history rather than a store of its own, and it obeys the same rank,
/// fixed-row and moved-value rules the save obeyed (FR-069, FR-070).
/// </para>
/// </remarks>
public interface IPermissionAdministrationService
{
    /// <summary>
    /// Every declared permission with the value in force for <paramref name="userId"/> and where that value came
    /// from. Composed here so the page, the gate and the who-holds-this view read one answer (FR-049, FR-065).
    /// </summary>
    Task<IReadOnlyList<PermissionValueRow>> GetForPersonAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a whole change set in one call, and drops the resolution cache when it lands, so the next gate
    /// reads the store rather than the answer it held before the save (FR-072).
    /// </summary>
    Task<PermissionChangeResult> ApplyAsync(
        long userId,
        IReadOnlyList<PermissionChange> changes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverses the whole of this person's most recent save, from the recorded history (FR-069, FR-071).
    /// </summary>
    /// <param name="userId">The person whose last save is being reversed.</param>
    /// <param name="restoreDespiteMovedValue">
    /// <see langword="false"/> sends back the value the save recorded, so a value that has moved since refuses the
    /// reversal and reports which key moved. <see langword="true"/> is the second half of FR-070: the reader has
    /// been shown what the value is now and has asked for the restore anyway, so the value now held is sent as the
    /// <c>from</c> and the recorded previous value still wins.
    /// </param>
    /// <param name="cancellationToken">Cancels the read and the write.</param>
    Task<PermissionChangeResult> ReverseLastSaveAsync(
        long userId,
        bool restoreDespiteMovedValue = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The roles whose baselines give <paramref name="permissionKey"/>, and only the people who differ from their
    /// own role's baseline, in either direction (FR-075, FR-076). Read-only: this view changes nothing, and every
    /// change still happens on the person's own page (FR-077).
    /// </summary>
    Task<PermissionHolders> GetHoldersAsync(string permissionKey, CancellationToken cancellationToken = default);
}
