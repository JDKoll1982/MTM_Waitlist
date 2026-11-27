using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Services;
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
    private readonly IServiceRuntime _runtime;

    /// <summary>Creates the page and resolves its view model.</summary>
    public ServiceSettingsPage()
    {
        // The runtime is the running application rather than a reference to it, so this page names no application
        // type while still resolving the one container and the one window the service owns.
        _runtime = Application.Current as IServiceRuntime
            ?? throw new InvalidOperationException("The service's runtime is not available, so this page cannot be built.");

        var services = _runtime.Services
            ?? throw new InvalidOperationException("The service container has not been built yet.");

        ViewModel = services.GetRequiredService<ServiceSettingsViewModel>();

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

        if (_runtime.ShellWindow is not { } owner)
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
