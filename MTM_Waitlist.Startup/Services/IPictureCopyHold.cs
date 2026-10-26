using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// Holds the launch window on a picture refresh that did not work until the person has read it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The refresh stays best effort; the statement does not.</b> A picture source that cannot be read may never
/// stop the launch (FR-026), so the launch still reaches its surface. What is held is the window: it stays on the
/// failure — with the button that lets the person go on — instead of being replaced the moment the launch ends,
/// because the surface that replaces it says nothing about pictures, and a machine drawing stale or missing copies
/// would never find out.
/// </para>
/// <para>
/// <b>It is decided from the line, not from the step.</b> The line the runner wrote for the <c>picture-cache</c>
/// entry is the one record of how the refresh ended, and a line that reports failure is the answer. An overrun, an
/// unreachable share, an unwritable folder and a fault inside the mirror all arrive as that same line.
/// </para>
/// <para>
/// <b>It comes back on the next launch that fails.</b> Acknowledging answers the launch it was read on: a retry
/// that ends the same launch does not ask again, and the next launch that cannot refresh the copies does.
/// </para>
/// </remarks>
public interface IPictureCopyHold
{
    /// <summary>Raised when the hold begins or is acknowledged, so the surface can show or clear its statement.</summary>
    event EventHandler? Changed;

    /// <summary>Whether the window is holding the refresh's failure until the person acknowledges it.</summary>
    bool IsHolding { get; }

    /// <summary>
    /// Begins the hold when the launch's own lines say the picture refresh did not work.
    /// </summary>
    /// <param name="entries">Everything the launch has written, in the order it wrote it.</param>
    /// <returns>Whether the window is now holding, which is also what the caller waits on.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>null</c>.</exception>
    bool HoldIfTheRefreshFailed(IReadOnlyList<LaunchFeedEntry> entries);

    /// <summary>Ends the hold, which is the person saying they have read it.</summary>
    void Acknowledge();

    /// <summary>
    /// Answers a task that completes when the person acknowledges the hold, or immediately when there is none.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait, which is the launch being abandoned.</param>
    Task WaitAsync(CancellationToken cancellationToken = default);
}
