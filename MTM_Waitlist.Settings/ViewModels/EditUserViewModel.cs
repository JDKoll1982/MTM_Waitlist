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
/// One person's page: the fields above, the actions below, and a read-only state rather than a second page
/// (FR-097 to FR-104).
/// </summary>
/// <remarks>
/// <para>
/// <b>Read-only is a state, not a second page.</b> A reader who cannot act on the person is told so at the top and
/// the fields and actions are shown unavailable with the reason in words rather than hidden, because an account
/// the reader cannot change is still an account they may need to read (FR-027, FR-101).
/// </para>
/// <para>
/// <b>Two things are unavailable on the reader's own account</b> — deactivating it and changing its sign-in name —
/// each with its reason, and the store refuses both as well, so a caller that never draws this screen is refused
/// too (FR-023, FR-024).
/// </para>
/// <para>
/// <b>Nothing acts on a half-edited person.</b> An action chosen while there are unsaved edits is refused with
/// the instruction to save or discard, rather than quietly discarding the reader's work or quietly applying half
/// of it (FR-100).
/// </para>
/// <para>
/// <b>The reset action is gated on its own permission</b>, read once and used both to decide whether the control
/// is offered and to decide whether the action is allowed, so a reader without the key is never offered it and a
/// caller that invokes it anyway is refused in the same words (FR-117).
/// </para>
/// </remarks>
public partial class EditUserViewModel : ObservableRecipient, INavigationAware
{
    private readonly IUserManagementService _userManagementService;
    private readonly IRoleCatalogService _roleCatalogService;
    private readonly IPermissionService _permissionService;
    private readonly INavigationService _navigationService;
    private readonly StartupState _startupState;

    public EditUserViewModel(
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

    /// <summary>The work the page's arrival started.</summary>
    public Task Initialization { get; private set; } = Task.CompletedTask;

    /// <summary>The person as the store holds them, which is the baseline an unsaved edit is measured against.</summary>
    public UserDetail? Person { get; private set; }

    /// <summary>The roles the reader may assign, highest rung first.</summary>
    public ObservableCollection<UserListFilterOption> RoleOptions { get; } = new();

    [ObservableProperty]
    public partial bool IsLoaded
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsBusy
    {
        get; set;
    }

    /// <summary>True from the moment a write starts until it answers, which guards the repeated press.</summary>
    [ObservableProperty]
    public partial bool IsSaving
    {
        get; set;
    }

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

    /// <summary>
    /// Whether the account may sign in. Named for the account rather than the person because the message-recipient
    /// base type already holds an <c>IsActive</c> of its own, and shadowing it would make which one a binding reads
    /// a matter of luck.
    /// </summary>
    [ObservableProperty]
    public partial bool IsAccountActive
    {
        get; set;
    }

    /// <summary>Whether the reader may act on this person at all: their permission, then the rank rule.</summary>
    [ObservableProperty]
    public partial bool IsEditable
    {
        get; set;
    }

    /// <summary>Whether the reset action is offered. Read from the declaration, never hard-coded.</summary>
    [ObservableProperty]
    public partial bool CanResetPassword
    {
        get; set;
    }

    /// <summary>What the screen says about the last thing that happened, in the reader's words.</summary>
    [ObservableProperty]
    public partial string MessageText
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// Whether the store held no account with the id the page was opened for. Stated rather than shown as a blank
    /// form, because a blank form invites a write against a person who is not there.
    /// </summary>
    [ObservableProperty]
    public partial bool IsMissing
    {
        get; set;
    }

    /// <summary>Whether the page is read-only, which is the state a reader who cannot act meets.</summary>
    public bool IsReadOnly => IsLoaded && !IsEditable;

    /// <summary>Whether this is the account the reader is signed in as.</summary>
    public bool IsSelfAccount => Person is not null && Person.UserId == _startupState.UserId;

    /// <summary>Deactivating one's own account is unavailable, with its reason (FR-023).</summary>
    public bool IsSelfDeactivateUnavailable => IsSelfAccount && IsAccountActive;

    /// <summary>Changing one's own sign-in name is unavailable, with its reason (FR-024).</summary>
    public bool IsSelfRenameUnavailable => IsSelfAccount;

    /// <summary>Whether the reader has edits the store has not been told about (FR-100).</summary>
    public bool HasUnsavedChanges =>
        IsEditable && Person is not null && !string.Equals(CurrentRequest(), UserEditRequest.From(Person));

    /// <summary>Whether the person is switched off, which is what offers the reactivation in its place.</summary>
    public bool IsSwitchedOff => Person is not null && !IsAccountActive;

    /// <summary>
    /// Whether deactivating is offered: the reader may act on the person, the person is switched on, and it is not
    /// the reader's own account (FR-023).
    /// </summary>
    public bool CanDeactivate => IsEditable && IsAccountActive && !IsSelfDeactivateUnavailable;

    /// <summary>
    /// Whether an action is unavailable because of unsaved edits. This is the reason the page states rather than
    /// acting on the half-edited person.
    /// </summary>
    public bool ActionNeedsSave => HasUnsavedChanges;

    public bool CanSave => IsEditable && !IsSaving;

    /// <summary>The credential a successful reset issued, or <c>null</c>. Cleared once the window has shown it.</summary>
    public IssuedCredential? IssuedCredential { get; private set; }

    /// <summary>Whether a credential is waiting to be handed over.</summary>
    public bool HasIssuedCredential => IssuedCredential is not null;

    public string TitleText => "EditUser_Title.Title".GetLocalized();

    public string FieldsHeadingText => "EditUser_Fields.Heading".GetLocalized();

    public string ActionsHeadingText => "EditUser_Actions.Heading".GetLocalized();

    public string SignInNameLabelText => "EditUser_SignInName.Label".GetLocalized();

    public string FirstNameLabelText => "EditUser_FirstName.Label".GetLocalized();

    public string LastNameLabelText => "EditUser_LastName.Label".GetLocalized();

    public string EmployeeNumberLabelText => "EditUser_EmployeeNumber.Label".GetLocalized();

    public string RoleLabelText => "EditUser_Role.Label".GetLocalized();

    public string ActiveLabelText => "EditUser_Active.Label".GetLocalized();

    public string SaveLabelText => "EditUser_Save.Label".GetLocalized();

    public string DiscardLabelText => "EditUser_Discard.Label".GetLocalized();

    public string ResetPasswordLabelText => "EditUser_ResetPassword.Label".GetLocalized();

    public string DeactivateLabelText => "EditUser_Deactivate.Label".GetLocalized();

    public string ReactivateLabelText => "EditUser_Reactivate.Label".GetLocalized();

    public string BackLabelText => "EditUser_Back.Label".GetLocalized();

    /// <summary>
    /// The sentence a deactivation confirms and repeats: the person cannot sign in again, and a session already
    /// open stays open until they close the application (FR-102). It sits on the page as well as in the
    /// confirmation, so it is findable after the confirmation is gone.
    /// </summary>
    public string DeactivateExplanationText => "EditUser_Deactivate.Explanation.Text".GetLocalized();

    /// <summary>The sentence a reactivation gives: access returns from the next sign-in.</summary>
    public string ReactivateExplanationText => "EditUser_Reactivate.Explanation.Text".GetLocalized();

    /// <summary>What an action chosen with unsaved edits says (FR-100).</summary>
    public string ActionNeedsSaveText => "EditUser_Unsaved.ActionNeedsSave".GetLocalized();

    /// <summary>The reason a reader who may not act is given, in words (FR-101).</summary>
    public string ReadOnlyReasonText { get; private set; } = string.Empty;

    /// <summary>The reason the reader's own deactivation is unavailable (FR-023).</summary>
    public string SelfDeactivateUnavailableText => "EditUser_SelfDeactivate.Unavailable".GetLocalized();

    /// <summary>The reason the reader's own sign-in name cannot be changed (FR-024).</summary>
    public string SelfRenameUnavailableText => "EditUser_SelfRename.Unavailable".GetLocalized();

    /// <summary>What the reset confirmation says before the credential is issued (FR-028).</summary>
    public string ResetConfirmationText => "EditUser_ResetConfirmation.Text".GetLocalized();

    /// <inheritdoc />
    public void OnNavigatedTo(object parameter) => Initialization = InitializeAsync(parameter);

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// The page's arrival. <paramref name="parameter"/> is the person's id, which is what a row on the list hands
    /// over.
    /// </summary>
    public async Task InitializeAsync(object? parameter, CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        IsMissing = false;
        MessageText = string.Empty;
        IssuedCredential = null;

        try
        {
            if (parameter is not long userId || userId <= 0)
            {
                IsMissing = true;
                IsLoaded = true;
                return;
            }

            Person = ToDetail(await _userManagementService.GetAsync(userId, cancellationToken).ConfigureAwait(true));
            if (Person is null)
            {
                IsMissing = true;
                IsLoaded = true;
                return;
            }

            await RefreshEntitlementAsync(cancellationToken).ConfigureAwait(true);
            await LoadRoleOptionsAsync(cancellationToken).ConfigureAwait(true);
            ApplyPersonToForm(Person);
            IsLoaded = true;
        }
        finally
        {
            IsBusy = false;
            AnnounceState();
        }
    }

    /// <summary>
    /// Reads what the reader may do here: the declaration for the two keys, then the one catalogue read for the
    /// rank rule. Nothing is acted on before this has answered (FR-101).
    /// </summary>
    public async Task RefreshEntitlementAsync(CancellationToken cancellationToken = default)
    {
        var answers = await _permissionService
            .HasPermissionsAsync([PermissionKeys.AdminUsers, PermissionKeys.AdminResetPassword], cancellationToken)
            .ConfigureAwait(true);

        var mayManage = answers.TryGetValue(PermissionKeys.AdminUsers, out var manage) && manage;
        CanResetPassword = answers.TryGetValue(PermissionKeys.AdminResetPassword, out var reset) && reset;

        if (!mayManage || Person is null)
        {
            IsEditable = false;
            ReadOnlyReasonText = "EditUser_ReadOnly.NoPermission".GetLocalized();
            AnnounceState();
            return;
        }

        var catalogue = await _roleCatalogService.GetRolesAsync(cancellationToken).ConfigureAwait(true);
        var readerRole = catalogue.FirstOrDefault(
            entry => string.Equals(entry.RoleCode, _startupState.CurrentRoleCode, StringComparison.OrdinalIgnoreCase));
        var personRole = catalogue.FirstOrDefault(
            entry => string.Equals(entry.RoleCode, Person.RoleCode, StringComparison.OrdinalIgnoreCase));

        // A role the catalogue no longer holds is nobody's rung, so the rank rule cannot clear it: the honest
        // answer is that this account cannot be acted on here.
        var outranked = readerRole is not null && personRole is not null && RoleAuthorization.IsAbove(personRole, readerRole);

        IsEditable = !outranked;
        ReadOnlyReasonText = outranked ? "EditUser_ReadOnly.Outranked".GetLocalized() : string.Empty;

        AnnounceState();
    }

    /// <summary>Writes the correction, one press to one write.</summary>
    [RelayCommand]
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (IsSaving || !IsEditable || Person is null)
        {
            return;
        }

        IsSaving = true;
        MessageText = string.Empty;
        AnnounceState();

        try
        {
            var request = CurrentRequest();
            var result = await _userManagementService
                .UpdateAsync(Person.UserId, request.ToAccountEdit(), cancellationToken)
                .ConfigureAwait(true);

            MessageText = UserManagementResults.MessageFor(result);

            if (result.IsSuccess)
            {
                // The store is the record now, so the form is re-based on what it holds and the unsaved-change
                // state clears rather than staying on against values that were written.
                Person = ToDetail(await _userManagementService.GetAsync(Person.UserId, cancellationToken).ConfigureAwait(true));
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("EditUser", ex, "The save did not reach the store; the reader's work is kept.");
            MessageText = UnavailableMessage();
        }
        finally
        {
            IsSaving = false;
            AnnounceState();
        }
    }

    /// <summary>Puts the form back to what the store holds, which is the other half of "save or discard".</summary>
    [RelayCommand]
    public void DiscardChanges()
    {
        if (Person is null)
        {
            return;
        }

        ApplyPersonToForm(Person);
        MessageText = string.Empty;
        AnnounceState();
    }

    /// <summary>Switches the person off, or reports why nothing happened (FR-023, FR-102).</summary>
    [RelayCommand]
    public Task DeactivateAsync(CancellationToken cancellationToken = default) =>
        ChangeActiveStateAsync(activate: false, cancellationToken);

    /// <summary>Switches the person back on, or reports why nothing happened (FR-102).</summary>
    [RelayCommand]
    public Task ReactivateAsync(CancellationToken cancellationToken = default) =>
        ChangeActiveStateAsync(activate: true, cancellationToken);

    /// <summary>
    /// Issues a fresh one-time credential, gated on its own permission, refused while edits are unsaved and with
    /// the credential returned once (FR-028, FR-030).
    /// </summary>
    [RelayCommand]
    public async Task ResetPasswordAsync(CancellationToken cancellationToken = default)
    {
        if (Person is null || !IsEditable)
        {
            MessageText = ReadOnlyReasonText;
            return;
        }

        if (!CanResetPassword)
        {
            // Refused at the action as well as absent from the control, and in the same words as the absence.
            MessageText = "EditUser_ReadOnly.NoPermission".GetLocalized();
            return;
        }

        if (HasUnsavedChanges)
        {
            MessageText = ActionNeedsSaveText;
            return;
        }

        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        MessageText = string.Empty;
        AnnounceState();

        try
        {
            var result = await _userManagementService
                .ResetPasswordAsync(Person.UserId, cancellationToken)
                .ConfigureAwait(true);

            MessageText = UserManagementResults.MessageFor(result);

            if (!result.IsSuccess)
            {
                return;
            }

            IssuedCredential = new IssuedCredential(
                result.TemporaryPin,
                Person.DisplayName,
                Person.UsernameNormalized,
                DateTimeOffset.UtcNow,
                _startupState.EmployeeName);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("EditUser", ex, "The reset did not reach the store; nothing was issued.");
            MessageText = UnavailableMessage();
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

    /// <summary>Returns to the list the page was opened from.</summary>
    [RelayCommand]
    public void GoBack() => _navigationService.GoBack();

    /// <summary>What the form currently holds.</summary>
    public UserEditRequest CurrentRequest() => new(
        Person?.UserId ?? 0,
        Username ?? string.Empty,
        FirstName ?? string.Empty,
        LastName ?? string.Empty,
        EmployeeNumber ?? string.Empty,
        SelectedRole?.RoleCode ?? string.Empty,
        IsAccountActive);

    /// <summary>The store's account as the page's own shape, or <c>null</c> when there is no such account.</summary>
    private static UserDetail? ToDetail(UserAccount? account) => account is null ? null : new UserDetail(account);

    private async Task ChangeActiveStateAsync(bool activate, CancellationToken cancellationToken)
    {
        if (Person is null || !IsEditable)
        {
            MessageText = ReadOnlyReasonText;
            return;
        }

        if (activate == Person.IsActive)
        {
            return;
        }

        if (!activate && IsSelfDeactivateUnavailable)
        {
            MessageText = SelfDeactivateUnavailableText;
            return;
        }

        if (HasUnsavedChanges)
        {
            MessageText = ActionNeedsSaveText;
            return;
        }

        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        MessageText = string.Empty;
        AnnounceState();

        try
        {
            var request = UserEditRequest.From(Person) with { IsActive = activate };
            var result = await _userManagementService
                .UpdateAsync(Person.UserId, request.ToAccountEdit(), cancellationToken)
                .ConfigureAwait(true);

            MessageText = UserManagementResults.MessageFor(result);

            if (result.IsSuccess)
            {
                Person = ToDetail(await _userManagementService.GetAsync(Person.UserId, cancellationToken).ConfigureAwait(true));
                if (Person is not null)
                {
                    ApplyPersonToForm(Person);
                }
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("EditUser", ex, "The change of state did not reach the store; the reader's work is kept.");
            MessageText = UnavailableMessage();
        }
        finally
        {
            IsSaving = false;
            AnnounceState();
        }
    }

    private void ApplyPersonToForm(UserDetail person)
    {
        Username = person.UsernameNormalized;
        FirstName = person.FirstName;
        LastName = person.LastName;
        EmployeeNumber = person.EmployeeIdentifier;
        IsAccountActive = person.IsActive;

        SelectedRole = RoleOptions.FirstOrDefault(
                option => string.Equals(option.RoleCode, person.RoleCode, StringComparison.Ordinal))
            ?? RoleOptions.FirstOrDefault();
    }

    private async Task LoadRoleOptionsAsync(CancellationToken cancellationToken)
    {
        var catalogue = await _roleCatalogService.GetRolesAsync(cancellationToken).ConfigureAwait(true);

        RoleOptions.Clear();

        var readerRole = catalogue.FirstOrDefault(
            entry => string.Equals(entry.RoleCode, _startupState.CurrentRoleCode, StringComparison.OrdinalIgnoreCase));

        if (readerRole is null)
        {
            return;
        }

        foreach (var entry in RoleAuthorization.AtOrBelow(catalogue, readerRole))
        {
            RoleOptions.Add(UserListFilterOption.Role(entry.RoleCode, entry.RoleName));
        }
    }

    private static string UnavailableMessage() =>
        UserManagementResults.MessageFor(
            UserManagementResult.Failed(
                UserManagementOutcomeKind.StoreUnavailable,
                UserManagementMessages.StoreUnavailableKey,
                UserManagementMessages.StoreUnavailable));

    private void AnnounceState()
    {
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(IsSelfAccount));
        OnPropertyChanged(nameof(IsSelfDeactivateUnavailable));
        OnPropertyChanged(nameof(IsSelfRenameUnavailable));
        OnPropertyChanged(nameof(IsSwitchedOff));
        OnPropertyChanged(nameof(CanDeactivate));
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(ActionNeedsSave));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(HasIssuedCredential));
        OnPropertyChanged(nameof(ReadOnlyReasonText));
    }
}
