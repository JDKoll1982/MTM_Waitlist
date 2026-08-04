using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Startup.ViewModels;

public partial class LoginViewModel : ObservableRecipient
{
    private const string LocalSessionTokenKey = "Startup.Session.Token";
    private const string LocalSessionExpiryKey = "Startup.Session.ExpiresUtc";
    private const string RememberPasswordKey = "Login.RememberPassword";
    private const string RememberedUsernameKey = "Login.RememberedUsername";
    private const string RememberedPasswordKey = "Login.RememberedPassword";

    private readonly IStartupSessionRepository _startupSessionRepository;
    private readonly IStartupRegistrationService _startupRegistrationService;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IStartupShellStateService _startupShellStateService;
    private readonly INavigationService _navigationService;
    private readonly StartupState _startupState;
    private long _pendingUserIdForPasswordChange;
    private string _pendingRole = string.Empty;

    [ObservableProperty]
    public partial string Username
    {
        get;
        set;
    }

    [ObservableProperty]
    public partial string Password
    {
        get;
        set;
    }

    [ObservableProperty]
    public partial string NewPassword
    {
        get;
        set;
    }

    [ObservableProperty]
    public partial string ConfirmPassword
    {
        get;
        set;
    }

    [ObservableProperty]
    public partial bool RememberPassword
    {
        get;
        set;
    }

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

    [ObservableProperty]
    public partial bool ShowPasswordChangePrompt
    {
        get;
        set;
    }

    public LoginViewModel(
        IStartupSessionRepository startupSessionRepository,
        IStartupRegistrationService startupRegistrationService,
        ILocalSettingsService localSettingsService,
        IStartupShellStateService startupShellStateService,
        INavigationService navigationService,
        StartupState startupState)
    {
        ArgumentNullException.ThrowIfNull(startupSessionRepository);
        ArgumentNullException.ThrowIfNull(startupRegistrationService);
        ArgumentNullException.ThrowIfNull(localSettingsService);
        ArgumentNullException.ThrowIfNull(startupShellStateService);
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(startupState);

        _startupSessionRepository = startupSessionRepository;
        _startupRegistrationService = startupRegistrationService;
        _localSettingsService = localSettingsService;
        _startupShellStateService = startupShellStateService;
        _navigationService = navigationService;
        _startupState = startupState;
        Username = _startupState.Username;
        Password = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        RememberPassword = false;
        LoginHint = string.IsNullOrWhiteSpace(_startupState.LoginHint)
            ? "Sign in to continue."
            : _startupState.LoginHint;
        ShowNewUserAction = _startupState.RequireNewUserAction;
        ShowPasswordChangePrompt = false;
    }

    public async Task InitializeAsync()
    {
        var rememberPassword = await _localSettingsService.ReadSettingAsync<bool>(RememberPasswordKey);
        RememberPassword = rememberPassword;

        if (!RememberPassword)
        {
            return;
        }

        var rememberedUsername = await _localSettingsService.ReadSettingAsync<string>(RememberedUsernameKey);
        var rememberedPassword = await _localSettingsService.ReadSettingAsync<string>(RememberedPasswordKey);

        if (!string.IsNullOrWhiteSpace(rememberedUsername))
        {
            Username = rememberedUsername;
        }

        if (!string.IsNullOrEmpty(rememberedPassword))
        {
            Password = rememberedPassword;
        }
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password))
        {
            LoginHint = "Enter your username and password to continue.";
            _startupState.LoginHint = LoginHint;
            return;
        }

        var credentialResult = await _startupSessionRepository.CheckCredentialsAsync(Username, Password);
        if (!credentialResult.IsAuthenticated)
        {
            LoginHint = "Sign-in failed. Check your credentials and try again.";
            _startupState.LoginHint = LoginHint;
            return;
        }

        if (credentialResult.RequiresPasswordChange)
        {
            _pendingUserIdForPasswordChange = credentialResult.UserId;
            _pendingRole = credentialResult.CurrentRole;
            ShowPasswordChangePrompt = true;
            LoginHint = "You signed in with temporary password 0000. Set a new password now.";
            _startupState.LoginHint = LoginHint;
            Password = string.Empty;
            return;
        }

        await CompleteLoginAsync(credentialResult.UserId, credentialResult.CurrentRole, Password);
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (_pendingUserIdForPasswordChange <= 0)
        {
            LoginHint = "Sign in again before changing your password.";
            _startupState.LoginHint = LoginHint;
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            LoginHint = "Enter and confirm your new password.";
            _startupState.LoginHint = LoginHint;
            return;
        }

        if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
        {
            LoginHint = "New password and confirmation do not match.";
            _startupState.LoginHint = LoginHint;
            return;
        }

        if (string.Equals(NewPassword, "0000", StringComparison.Ordinal))
        {
            LoginHint = "New password cannot be 0000.";
            _startupState.LoginHint = LoginHint;
            return;
        }

        var updated = await _startupSessionRepository.UpdatePasswordAsync(_pendingUserIdForPasswordChange, NewPassword);
        if (!updated)
        {
            LoginHint = "We could not save the new password. Try again.";
            _startupState.LoginHint = LoginHint;
            return;
        }

        ShowPasswordChangePrompt = false;
        LoginHint = "Password updated. Completing sign-in...";
        _startupState.LoginHint = LoginHint;
        await CompleteLoginAsync(_pendingUserIdForPasswordChange, _pendingRole, NewPassword);
    }

    [RelayCommand]
    private Task CancelAsync()
    {
        App.Current.Exit();
        return Task.CompletedTask;
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

    private async Task CompleteLoginAsync(long userId, string currentRole, string passwordToRemember)
    {
        _startupState.Username = Username.Trim().ToLowerInvariant();
        _startupState.CurrentRole = currentRole;
        _startupState.IsUserMatched = true;
        _startupState.IsSessionValid = true;
        _startupState.SessionTokenSource = StartupState.SessionTokenSourceLocal;

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var expiryUtc = DateTimeOffset.UtcNow.AddHours(8);

        await _localSettingsService.SaveSettingAsync(LocalSessionTokenKey, token);
        await _localSettingsService.SaveSettingAsync(LocalSessionExpiryKey, expiryUtc.ToString("O"));
        await _localSettingsService.SaveSettingAsync(RememberPasswordKey, RememberPassword);

        if (RememberPassword)
        {
            await _localSettingsService.SaveSettingAsync(RememberedUsernameKey, Username.Trim());
            await _localSettingsService.SaveSettingAsync(RememberedPasswordKey, passwordToRemember);
        }
        else
        {
            await _localSettingsService.SaveSettingAsync<string?>(RememberedUsernameKey, null);
            await _localSettingsService.SaveSettingAsync<string?>(RememberedPasswordKey, null);
        }

        await _startupShellStateService.EnterMainModeAsync();
        _navigationService.NavigateTo(typeof(WaitlistViewViewModel).FullName!, null, true);
        App.ShowMainWindowAndCloseLoginWindow();
    }
}
