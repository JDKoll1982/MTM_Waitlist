using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

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
/// The permissions page: one person at a time, their features as a list rather than a grid of roles against
/// features, and a save that is confirmed before it is written and can be undone afterwards (FR-063 to FR-074).
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing here edits a role's baseline.</b> The baselines are seeded rows, and changing what a role may do is a
/// data change rather than a screen. Every change on this page is a choice made for one person (FR-063).
/// </para>
/// <para>
/// <b>Navigation-aware, loading asynchronously.</b> The page becomes usable when it is reached, never on a
/// synchronous read of the interface thread; the entitlement is re-read on every arrival so a reader whose access
/// changed while they were away does not keep a screen they may no longer use (FR-114).
/// </para>
/// <para>
/// <b>A person who outranks the reader is still selectable to view</b>, with their rows shown unavailable and the
/// reason in words rather than hidden (FR-066).
/// </para>
/// </remarks>
public partial class PermissionsViewModel : ObservableRecipient, INavigationAware
{
    /// <summary>The person's page, as the page service routes it: a differing person opens their own page.</summary>
    internal const string PersonPageViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.EditUserViewModel";

    /// <summary>
    /// The reversal the reader asked for and that met a value which had moved, kept so the confirmation can send
    /// the value now in force as the `from` (FR-070).
    /// </summary>
    private long _pendingReversalUserId;

    private readonly IPermissionAdministrationService _permissionAdministrationService;
    private readonly IPermissionService _permissionService;
    private readonly IUserManagementService _userManagementService;
    private readonly IRoleCatalogService _roleCatalogService;
    private readonly INavigationService _navigationService;
    private readonly StartupState _startupState;

    public PermissionsViewModel(
        IPermissionAdministrationService permissionAdministrationService,
        IPermissionService permissionService,
        IUserManagementService userManagementService,
        IRoleCatalogService roleCatalogService,
        INavigationService navigationService,
        StartupState startupState)
    {
        _permissionAdministrationService = permissionAdministrationService ?? throw new ArgumentNullException(nameof(permissionAdministrationService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
        _roleCatalogService = roleCatalogService ?? throw new ArgumentNullException(nameof(roleCatalogService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _startupState = startupState ?? throw new ArgumentNullException(nameof(startupState));
    }

    /// <summary>The work the page's arrival started.</summary>
    public Task Initialization { get; private set; } = Task.CompletedTask;

    /// <summary>The column of people. One is chosen at a time.</summary>
    public ObservableCollection<UserSummary> People { get; } = new();

    /// <summary>The chosen person's features, one row per declared permission.</summary>
    public ObservableCollection<PermissionRow> Rows { get; } = new();

    /// <summary>Whether the reader may open this page at all (FR-064).</summary>
    [ObservableProperty]
    public partial bool IsEntitled
    {
        get; set;
    }

    /// <summary>
    /// Whether the reader may not open this page, which is what shows the screen unavailable with the reason in
    /// words rather than hiding it (FR-101's rule, applied here).
    /// </summary>
    public bool NotEntitled => !IsEntitled;

    [ObservableProperty]
    public partial UserSummary? SelectedPerson
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

    /// <summary>Whether the store could not be read. Only ever true together with a stated reason.</summary>
    [ObservableProperty]
    public partial bool IsStoreUnavailable
    {
        get; set;
    }

    /// <summary>What the page says about the last thing that happened, in the reader's words.</summary>
    [ObservableProperty]
    public partial string MessageText
    {
        get; set;
    } = string.Empty;

    /// <summary>Whether the reader is being asked to confirm the reversal of a value that has moved (FR-070).</summary>
    [ObservableProperty]
    public partial bool IsRestorePromptVisible
    {
        get; set;
    }

    /// <summary>What that question says, naming what the value is now.</summary>
    [ObservableProperty]
    public partial string RestorePromptText
    {
        get; set;
    } = string.Empty;

    /// <summary>Whether the chosen person outranks the reader, so their rows are shown and cannot be changed.</summary>
    [ObservableProperty]
    public partial bool IsPersonReadOnly
    {
        get; set;
    }

    /// <summary>How many rows have been changed and not saved.</summary>
    public int PendingCount { get; private set; }

    /// <summary>Whether anything is pending.</summary>
    public bool HasPendingChanges => PendingCount > 0;

    /// <summary>
    /// Whether saving is available. Nothing changed is unavailable rather than a write that happens to have no
    /// effect (FR-068).
    /// </summary>
    public bool CanSave => IsEntitled && !IsPersonReadOnly && HasPendingChanges && !IsSaving && !IsRestorePromptVisible;

    /// <summary>Whether the reader has something to undo.</summary>
    public bool CanUndo => IsEntitled && !IsPersonReadOnly && SelectedPerson is not null && !IsSaving && !IsRestorePromptVisible;

    /// <summary>
    /// Whether saving is unavailable because nothing has changed, so the page can say why rather than leaving a
    /// greyed-out button unexplained (FR-068).
    /// </summary>
    public bool NothingChanged => IsEntitled && SelectedPerson is not null && !HasPendingChanges && !IsSaving;

    public string TitleText => "Permissions_Title.Title".GetLocalized();

    public string SubtitleText => "Permissions_Subtitle.Text".GetLocalized();

    public string PeopleHeadingText => "Permissions_People.Heading".GetLocalized();

    public string FeaturesHeadingText => "Permissions_Features.Heading".GetLocalized();

    public string SaveLabelText => "Permissions_Save.Label".GetLocalized();

    public string UndoLabelText => "Permissions_Undo.Label".GetLocalized();

    public string BackLabelText => "Permissions_Back.Label".GetLocalized();

    /// <summary>
    /// The answer that declines the restore and leaves the value where somebody else moved it.
    /// </summary>
    /// <remarks>
    /// The pinned resource keys for this feature carry no wording for declining the question FR-070 asks, so the
    /// existing discard wording is reused rather than a new key being invented here: the feature's resource map is
    /// owned by one task, and a story phase adding keys to it is exactly what that ownership prevents.
    /// </remarks>
    public string KeepValueLabelText => "EditUser_Discard.Label".GetLocalized();

    /// <summary>Why saving is unavailable right now, in the reader's words.</summary>
    public string NothingChangedText => "Permissions_Save.NothingChanged".GetLocalized();

    /// <summary>The reason a person who outranks the reader is shown rather than hidden (FR-066).</summary>
    public string ReadOnlyReasonText => "Permissions_Row.UnavailableOutranked".GetLocalized();

    /// <summary>What the page says when the store cannot be read. Never an empty list (FR-096's rule, applied here).</summary>
    public string UnavailableText => "UserManagement_State.Unavailable".GetLocalized();

    public string RetryText => "UserManagement_State.Retry".GetLocalized();

    /// <summary>What leaving with unsaved changes warns with, stating how many rows are pending (FR-073).</summary>
    public string UnsavedWarningText => string.Format(
        CultureInfo.CurrentCulture,
        "Permissions_Unsaved.Leaving".GetLocalized(),
        PendingCount);

    /// <summary>The set that would be written right now, built from the rows that have changed.</summary>
    public PermissionChangeSet CurrentChangeSet =>
        SelectedPerson is null ? PermissionChangeSet.Empty(0) : PermissionChangeSet.From(SelectedPerson.UserId, Rows);

    /// <summary>
    /// What the reader is asked to confirm before anything is written: what changes and for whom (FR-067).
    /// </summary>
    public IReadOnlyList<string> ConfirmationSentences =>
        SelectedPerson is null ? [] : CurrentChangeSet.ConfirmationSentences(SelectedPerson.DisplayName);

    /// <inheritdoc />
    public void OnNavigatedTo(object parameter) => Initialization = InitializeAsync();

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    /// <summary>The page's arrival: the reader's entitlement, then the column of people.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        IsStoreUnavailable = false;
        MessageText = string.Empty;

        try
        {
            IsEntitled = await _permissionService
                .HasPermissionAsync(PermissionKeys.AdminPermissions, cancellationToken)
                .ConfigureAwait(true);

            if (!IsEntitled)
            {
                MessageText = "EditUser_ReadOnly.NoPermission".GetLocalized();
                return;
            }

            await LoadPeopleAsync(cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
            AnnounceState();
        }
    }

    /// <summary>The reader's own retry, which reads the column of people again.</summary>
    [RelayCommand]
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        IsStoreUnavailable = false;

        try
        {
            await LoadPeopleAsync(cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
            AnnounceState();
        }
    }

    /// <summary>Loads the chosen person's features, marked with what is in force and where it came from.</summary>
    [RelayCommand]
    public async Task LoadPersonAsync(CancellationToken cancellationToken = default)
    {
        var person = SelectedPerson;
        if (person is null || !IsEntitled)
        {
            ReplaceRows([]);
            return;
        }

        IsBusy = true;
        IsStoreUnavailable = false;
        MessageText = string.Empty;
        IsRestorePromptVisible = false;

        try
        {
            await LoadPersonCoreAsync(person, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
            AnnounceState();
        }
    }

    /// <summary>
    /// Writes the whole change set in one call, after the page has been through the confirmation (FR-067, FR-071).
    /// </summary>
    [RelayCommand]
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var person = SelectedPerson;
        var changeSet = CurrentChangeSet;

        if (person is null || IsSaving || !CanSave || changeSet.IsEmpty)
        {
            return;
        }

        IsSaving = true;
        MessageText = string.Empty;
        AnnounceState();

        try
        {
            var result = await _permissionAdministrationService
                .ApplyAsync(person.UserId, changeSet.Entries, cancellationToken)
                .ConfigureAwait(true);

            MessageText = Resolve(result);

            if (result.IsSuccess)
            {
                await LoadPersonCoreAsync(person, cancellationToken).ConfigureAwait(true);
            }
        }
        finally
        {
            IsSaving = false;
            AnnounceState();
        }
    }

    /// <summary>
    /// Reverses the whole of the person's last save. Where a value has moved since, the page says what it is now
    /// and asks before restoring rather than overwriting it (FR-069, FR-070, FR-071).
    /// </summary>
    [RelayCommand]
    public async Task UndoAsync(CancellationToken cancellationToken = default)
    {
        var person = SelectedPerson;
        if (person is null || IsSaving || !CanUndo)
        {
            return;
        }

        IsSaving = true;
        MessageText = string.Empty;
        IsRestorePromptVisible = false;
        AnnounceState();

        try
        {
            var result = await _permissionAdministrationService
                .ReverseLastSaveAsync(person.UserId, restoreDespiteMovedValue: false, cancellationToken)
                .ConfigureAwait(true);

            await HandleReversalOutcomeAsync(person, result, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsSaving = false;
            AnnounceState();
        }
    }

    /// <summary>
    /// The reader's answer to the question the moved value raised: restore it anyway. The value now in force is
    /// sent as the `from`, so nothing is overwritten blindly even here (FR-070).
    /// </summary>
    [RelayCommand]
    public async Task ConfirmRestoreAsync(CancellationToken cancellationToken = default)
    {
        if (_pendingReversalUserId <= 0 || SelectedPerson is null)
        {
            IsRestorePromptVisible = false;
            return;
        }

        IsSaving = true;
        IsRestorePromptVisible = false;
        MessageText = string.Empty;
        AnnounceState();

        try
        {
            var person = SelectedPerson;
            var result = await _permissionAdministrationService
                .ReverseLastSaveAsync(person.UserId, restoreDespiteMovedValue: true, cancellationToken)
                .ConfigureAwait(true);

            await HandleReversalOutcomeAsync(person, result, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsSaving = false;
            AnnounceState();
        }
    }

    /// <summary>Declines the restore, which leaves the value where somebody else moved it.</summary>
    [RelayCommand]
    public void DismissRestore()
    {
        _pendingReversalUserId = 0;
        IsRestorePromptVisible = false;
        AnnounceState();
    }

    /// <summary>Opens a differing person's own page, which is where a person is changed (FR-077).</summary>
    [RelayCommand]
    public void OpenPerson(UserSummary? person)
    {
        if (person is null)
        {
            return;
        }

        _navigationService.NavigateTo(PersonPageViewModelName, person.UserId);
    }

    /// <summary>Leaves the page, having warned first if anything is pending (FR-073).</summary>
    [RelayCommand]
    public void GoBack() => _navigationService.GoBack();

    partial void OnSelectedPersonChanged(UserSummary? value) => _ = LoadPersonAsync();

    private async Task LoadPeopleAsync(CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _userManagementService.SearchAsync(null, null, cancellationToken).ConfigureAwait(true);

            People.Clear();
            foreach (var row in rows)
            {
                People.Add(new UserSummary(row));
            }

            if (SelectedPerson is null && People.Count > 0)
            {
                // The first person is chosen so the page opens on something rather than on an empty column.
                SelectedPerson = People[0];
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "Permissions",
                ex,
                "The column of people could not be read, so the page shows its unavailable state rather than an empty column (FR-096).");

            People.Clear();
            IsStoreUnavailable = true;
            MessageText = UnavailableText;
        }
    }

    private async Task LoadPersonCoreAsync(UserSummary person, CancellationToken cancellationToken)
    {
        IReadOnlyList<PermissionValueRow> values;
        try
        {
            values = await _permissionAdministrationService
                .GetForPersonAsync(person.UserId, cancellationToken)
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("Permissions", ex, "The person's permissions could not be read; the page says so.");
            ReplaceRows([]);
            IsStoreUnavailable = true;
            MessageText = UnavailableText;
            return;
        }

        IsPersonReadOnly = await PersonOutranksReaderAsync(person, cancellationToken).ConfigureAwait(true);

        var rows = values
            .Select(value => new PermissionRow(value))
            .ToArray();

        foreach (var row in rows)
        {
            row.IsAvailable = !IsPersonReadOnly;
        }

        ReplaceRows(rows);
    }

    private async Task<bool> PersonOutranksReaderAsync(UserSummary person, CancellationToken cancellationToken)
    {
        var catalogue = await _roleCatalogService.GetRolesAsync(cancellationToken).ConfigureAwait(true);

        var readerRole = catalogue.FirstOrDefault(
            entry => string.Equals(entry.RoleCode, _startupState.CurrentRoleCode, StringComparison.OrdinalIgnoreCase));
        var personRole = catalogue.FirstOrDefault(
            entry => string.Equals(entry.RoleCode, person.RoleCode, StringComparison.OrdinalIgnoreCase));

        // A role the catalogue no longer holds is nobody's rung, so the rank rule cannot clear it: the honest
        // answer is that this person cannot be changed here.
        if (readerRole is null || personRole is null)
        {
            return catalogue.Count > 0;
        }

        return RoleAuthorization.IsAbove(personRole, readerRole);
    }

    private async Task HandleReversalOutcomeAsync(
        UserSummary person,
        PermissionChangeResult result,
        CancellationToken cancellationToken)
    {
        if (result.Kind == PermissionChangeOutcomeKind.ValueMoved)
        {
            // Show what the value is now and ask. Nothing has been written at this point.
            await LoadPersonCoreAsync(person, cancellationToken).ConfigureAwait(true);

            var row = Rows.FirstOrDefault(candidate => string.Equals(candidate.Key, result.MovedKey, StringComparison.Ordinal));

            _pendingReversalUserId = person.UserId;
            RestorePromptText = string.Format(
                CultureInfo.CurrentCulture,
                "Permissions_Undo.ValueMoved".GetLocalized(),
                row?.LabelText ?? result.MovedKey);
            IsRestorePromptVisible = true;
            MessageText = string.Empty;
            return;
        }

        MessageText = Resolve(result);

        if (result.IsSuccess)
        {
            await LoadPersonCoreAsync(person, cancellationToken).ConfigureAwait(true);
        }
    }

    private void ReplaceRows(IReadOnlyCollection<PermissionRow> rows)
    {
        foreach (var existing in Rows)
        {
            existing.PropertyChanged -= OnRowChanged;
        }

        Rows.Clear();

        foreach (var row in rows)
        {
            row.PropertyChanged += OnRowChanged;
            Rows.Add(row);
        }

        RecomputePending();
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(PermissionRow.IsPending))
        {
            RecomputePending();
        }
    }

    private void RecomputePending()
    {
        PendingCount = Rows.Count(row => row.IsPending);
        AnnounceState();
    }

    private void AnnounceState()
    {
        OnPropertyChanged(nameof(NotEntitled));
        OnPropertyChanged(nameof(NothingChanged));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(HasPendingChanges));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(UnsavedWarningText));
        OnPropertyChanged(nameof(ConfirmationSentences));
        OnPropertyChanged(nameof(CurrentChangeSet));
        OnPropertyChanged(nameof(ReadOnlyReasonText));
    }

    private static string Resolve(PermissionChangeResult result)
    {
        var localized = result.MessageKey.GetLocalized();

        return string.Equals(localized, result.MessageKey, StringComparison.Ordinal)
            ? result.Message
            : localized;
    }
}
