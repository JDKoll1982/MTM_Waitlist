using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// The write side of a part picture: set it, replace it, refuse it where it cannot be stored, and read back what
/// was changed.
/// </summary>
/// <remarks>
/// <para>
/// A part picture is keyed by the system and the part number together. Nothing on this interface accepts a part
/// number on its own, because a Visual part and a WIP part that share a number are two pictures.
/// </para>
/// <para>
/// The change record is not written here. It is written by the store's own triggers, inside the same statement as
/// the row it records, so a picture cannot be set without the change being recorded and a change cannot be
/// recorded without the picture being set. This side only reads it back.
/// </para>
/// </remarks>
public interface IPartPictureWriteService
{
    /// <summary>
    /// Validates a picture, stores it where the part's number puts it, and points the part at it.
    /// </summary>
    /// <param name="system">The part's system: Visual or WIP.</param>
    /// <param name="partNumber">The part number, as the application names it.</param>
    /// <param name="sourceFilePath">The picture the person chose.</param>
    /// <param name="userId">The person making the change, for the record.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A typed answer. A refusal — a number past the storage's limit, or a name another part already holds — is
    /// answered rather than thrown, because each one is something the person has to be told.
    /// </returns>
    Task<PartPictureSetResult> SetPictureAsync(
        PartPictureSystem system,
        string partNumber,
        string sourceFilePath,
        long? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The part's change record, newest first: who set or replaced its picture, when, and what it replaced.
    /// </summary>
    /// <param name="system">The part's system: Visual or WIP.</param>
    /// <param name="partNumber">The part number.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>One entry per set or replace; empty when the part has never had a picture.</returns>
    /// <remarks>
    /// A read that cannot reach the store answers nothing rather than throwing, which is the same rule the picture
    /// reader follows: the screen shows the record it could read and reports the store as unavailable itself.
    /// </remarks>
    Task<IReadOnlyList<PartPictureChangeEntry>> GetChangeRecordAsync(
        PartPictureSystem system,
        string partNumber,
        CancellationToken cancellationToken = default);
}
