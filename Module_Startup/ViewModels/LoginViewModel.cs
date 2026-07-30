using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.ViewModels;

public partial class LoginViewModel : ObservableRecipient
{
    private readonly IStartupRegistrationService _startupRegistrationService;
    private readonly StartupState _startupState;

    [ObservableProperty]
    public partial string LoginHint
    {
        get;
        set;
    }

    [ObservableProperty]
    public partial bool ShowNewUserAction
    {
        get;
        set;
    }

    public LoginViewModel(IStartupRegistrationService startupRegistrationService, StartupState startupState)
    {
        ArgumentNullException.ThrowIfNull(startupRegistrationService);
        ArgumentNullException.ThrowIfNull(startupState);

        _startupRegistrationService = startupRegistrationService;
        _startupState = startupState;
        LoginHint = string.IsNullOrWhiteSpace(_startupState.LoginHint)
            ? "Sign in to continue."
            : _startupState.LoginHint;
        ShowNewUserAction = _startupState.RequireNewUserAction;
    }

    [RelayCommand]
    private async Task NewUserAsync()
    {
        await _startupRegistrationService.SubmitNewUserRequestAsync(_startupState);

        _startupState.RequireNewUserAction = false;
        _startupState.LoginHint = "New User request saved. A supervisor can finish registration from startup controls.";

        ShowNewUserAction = false;
        LoginHint = _startupState.LoginHint;
    }
}
