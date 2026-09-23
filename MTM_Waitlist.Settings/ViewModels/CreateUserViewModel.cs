using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Models.UserManagement;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// The create form: five fields, one role, and a save that creates exactly one person (FR-099, FR-101, FR-104).
/// </summary>
/// <remarks>
/// <para>
/// <b>A form only.</b> There is no active control, because a person is created active and switching them off is
/// something the person's own page does afterwards (FR-007). There is no actions area either: the only thing this
/// screen does is create.
/// </para>
/// <para>
/// <b>The picker offers only roles at or below the reader's own rung.</b> The bound is stated in words rather than
/// shown as a list of withheld roles, because a role the reader may not assign is not a choice with a reason, it
/// is not a choice (FR-022).
/// </para>
/// <para>
/// <b>The identity rules are applied once, in the service.</b> This screen does not restate the length or the
/// four-digit rule, because two copies of a rule are two rules; what it does is keep every value the reader typed
/// when the write is refused, so nothing has to be retyped (FR-005, FR-104).
/// </para>
/// <para>
/// <b>One press creates one person.</b> The save is guarded against a repeated press, and nothing in the path
/// retries: a second press while a save is in flight is ignored rather than sent (FR-104).
/// </para>
/// </remarks>
public partial class CreateUserViewModel : ObservableRecipient, INavigationAware
{
    private readonly IUserManagementService _userManagementService;
    private readonly IRoleCatalogService _roleCatalogService;
    private readonly IPermissionService _permissionService;
    private readonly INavigationService _navigationService;
    private readonly StartupState _startupState;

    public CreateUserViewModel(
        IUserManagementService userManagementService,
        IRoleCatalogService roleCatalogService,
        IPermissionService permissionService,
        INavigationService navigationService,
        StartupState startupState)
    {
        _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
        _roleCatalogService = roleCatalogService ?? throw new ArgumentNullException(nameof(roleCatalogService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _startupState = startupState ?? throw new ArgumentNullException(nameof(startupState));
    }

    /// <summary>The work the page's arrival started, so a caller can await it rather than wait and hope.</summary>
    public Task Initialization { get; private set; } = Task.CompletedTask;

    /// <summary>The roles the reader may assign, highest rung first.</summary>
    public ObservableCollection<UserListFilterOption> RoleOptions { get; } = new();

    /// <summary>Whether the reader may create a person. Read from the declaration on every arrival.</summary>
    [ObservableProperty]
    public partial bool IsEntitled
    {
        get; set;
    }

    /// <summary>
    /// Whether the reader may not create a person, which is what shows the form unavailable with the reason in
    /// words rather than hiding the screen (FR-101).
    /// </summary>
    public bool NotEntitled => !IsEntitled;

    [ObservableProperty]
    public partial string Username
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string FirstName
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string LastName
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string EmployeeNumber
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial UserListFilterOption? SelectedRole
    {
        get; set;
    }

    /// <summary>Whether the screen is working, so the reader meets a busy state rather than a freeze (FR-114).</summary>
    [ObservableProperty]
    public partial bool IsBusy
    {
        get; set;
    }

    /// <summary>True from the moment the save starts until it answers, which is what guards the repeated press.</summary>
    [ObservableProperty]
    public partial bool IsSaving
    {
        get; set;
    }

    /// <summary>What the screen says about the last save, in the reader's words.</summary>
    [ObservableProperty]
    public partial string MessageText
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// The credential a successful create issued, or <c>null</c>. The page opens the reveal window from this and
    /// then clears it, which is the whole of the credential's life in readable form.
    /// </summary>
    public IssuedCredential? IssuedCredential { get; private set; }

    /// <summary>Whether a credential is waiting to be handed over.</summary>
    public bool HasIssuedCredential => IssuedCredential is not null;

    /// <summary>Whether the last save happened, so the page can decide what to do next.</summary>
    public bool LastSaveSucceeded { get; private set; }

    public bool CanSave => IsEntitled && !IsSaving;

    public string TitleText => "CreateUser_Title.Title".GetLocalized();

    public string SubtitleText => "CreateUser_Subtitle.Text".GetLocalized();

    public string SignInNameLabelText => "CreateUser_SignInName.Label".GetLocalized();

    public string FirstNameLabelText => "CreateUser_FirstName.Label".GetLocalized();

    public string LastNameLabelText => "CreateUser_LastName.Label".GetLocalized();

    public string EmployeeNumberLabelText => "CreateUser_EmployeeNumber.Label".GetLocalized();

    public string RoleLabelText => "CreateUser_Role.Label".GetLocalized();

    /// <summary>The sentence stating the bound on the picker, shown whether or not any role was withheld.</summary>
    public string RoleBoundText => "CreateUser_Role.OnlyAtOrBelowYourRung".GetLocalized();

    public string SaveLabelText => "CreateUser_Save.Label".GetLocalized();

    public string CancelLabelText => "CreateUser_Cancel.Label".GetLocalized();

    /// <inheritdoc />
    public void OnNavigatedTo(object parameter) => Initialization = InitializeAsync();

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    /// <summary>The page's arrival: check the reader's entitlement, then fill the picker from the one catalogue.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            IsEntitled = await _permissionService
                .HasPermissionAsync(PermissionKeys.AdminUsers, cancellationToken)
                .ConfigureAwait(true);

            if (!IsEntitled)
            {
                MessageText = "EditUser_ReadOnly.NoPermission".GetLocalized();
                return;
            }

            await LoadRoleOptionsAsync(cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
            AnnounceState();
        }
    }

    /// <summary>
    /// Creates the person, or reports why it could not. Every typed value stays where it is on every outcome, so a
    /// refusal costs the reader nothing (FR-104).
    /// </summary>
    [RelayCommand]
    public async Task CreateAsync(CancellationToken cancellationToken = default)
    {
        if (IsSaving || !IsEntitled)
        {
            // One press creates one person. A press that arrives while a create is in flight is the same press.
            return;
        }

        IsSaving = true;
        LastSaveSucceeded = false;
        IssuedCredential = null;
        MessageText = string.Empty;
        AnnounceState();

        try
        {
            var request = CurrentRequest();
            var result = await _userManagementService
                .CreateAsync(request.ToAccountEdit(), cancellationToken)
                .ConfigureAwait(true);

            MessageText = UserManagementResults.MessageFor(result);

            if (!result.IsSuccess)
            {
                return;
            }

            LastSaveSucceeded = true;
            IssuedCredential = new IssuedCredential(
                result.TemporaryPin,
                DeriveDisplayName(request.FirstName, request.LastName),
                request.Username.ToUpperInvariant(),
                DateTimeOffset.UtcNow,
                _startupState.EmployeeName);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "CreateUser",
                ex,
                "The create did not reach the store; the typed values are kept so the reader can try again (FR-104).");

            MessageText = UserManagementResults.MessageFor(
                UserManagementResult.Failed(
                    UserManagementOutcomeKind.StoreUnavailable,
                    UserManagementMessages.StoreUnavailableKey,
                    UserManagementMessages.StoreUnavailable));
        }
        finally
        {
            IsSaving = false;
            AnnounceState();
        }
    }

    /// <summary>Drops the credential once the window that revealed it has closed.</summary>
    public void DismissIssuedCredential()
    {
        IssuedCredential = null;
        AnnounceState();
    }

    /// <summary>Returns to the list the reader came from.</summary>
    [RelayCommand]
    public void Cancel() => _navigationService.GoBack();

    /// <summary>What the form currently holds.</summary>
    public UserEditRequest CurrentRequest() => new(
        0,
        Username ?? string.Empty,
        FirstName ?? string.Empty,
        LastName ?? string.Empty,
        EmployeeNumber ?? string.Empty,
        SelectedRole?.RoleCode ?? string.Empty,
        IsActive: true);

    /// <summary>
    /// Fills the picker with the roles at or below the reader's own rung, read from the one catalogue.
    /// </summary>
    private async Task LoadRoleOptionsAsync(CancellationToken cancellationToken)
    {
        var catalogue = await _roleCatalogService.GetRolesAsync(cancellationToken).ConfigureAwait(true);

        RoleOptions.Clear();

        var readerRole = catalogue.FirstOrDefault(
            entry => string.Equals(entry.RoleCode, _startupState.CurrentRoleCode, StringComparison.OrdinalIgnoreCase));

        if (readerRole is null)
        {
            // The reader's own rung is unknown, so what they may assign is unknown too. Offering everything would
            // be guessing upwards, which is the one direction the rank rule exists to stop.
            SelectedRole = null;
            return;
        }

        foreach (var entry in RoleAuthorization.AtOrBelow(catalogue, readerRole))
        {
            RoleOptions.Add(UserListFilterOption.Role(entry.RoleCode, entry.RoleName));
        }

        SelectedRole = RoleOptions.FirstOrDefault();
    }

    private static string DeriveDisplayName(string firstName, string lastName) =>
        $"{firstName} {lastName}".Trim();

    private void AnnounceState()
    {
        OnPropertyChanged(nameof(NotEntitled));
        OnPropertyChanged(nameof(HasIssuedCredential));
        OnPropertyChanged(nameof(CanSave));
    }
}
