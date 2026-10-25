using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Views;
using MTM_Waitlist.Module_Shared.Dialogs;

namespace MTM_Waitlist.Services;

/// <summary>
/// App-side implementation of <see cref="ISetupDialogService"/> for the Setup
/// dunnage workflow. Lives in the composition root because it needs the app-owned
/// <see cref="SetupDunnageImageSearchDialog"/> view and the live main window that owns
/// the dialogs it opens.
/// </summary>
public sealed class SetupDialogService : ISetupDialogService
{
    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.Equals(localized, key, StringComparison.Ordinal) ? fallback : localized;
    }

    private static Window? TryGetOwnerWindow() => App.MainWindow;

    /// <inheritdoc />
    public Task<SetupDunnagePart?> ShowDunnageImageSearchDialogAsync()
    {
        if (TryGetOwnerWindow() is not { } owner)
        {
            // No window to own the dialog (for example a headless test host); no dialog can be shown.
            return Task.FromResult<SetupDunnagePart?>(null);
        }

        var dialog = App.GetService<SetupDunnageImageSearchDialog>();
        return dialog.ShowWindowedAsync(owner);
    }

    /// <inheritdoc />
    public Task<SetupWorkOrderSearchResult?> ShowWorkOrderSearchDialogAsync()
    {
        if (TryGetOwnerWindow() is not { } owner)
        {
            // No window to own the dialog (for example a headless test host); no dialog can be shown.
            return Task.FromResult<SetupWorkOrderSearchResult?>(null);
        }

        var dialog = App.GetService<SetupWorkOrderSearchDialog>();
        return dialog.ShowWindowedAsync(owner);
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmNoDunnageAsync()
    {
        if (TryGetOwnerWindow() is not { } owner)
        {
            // No window to own the dialog (for example a headless test host); proceed.
            return true;
        }

        var outcome = await MessageDialogHost.AskAsync(owner, new MessageDialogRequest
        {
            Kind = MessageKind.Decide,
            Title = LocalizeOrDefault("Setup_NoDunnage.DialogTitle", "Continue without dunnage?"),
            Message = LocalizeOrDefault("Setup_NoDunnage.DialogMessage", "No dunnage was selected for this job. Do you want to continue without dunnage?"),
            ActLabel = LocalizeOrDefault("Setup_NoDunnage.Confirm", "Yes"),
            CancelLabel = LocalizeOrDefault("Setup_NoDunnage.Cancel", "No"),
        });

        return outcome.IsAct;
    }
}
