namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// The confirmation surface the waitlist view models need without owning a XAML root. Lives behind an
/// interface for the same reason the Setup module's dialog service does: the app supplies the real
/// <c>ContentDialog</c> from its composition root, and a headless host (the unit suite) supplies a no-op,
/// so the view models stay constructible and testable with no UI at all.
/// </summary>
/// <remarks>
/// Every method must be safe to call with no window: an implementation that has no XAML root answers
/// "not confirmed" rather than throwing, so an unanswered prompt can never be mistaken for consent.
/// </remarks>
public interface IWaitlistRequestActionPrompt
{
    /// <summary>
    /// Shows a warning the user must acknowledge — used when an action lost a race, so the reason is
    /// impossible to miss rather than arriving as a quiet line on a list the user has already moved on from.
    /// </summary>
    /// <returns>A task that completes once the warning has been dismissed (or immediately when none can be shown).</returns>
    Task ShowWarningAsync(
        string title,
        string message,
        string closeText,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks the user to confirm a request-scoped action.
    /// </summary>
    /// <returns><see langword="true"/> only when the confirming button was chosen.</returns>
    Task<bool> ConfirmAsync(
        string title,
        string message,
        string acceptText,
        string cancelText,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks the user to confirm cancelling their own request and to give a reason.
    /// </summary>
    /// <returns>
    /// The reason when the confirming button was chosen — an empty string means "confirmed, no reason given"
    /// — or <see langword="null"/> when the prompt was dismissed, which means nothing must happen.
    /// </returns>
    Task<string?> RequestCancellationReasonAsync(
        string title,
        string message,
        string confirmText,
        string cancelText,
        CancellationToken cancellationToken = default);
}
