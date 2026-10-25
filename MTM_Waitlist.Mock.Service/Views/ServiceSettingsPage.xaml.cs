using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.ViewModels;
using MTM_Waitlist.Module_Shared.Dialogs;

namespace MTM_Waitlist.Mock.Service.Views;

/// <summary>
/// The service settings surface.
/// </summary>
/// <remarks>
/// <para>
/// Every value is validated before it is persisted, so an invalid port or destination is refused with a
/// message rather than accepted and failing later (FR-012). The view model is resolved from the
/// container before the XAML is loaded, which is the repository's page pattern.
/// </para>
/// <para>
/// The restore action is the one interactive flow here: it shows an explicit confirmation dialog and
/// passes the operator's answer to the view model, which does nothing at all when the answer is "no"
/// (FR-010).
/// </para>
/// <para>
/// The page is the shell's search host, so the title-bar search box narrows the group cards.
/// </para>
/// </remarks>
public sealed partial class ServiceSettingsPage : Page, IServiceSearchHost
{
    /// <summary>Creates the page and resolves its view model.</summary>
    public ServiceSettingsPage()
    {
        ViewModel = App.GetService<ServiceSettingsViewModel>();

        InitializeComponent();

        ViewModel.Load();
        ViewModel.LoadRestoreArtifacts();

        _ = ViewModel.RefreshToolStateAsync();
    }

    /// <summary>The page's view model.</summary>
    public ServiceSettingsViewModel ViewModel { get; }

    /// <inheritdoc />
    public IServiceSearchTarget? SearchTarget => ViewModel;

    private async void OnRestoreClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRestoreArtifact is null)
        {
            ViewModel.RestoreSelectedCommand.Execute(false);
            return;
        }

        if (App.CurrentWindow is not { } owner)
        {
            ViewModel.RestoreSelectedCommand.Execute(false);
            return;
        }

        // A restore replaces the mirror, so the dismissing answer is the default.
        var outcome = await MessageDialogHost.AskAsync(owner, new MessageDialogRequest
        {
            Kind = MessageKind.DestructiveConfirm,
            Title = ViewModel.RestoreConfirmTitleText,
            Message = ViewModel.RestoreConfirmBodyText,
            ActLabel = ViewModel.RestoreConfirmPrimaryText,
            CancelLabel = ViewModel.RestoreConfirmCloseText,
        });

        ViewModel.RestoreSelectedCommand.Execute(outcome.IsAct);
    }
}
