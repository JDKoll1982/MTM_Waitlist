using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Contracts.Services;

namespace MTM_Waitlist.ViewModels;

public partial class DeveloperModeViewModel : ObservableRecipient
{
    private readonly IStartupRecoveryService _startupRecoveryService;

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Ready for developer diagnostics.";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public DeveloperModeViewModel(IStartupRecoveryService startupRecoveryService)
    {
        ArgumentNullException.ThrowIfNull(startupRecoveryService);
        _startupRecoveryService = startupRecoveryService;
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
