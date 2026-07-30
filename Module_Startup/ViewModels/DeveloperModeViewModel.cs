using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_DevTools.ViewModels;

namespace MTM_Waitlist.Module_Startup.ViewModels;

public partial class DeveloperModeViewModel : ObservableRecipient
{
    private readonly INavigationService _navigationService;
    private readonly IStartupRecoveryService _startupRecoveryService;

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Ready for developer diagnostics.";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public DeveloperModeViewModel(
        IStartupRecoveryService startupRecoveryService,
        INavigationService navigationService)
    {
        ArgumentNullException.ThrowIfNull(startupRecoveryService);
        ArgumentNullException.ThrowIfNull(navigationService);

        _startupRecoveryService = startupRecoveryService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private void OpenRequestTypeBuilder()
    {
        var didNavigate = _navigationService.NavigateTo(typeof(RequestTypeBuilderViewModel).FullName!);
        StatusText = didNavigate
            ? "Opened Request Type Builder."
            : "Request Type Builder could not be opened.";
    }

    [RelayCommand]
    private async Task CorruptStartupDataAndRestartAsync()
    {
        IsBusy = true;
        StatusText = "Corrupting startup data and restarting...";

        try
        {
            await _startupRecoveryService.CorruptAndRestartAsync();
        }
        catch (Exception)
        {
            IsBusy = false;
            StatusText = "The recovery test could not be started.";
        }
    }
}
