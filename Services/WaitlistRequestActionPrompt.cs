using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Services;

/// <summary>
/// App-side implementation of <see cref="IWaitlistRequestActionPrompt"/>. Lives in the composition root
/// because it needs the live XAML root from the main window — the same arrangement the Setup dunnage
/// workflow uses for its dialog service.
/// </summary>
/// <remarks>
/// With no XAML root (a headless host) every method answers "not confirmed" instead of throwing, so an
/// unanswered prompt can never be read as consent.
/// </remarks>
public sealed class WaitlistRequestActionPrompt : IWaitlistRequestActionPrompt
{
    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.Equals(localized, key, StringComparison.Ordinal) ? fallback : localized;
    }

    private static XamlRoot? TryGetXamlRoot()
        => (App.MainWindow?.Content as FrameworkElement)?.XamlRoot;

    /// <inheritdoc />
    public async Task ShowWarningAsync(
        string title,
        string message,
        string closeText,
        CancellationToken cancellationToken = default)
    {
        var xamlRoot = TryGetXamlRoot();
        if (xamlRoot is null)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = closeText,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot,
        };

        await dialog.ShowAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(
        string title,
        string message,
        string acceptText,
        string cancelText,
        CancellationToken cancellationToken = default)
    {
        var xamlRoot = TryGetXamlRoot();
        if (xamlRoot is null)
        {
            return false;
        }

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = acceptText,
            CloseButtonText = cancelText,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot,
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    /// <inheritdoc />
    public async Task<string?> RequestCancellationReasonAsync(
        string title,
        string message,
        string confirmText,
        string cancelText,
        CancellationToken cancellationToken = default)
    {
        var xamlRoot = TryGetXamlRoot();
        if (xamlRoot is null)
        {
            return null;
        }

        var reasonBox = new TextBox
        {
            AcceptsReturn = false,
            PlaceholderText = LocalizeOrDefault("Waitlist_Action.Cancel.ReasonPlaceholder", "Reason (optional)"),
        };

        var content = new StackPanel { Spacing = 10 };
        content.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.WrapWholeWords });
        content.Children.Add(reasonBox);

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = confirmText,
            CloseButtonText = cancelText,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot,
        };

        // Dismissed is null: "no answer" must stay distinguishable from "answered with an empty reason",
        // because only the latter is a cancellation.
        return await dialog.ShowAsync() == ContentDialogResult.Primary
            ? reasonBox.Text?.Trim() ?? string.Empty
            : null;
    }
}
