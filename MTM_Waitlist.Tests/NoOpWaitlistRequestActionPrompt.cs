using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests;

/// <summary>
/// Headless double for <see cref="IWaitlistRequestActionPrompt"/>. Mirrors
/// <c>NoOpSetupDialogService</c>: with no window to show, nothing is confirmed and nothing happens —
/// which is exactly what a real implementation must answer when it has no XAML root.
/// </summary>
/// <remarks>
/// The defaults are the safe answers, and both can be overridden per test so a confirmation path and a
/// dismissal path are each exercisable without a UI.
/// </remarks>
public sealed class NoOpWaitlistRequestActionPrompt : IWaitlistRequestActionPrompt
{
    /// <summary>What <see cref="ConfirmAsync"/> answers. Defaults to the safe "not confirmed".</summary>
    public bool ConfirmResult { get; set; }

    /// <summary>What <see cref="RequestCancellationReasonAsync"/> answers. Defaults to a dismissal.</summary>
    public string? CancellationReasonResult { get; set; }

    /// <summary>How many times each prompt was asked, so a test can prove no prompt means no call.</summary>
    public int ConfirmCallCount { get; private set; }

    /// <summary>How many times the cancellation prompt was asked.</summary>
    public int CancellationPromptCallCount { get; private set; }

    /// <summary>How many times a warning was shown, so a test can prove the loser was told.</summary>
    public int WarningCallCount { get; private set; }

    /// <summary>The title of the last warning shown.</summary>
    public string? LastWarningTitle { get; private set; }

    /// <summary>The body of the last warning shown.</summary>
    public string? LastWarningMessage { get; private set; }

    /// <inheritdoc />
    public Task ShowWarningAsync(
        string title,
        string message,
        string closeText,
        CancellationToken cancellationToken = default)
    {
        WarningCallCount++;
        LastWarningTitle = title;
        LastWarningMessage = message;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(
        string title,
        string message,
        string acceptText,
        string cancelText,
        CancellationToken cancellationToken = default)
    {
        ConfirmCallCount++;
        return Task.FromResult(ConfirmResult);
    }

    /// <inheritdoc />
    public Task<string?> RequestCancellationReasonAsync(
        string title,
        string message,
        string confirmText,
        string cancelText,
        CancellationToken cancellationToken = default)
    {
        CancellationPromptCallCount++;
        return Task.FromResult(CancellationReasonResult);
    }
}
