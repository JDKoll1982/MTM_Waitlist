using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// The seam through which the waitlist is told that the lugger arrangement changed, once per change, for the
/// session it opened with (FR-033, FR-036, FR-037).
/// </summary>
/// <remarks>
/// <para>
/// The session's watermark is taken once, when the list opens. Nothing at or below it is ever answered, so a change
/// made while the person was not using the application is never replayed to them as though it had just happened;
/// that is why there is no method here that asks for a change before the session.
/// </para>
/// <para>
/// Every answer is the full set of notices this session still holds, oldest change first, so a bar that has not
/// been dismissed stays where it is across ticks and a dismissal is the only thing that removes one.
/// </para>
/// </remarks>
public interface ILuggerChangeNoticeService
{
    /// <summary>
    /// Opens the session: takes the current highest change id as the watermark, in one read, and answers no notice.
    /// A change recorded before this call is never shown afterwards.
    /// </summary>
    Task<IReadOnlyList<LuggerChangeNotice>> OpenSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the changes above the session watermark and answers every notice this session still holds, in id
    /// order. A store that cannot be reached answers the notices already held rather than inventing any.
    /// </summary>
    Task<IReadOnlyList<LuggerChangeNotice>> PollAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that the person closed one notice. Only a close the notice's own close button raised is a dismissal
    /// (FR-036); any other reason leaves the notice in place. Answers the notices still held.
    /// </summary>
    /// <param name="changeId">The change row's id, which is the notice's id.</param>
    /// <param name="raisedByCloseButton">Whether the close button raised the close.</param>
    /// <param name="cancellationToken">Cancels the answer.</param>
    Task<IReadOnlyList<LuggerChangeNotice>> DismissAsync(
        long changeId,
        bool raisedByCloseButton,
        CancellationToken cancellationToken = default);
}
