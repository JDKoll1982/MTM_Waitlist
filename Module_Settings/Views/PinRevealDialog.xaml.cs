using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Shared.Dialogs;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The one-time credential window (FR-028, FR-030, FR-032, FR-033, FR-034).
/// </summary>
/// <remarks>
/// <para>
/// Its content holds the only two buttons the reader may use: Print and Close. The window belongs to the main one and
/// is centred on it, and it deliberately carries no title bar, so the only close it offers is its own Close button
/// rather than a system X (014-adopt-lab-design-system, FR-001, FR-002).
/// </para>
/// <para>
/// Closing drops the credential: the view model releases it, and after that it is held nowhere in the application.
/// </para>
/// </remarks>
public sealed partial class PinRevealDialog : UserControl
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

        MinWidth = DialogSizing.MinWidth;
        Width = DialogSizing.PreferredWidth;
        MaxHeight = DialogSizing.MaxBodyHeight;
    }

    /// <summary>
    /// Opens the window on a freshly issued credential, in its own window owned by <paramref name="owner"/>.
    /// </summary>
    /// <param name="owner">The application window the credential window is centred on and belongs to.</param>
    /// <param name="credential">The credential to reveal. It is released when the window closes.</param>
    public async Task ShowWindowedAsync(Window owner, IssuedCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        ViewModel.Show(credential);

        var host = WindowedDialogHost.Create(
            owner,
            ViewModel.TitleText,
            this,
            actLabel: null,
            cancelLabel: ViewModel.CloseLabelText,
            MessageDialogButton.Cancel);

        // No title bar: the only way out is the Close button this window offers, which is the deliberate close.
        host.HasTitleBar = false;

        try
        {
            await host.ShowAsync();
        }
        finally
        {
            ReleaseCredential();
        }
    }

    private void ReleaseCredential()
    {
        if (_credentialReleased)
        {
            return;
        }

        _credentialReleased = true;
        ViewModel.Closed();

        AppLog.Info("PinRevealDialog", "Closed; the credential has been released and is held nowhere.");
    }

    /// <summary>Printing leaves the window open, whatever it does: a failed or cancelled print costs nothing.</summary>
    private async void OnPrintClick(object sender, RoutedEventArgs e) => await ViewModel.PrintAsync(App.MainWindow);
}
