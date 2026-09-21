using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The one-time credential window (FR-028, FR-030, FR-032, FR-033, FR-034).
/// </summary>
/// <remarks>
/// <para>
/// Its content holds the only two buttons the reader may use: Print and Close. It deliberately carries no
/// <c>ContentDialog</c> close button and no primary button, because a close initiated by the framework — Escape,
/// the system back button, a gamepad B press — must take the refused path like any other dismissal. Accepting a
/// framework close button would make Escape a legitimate close, which is exactly what the deliberate-close rule
/// forbids.
/// </para>
/// <para>
/// Closing drops the credential: the view model releases it, and after that it is held nowhere in the
/// application.
/// </para>
/// </remarks>
public sealed partial class PinRevealDialog : ContentDialog
{
    /// <summary>Whether the credential has already been released, so release happens once.</summary>
    private bool _credentialReleased;

    public PinRevealDialogViewModel ViewModel
    {
        get;
    }

    public PinRevealDialog(PinRevealDialogViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        Closing += OnClosing;
        Closed += OnClosed;
    }

    /// <summary>
    /// Opens the window on a freshly issued credential. The root a dialog is shown from must be set explicitly in
    /// WinUI 3, and it is set here rather than by the caller so a page cannot forget it.
    /// </summary>
    public void ShowFor(IssuedCredential credential, XamlRoot xamlRoot)
    {
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentNullException.ThrowIfNull(xamlRoot);

        ViewModel.Show(credential);
        XamlRoot = xamlRoot;
        Title = ViewModel.TitleText;
    }

    /// <summary>
    /// Cancels every close that did not come from this window's own button, and says so, so the reader is told how
    /// to close it rather than being left with a window that appears not to respond.
    /// </summary>
    private void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        if (ViewModel.IsCloseRequested)
        {
            return;
        }

        args.Cancel = true;
        ViewModel.RefuseClose();
    }

    private void OnClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        if (_credentialReleased)
        {
            return;
        }

        _credentialReleased = true;
        ViewModel.Closed();

        StartupDebugLog.Info("PinRevealDialog", "Closed; the credential has been released and is held nowhere.");
    }

    /// <summary>Printing leaves the window open, whatever it does: a failed or cancelled print costs nothing.</summary>
    private async void OnPrintClick(object sender, RoutedEventArgs e) => await ViewModel.PrintAsync(App.MainWindow);

    /// <summary>
    /// The window's own close button, which is the only accepted way to close it.
    /// </summary>
    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        ViewModel.RequestCloseCommand.Execute(null);
        Hide();
    }
}
