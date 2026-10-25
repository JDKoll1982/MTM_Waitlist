using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Dialogs;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Services;

/// <summary>
/// App-side implementation of <see cref="IWaitlistRequestActionPrompt"/>. Lives in the composition root
/// because it needs the live main window to own the dialogs it opens.
/// </summary>
/// <remarks>
/// With no owner window (a headless host) every method answers "not confirmed" instead of throwing, so an
/// unanswered prompt can never be read as consent.
/// </remarks>
public sealed class WaitlistRequestActionPrompt : IWaitlistRequestActionPrompt
{
    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.Equals(localized, key, StringComparison.Ordinal) ? fallback : localized;
    }

    private static Window? TryGetOwnerWindow() => App.MainWindow;

    /// <inheritdoc />
    public async Task ShowWarningAsync(
        string title,
        string message,
        string closeText,
        CancellationToken cancellationToken = default)
    {
        if (TryGetOwnerWindow() is not { } owner)
        {
            return;
        }

        await MessageDialogHost.AskAsync(owner, new MessageDialogRequest
        {
            Kind = MessageKind.Acknowledge,
            Title = title,
            Message = message,
            CancelLabel = closeText,
        });
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(
        string title,
        string message,
        string acceptText,
        string cancelText,
        CancellationToken cancellationToken = default)
    {
        if (TryGetOwnerWindow() is not { } owner)
        {
            return false;
        }

        var outcome = await MessageDialogHost.AskAsync(owner, new MessageDialogRequest
        {
            Kind = MessageKind.Confirm,
            Title = title,
            Message = message,
            ActLabel = acceptText,
            CancelLabel = cancelText,
        });

        return outcome.IsAct;
    }

    /// <inheritdoc />
    public async Task<string?> RequestCancellationReasonAsync(
        string title,
        string message,
        string confirmText,
        string cancelText,
        CancellationToken cancellationToken = default)
    {
        if (TryGetOwnerWindow() is not { } owner)
        {
            return null;
        }

        var reasonBox = new TextBox
        {
            AcceptsReturn = false,
            PlaceholderText = LocalizeOrDefault("Waitlist_Action_Cancel.ReasonPlaceholder", "Reason (optional)"),
        };

        var content = new StackPanel { Spacing = 10 };
        content.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.WrapWholeWords });
        content.Children.Add(reasonBox);
        content.MinWidth = DialogSizing.MinWidth;
        content.MaxWidth = DialogSizing.MaxWidth;

        // This one keeps its own body because it collects a reason; only the window it opens in is shared.
        var result = await WindowedDialogHost.ShowAsync(
            owner,
            title,
            content,
            confirmText,
            cancelText,
            MessageDialogButton.Cancel);

        // Dismissed is null: "no answer" must stay distinguishable from "answered with an empty reason",
        // because only the latter is a cancellation.
        return result == ContentDialogResult.Primary
            ? reasonBox.Text?.Trim() ?? string.Empty
            : null;
    }
}
