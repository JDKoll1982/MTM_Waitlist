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
/// The permissions page: one card per area, that area's permissions across the top and the people down the side,
/// with a save confirmed before it is written and undoable afterwards (FR-063 to FR-074).
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing here edits a role's baseline.</b> The baselines are seeded rows, and changing what a role may do is a
/// data change rather than a screen. Every change on this page is a choice made for one person (FR-063), which is
/// why a cell the person has no row of their own for shows as their role's baseline rather than as theirs.
/// </para>
/// <para>
/// <b>The areas are the declaration's, not the page's.</b> <c>PermissionRegistry.Area</c> names exactly five, and
/// every declared permission appears in its area's card, so the page can neither lose a permission nor invent one
/// (FR-047).
/// </para>
/// <para>
/// <b>A save is written for one person.</b> The change-set procedure takes one person and a whole set of changes,
/// so one save is one atomic write and changing two people is two saves. A person who outranks the reader is shown
/// with the reason and cannot be saved at all (FR-066, FR-071).
/// </para>
/// <para>
/// <b>Navigation-aware, loading asynchronously.</b> The page becomes usable when it is reached, never on a
/// synchronous read of the interface thread; the entitlement is re-read on every arrival so a reader whose access
/// changed while they were away does not keep a screen they may no longer use (FR-114).
/// </para>
/// </remarks>
public partial class PermissionsViewModel : ObservableRecipient, INavigationAware
{
    /// <summary>The person's page, as the page service routes it: a person's name opens their own page (FR-077).</summary>
    internal const string PersonPageViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.EditUserViewModel";

    private readonly IPermissionAdministrationService _permissionAdministrationService;
    private readonly IPermissionService _permissionService;
    private readonly IUserManagementService _userManagementService;
    private readonly IRoleCatalogService _roleCatalogService;
    private readonly INavigationService _navigationService;
    private readonly StartupState _startupState;

    /// <summary>The person whose last save can be undone, or zero when there is nothing to undo.</summary>
    private long _undoUserId;

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

    /// <summary>One card per area the declaration names, in the declaration's order.</summary>
    public ObservableCollection<PermissionAreaCard> Cards { get; } = new();

    /// <summary>One row per person, which is what carries what is pending and what a save writes.</summary>
    public ObservableCollection<PermissionMatrixRow> Rows { get; } = new();

    /// <summary>The people whose cells have been turned over and not saved, which is what the page asks about.</summary>
    public ObservableCollection<PermissionMatrixRow> PendingRows { get; } = new();

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

    /// <summary>Whether there is nobody at all, which is stated rather than shown as an empty matrix (FR-096).</summary>
    [ObservableProperty]
    public partial bool IsRosterEmpty
    {
        get; set;
    }

    /// <summary>Whether any row belongs to somebody who outranks the reader, which is why the reason is shown.</summary>
    [ObservableProperty]
    public partial bool HasLockedRows
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

    /// <summary>What that question says, naming the permission whose value somebody else changed.</summary>
    [ObservableProperty]
    public partial string RestorePromptText
    {
        get; set;
    } = string.Empty;

    /// <summary>How many cells have been turned over across the whole page and not saved.</summary>
    public int PendingCount => Rows.Sum(row => row.PendingCount);

    /// <summary>Whether anything is pending anywhere.</summary>
    public bool HasPendingChanges => PendingCount > 0;

    /// <summary>
    /// Whether the last save can still be undone. A save is written for one person, so the undo is for that person
    /// (FR-069).
    /// </summary>
    public bool CanUndo => IsEntitled && _undoUserId > 0 && !IsSaving && !IsRestorePromptVisible;

    /// <summary>
    /// Whether saving is offered at all. With nothing changed it is unavailable rather than being a write that
    /// happens to have no effect (FR-068).
    /// </summary>
    public bool CanSave => IsEntitled && HasPendingChanges && !IsSaving && !IsRestorePromptVisible;

    /// <summary>
    /// Whether there is nothing to save, so the page can say why rather than leaving a greyed-out button
    /// unexplained (FR-068).
    /// </summary>
    public bool NothingChanged => IsEntitled && Rows.Count > 0 && !HasPendingChanges && !IsSaving;

    public string TitleText => "Permissions_Title.Title".GetLocalized();

    public string SubtitleText => "Permissions_Subtitle.Text".GetLocalized();

    public string PendingHeadingText => "Permissions_Pending.Heading".GetLocalized();

    public string SaveLabelText => "Permissions_Save.Label".GetLocalized();

    /// <summary>The heading over the confirmation, which asks the reader to check rather than repeating the button.</summary>
    public string ConfirmTitleText => "Permissions_Save.ConfirmTitle".GetLocalized();

    /// <summary>The answer that writes nothing.</summary>
    public string CancelLabelText => "Permissions_Save.Cancel".GetLocalized();

    public string UndoLabelText => "Permissions_Undo.Label".GetLocalized();

    /// <summary>The heading over the question a moved value raises (FR-070).</summary>
    public string UndoHeadingText => "Permissions_Undo.Heading".GetLocalized();

    /// <summary>The answer to that question that puts the reader's own value back.</summary>
    public string RestoreLabelText => "Permissions_Undo.Restore".GetLocalized();

    /// <summary>The answer that leaves the value where somebody else moved it.</summary>
    public string KeepValueLabelText => "Permissions_Undo.KeepMovedValue".GetLocalized();

    /// <summary>
    /// What the three marks in a cell mean, in one sentence. Without it the difference between a circle and a
    /// square is a difference nobody can read.
    /// </summary>
    public string LegendText => "Permissions_Legend.Text".GetLocalized();

    public string BackLabelText => "Permissions_Back.Label".GetLocalized();

    /// <summary>Why saving is unavailable right now, in the reader's words.</summary>
    public string NothingChangedText => "Permissions_Save.NothingChanged".GetLocalized();

    /// <summary>The reason a person who outranks the reader is shown rather than hidden (FR-066).</summary>
    public string ReadOnlyReasonText => "Permissions_Row.UnavailableOutranked".GetLocalized();

    /// <summary>What the page says when the store cannot be read. Never an empty matrix (FR-096).</summary>
    public string UnavailableText => "UserManagement_State.Unavailable".GetLocalized();

    /// <summary>What the page says when there is nobody to show, which is not the same as a store that failed.</summary>
    public string EmptyText => "UserManagement_State.Empty".GetLocalized();

    public string RetryText => "UserManagement_State.Retry".GetLocalized();

    /// <summary>
    /// What leaving with unsaved changes warns with, stating how many changes are pending (FR-073). One change is
    /// said one way and several another, because "1 changes" is the kind of sentence a reader stops trusting.
    /// </summary>
    public string UnsavedWarningText => PendingCount == 1
        ? "Permissions_Unsaved.LeavingOne".GetLocalized()
        : string.Format(
            CultureInfo.CurrentCulture,
            "Permissions_Unsaved.Leaving".GetLocalized(),
            PendingCount);

    /// <inheritdoc />
    public void OnNavigatedTo(object parameter) => Initialization = InitializeAsync();

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    /// <summary>The page's arrival: the reader's entitlement, then the matrix.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        IsStoreUnavailable = false;
        IsRosterEmpty = false;
        MessageText = string.Empty;

        try
        {
            IsEntitled = await _permissionService
                .HasPermissionAsync(PermissionKeys.AdminPermissions, cancellationToken)
                .ConfigureAwait(true);

            if (!IsEntitled)
            {
                MessageText = "EditUser_ReadOnly.NoPermission".GetLocalized();
                Clear();
                return;
            }

            await LoadMatrixAsync(cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
            AnnounceState();
        }
    }

    /// <summary>The reader's own retry, which reads the matrix again.</summary>
    [RelayCommand]
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        IsStoreUnavailable = false;
        IsRosterEmpty = false;

        try
        {
            await LoadMatrixAsync(cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
            AnnounceState();
        }
    }

    /// <summary>
    /// Writes the whole of one person's change set in one call, after the page has been through the confirmation
    /// (FR-067, FR-071).
    /// </summary>
    public async Task SaveAsync(PermissionMatrixRow? person, CancellationToken cancellationToken = default)
    {
        if (person is null || IsSaving || !IsEntitled || !person.CanSave)
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
                .ApplyAsync(person.UserId, person.CurrentChangeSet.Entries, cancellationToken)
                .ConfigureAwait(true);

            if (result.Kind == PermissionChangeOutcomeKind.ValueMoved)
            {
                // Somebody changed the same value while the reader was looking. The value shown is refreshed and
                // the sentence names the permission and the person, rather than only saying that something moved
                // (FR-070).
                await RebaseAsync(person, cancellationToken).ConfigureAwait(true);

                var moved = person.Cells.FirstOrDefault(cell => string.Equals(cell.Key, result.MovedKey, StringComparison.Ordinal));

                MessageText = string.Format(
                    CultureInfo.CurrentCulture,
                    "Permissions_Save.ValueMoved".GetLocalized(),
                    moved?.LabelText ?? PermissionRegistry.Label(result.MovedKey),
                    person.DisplayName);

                return;
            }

            if (!result.IsSuccess)
            {
                MessageText = Resolve(result);
                return;
            }

            _undoUserId = person.UserId;

            // What the store holds is what the row shows now rather than what was asked for: the save landed, so
            // the page reads it back and the reader sees the answer rather than their request.
            await RebaseAsync(person, cancellationToken).ConfigureAwait(true);

            MessageText = string.Format(
                CultureInfo.CurrentCulture,
                "Permissions_Save.SavedFor".GetLocalized(),
                person.DisplayName);
        }
        finally
        {
            IsSaving = false;
            AnnounceState();
        }
    }

    /// <summary>
    /// Reverses the whole of the last save, for the person it was written for. Where a value has moved since, the
    /// page says which one and asks before restoring rather than overwriting it (FR-069, FR-070, FR-071).
    /// </summary>
    [RelayCommand]
    public async Task UndoAsync(CancellationToken cancellationToken = default)
    {
        if (_undoUserId <= 0 || IsSaving || !CanUndo)
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
                .ReverseLastSaveAsync(_undoUserId, restoreDespiteMovedValue: false, cancellationToken)
                .ConfigureAwait(true);

            await HandleReversalOutcomeAsync(result, cancellationToken).ConfigureAwait(true);
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
        if (_undoUserId <= 0)
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
            var result = await _permissionAdministrationService
                .ReverseLastSaveAsync(_undoUserId, restoreDespiteMovedValue: true, cancellationToken)
                .ConfigureAwait(true);

            await HandleReversalOutcomeAsync(result, cancellationToken).ConfigureAwait(true);
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
        IsRestorePromptVisible = false;
        AnnounceState();
    }

    /// <summary>Opens a person's own page, which is where their account is changed (FR-077).</summary>
    [RelayCommand]
    public void OpenPerson(PermissionMatrixRow? person)
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

    /// <summary>
    /// Reads the people, their roles and every one of their permissions, and builds the cards the page is drawn
    /// from. One read per person, because the store answers for one person at a time.
    /// </summary>
    private async Task LoadMatrixAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<RoleCatalogEntry> catalogue;
        IReadOnlyList<UserRosterRow> roster;

        try
        {
            catalogue = await _roleCatalogService.GetRolesAsync(cancellationToken).ConfigureAwait(true);
            roster = await _userManagementService.SearchAsync(null, null, cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "Permissions",
                ex,
                "The people could not be read, so the page shows its unavailable state rather than an empty matrix (FR-096).");

            Clear();
            IsStoreUnavailable = true;
            MessageText = UnavailableText;
            return;
        }

        if (roster.Count == 0)
        {
            Clear();
            IsRosterEmpty = true;
            return;
        }

        var people = new List<UserSummary>(roster.Count);
        var readings = new List<IReadOnlyList<PermissionValueRow>>(roster.Count);

        try
        {
            foreach (var entry in Order(roster, catalogue))
            {
                var person = new UserSummary(entry);
                people.Add(person);
                readings.Add(await _permissionAdministrationService
                    .GetForPersonAsync(person.UserId, cancellationToken)
                    .ConfigureAwait(true));
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "Permissions",
                ex,
                "A person's permissions could not be read, so the page shows its unavailable state rather than a matrix missing a row.");

            Clear();
            IsStoreUnavailable = true;
            MessageText = UnavailableText;
            return;
        }

        var readerRole = catalogue.FirstOrDefault(
            entry => string.Equals(entry.RoleCode, _startupState.CurrentRoleCode, StringComparison.OrdinalIgnoreCase));

        Replace(people, readings, catalogue, readerRole);
    }

    /// <summary>
    /// The people in the order the page shows them: the highest rung first, then the name, and then the sign-in
    /// name so two accounts that share a name cannot swap places between one load and the next.
    /// </summary>
    private static IEnumerable<UserRosterRow> Order(
        IReadOnlyList<UserRosterRow> roster,
        IReadOnlyList<RoleCatalogEntry> catalogue)
    {
        return roster
            .OrderByDescending(person => Rank(catalogue, person.RoleCode))
            .ThenBy(person => person.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(person => person.UsernameNormalized, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A person's rung, or the floor when the catalogue does not hold their role. A role that is not in the
    /// catalogue is nobody's rung, so it sorts last rather than being given one.
    /// </summary>
    private static int Rank(IReadOnlyList<RoleCatalogEntry> catalogue, string roleCode) =>
        catalogue.FirstOrDefault(entry => string.Equals(entry.RoleCode, roleCode, StringComparison.OrdinalIgnoreCase))
            ?.RoleRank ?? int.MinValue;

    /// <summary>
    /// Whether the person's role stands above the reader's, which locks their whole row. A role the catalogue no
    /// longer holds is nobody's rung, so the rank rule cannot clear it: the honest answer is that this person
    /// cannot be changed here (FR-066).
    /// </summary>
    private static bool OutranksReader(
        IReadOnlyList<RoleCatalogEntry> catalogue,
        RoleCatalogEntry? readerRole,
        string personRoleCode)
    {
        var personRole = catalogue.FirstOrDefault(
            entry => string.Equals(entry.RoleCode, personRoleCode, StringComparison.OrdinalIgnoreCase));

        if (readerRole is null || personRole is null)
        {
            return catalogue.Count > 0;
        }

        return RoleAuthorization.IsAbove(personRole, readerRole);
    }

    private void Replace(
        IReadOnlyList<UserSummary> people,
        IReadOnlyList<IReadOnlyList<PermissionValueRow>> readings,
        IReadOnlyList<RoleCatalogEntry> catalogue,
        RoleCatalogEntry? readerRole)
    {
        Clear();

        for (var index = 0; index < people.Count; index++)
        {
            var person = people[index];
            var isLocked = OutranksReader(catalogue, readerRole, person.RoleCode);
            var values = readings[index];

            var cells = PermissionRegistry.All
                .Select(entry =>
                {
                    var stored = values.FirstOrDefault(value => string.Equals(value.Key, entry.Key, StringComparison.Ordinal));

                    return new PermissionCell(
                        entry.Key,
                        PermissionRegistry.Label(entry.Key),
                        PermissionMatrixRow.AccountAnnouncementOf(person.DisplayName, person.UsernameNormalized),
                        stored?.Value ?? entry.Fallback,
                        stored?.Provenance ?? PermissionProvenance.Fallback,
                        isFixed: string.Equals(entry.Key, PermissionKeys.AdminPermissions, StringComparison.Ordinal))
                    {
                        IsAvailable = !isLocked,
                    };
                })
                .ToArray();

            var row = new PermissionMatrixRow(person.UserId, person.DisplayName, person.UsernameNormalized, person.RoleText, isLocked, cells);
            row.PropertyChanged += OnRowChanged;

            Rows.Add(row);
            HasLockedRows |= isLocked;
        }

        foreach (var area in Enum.GetValues<PermissionRegistry.Area>())
        {
            var entries = PermissionRegistry.All
                .Where(entry => entry.BelongsTo == area)
                .ToArray();

            if (entries.Length == 0)
            {
                continue;
            }

            var columns = entries
                .Select(entry => new PermissionColumn(entry.Key, PermissionRegistry.Label(entry.Key), PermissionRegistry.Gates(entry.Key)))
                .ToArray();

            var cardRows = Rows
                .Select(row => new PermissionCardRow(
                    row,
                    entries.Select(entry => row.Cells.First(cell => string.Equals(cell.Key, entry.Key, StringComparison.Ordinal))).ToArray()))
                .ToArray();

            Cards.Add(new PermissionAreaCard(AreaHeading(area), columns, cardRows));
        }

        RebuildPending();
    }

    private static string AreaHeading(PermissionRegistry.Area area) =>
        $"Permissions_Area_{area}.Heading".GetLocalized();

    /// <summary>
    /// Reads back what the store now holds for one person and re-bases their row on it. A read that fails after a
    /// save that landed leaves the row as it is, because the save happened and saying otherwise would be a lie.
    /// </summary>
    private async Task RebaseAsync(PermissionMatrixRow person, CancellationToken cancellationToken)
    {
        try
        {
            var stored = await _permissionAdministrationService
                .GetForPersonAsync(person.UserId, cancellationToken)
                .ConfigureAwait(true);

            person.Rebase(stored);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "Permissions",
                ex,
                "The person's permissions could not be read back after the save; the save landed and the screen still shows what was asked for.");

            person.Rebase([]);
        }

        RebuildPending();
    }

    private async Task HandleReversalOutcomeAsync(PermissionChangeResult result, CancellationToken cancellationToken)
    {
        var person = Rows.FirstOrDefault(row => row.UserId == _undoUserId);

        if (result.Kind == PermissionChangeOutcomeKind.ValueMoved)
        {
            // Show what the value is now and ask. Nothing has been written at this point.
            if (person is not null)
            {
                await RebaseAsync(person, cancellationToken).ConfigureAwait(true);
            }

            var moved = person?.Cells.FirstOrDefault(cell => string.Equals(cell.Key, result.MovedKey, StringComparison.Ordinal));

            RestorePromptText = string.Format(
                CultureInfo.CurrentCulture,
                "Permissions_Undo.ValueMoved".GetLocalized(),
                moved?.LabelText ?? PermissionRegistry.Label(result.MovedKey));

            IsRestorePromptVisible = true;
            MessageText = string.Empty;
            return;
        }

        if (result.IsSuccess)
        {
            // An undo is not a save, and saying "Saved." after one leaves the reader unable to tell whether the
            // change went in or came back out.
            MessageText = person is null
                ? Resolve(result)
                : string.Format(CultureInfo.CurrentCulture, "Permissions_Undo.Done".GetLocalized(), person.DisplayName);

            _undoUserId = 0;

            if (person is not null)
            {
                await RebaseAsync(person, cancellationToken).ConfigureAwait(true);
            }

            return;
        }

        MessageText = Resolve(result);
    }

    private void Clear()
    {
        foreach (var row in Rows)
        {
            row.PropertyChanged -= OnRowChanged;
        }

        Rows.Clear();
        Cards.Clear();
        PendingRows.Clear();
        HasLockedRows = false;
        _undoUserId = 0;
        IsRestorePromptVisible = false;
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(PermissionMatrixRow.PendingCount))
        {
            RebuildPending();
        }
    }

    private void RebuildPending()
    {
        PendingRows.Clear();

        foreach (var row in Rows.Where(candidate => candidate.HasPendingChanges))
        {
            PendingRows.Add(row);
        }

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
        OnPropertyChanged(nameof(ReadOnlyReasonText));
    }

    /// <summary>The sentence for a refused change, resolved through the resource map, falling back to the shipped one.</summary>
    private static string Resolve(PermissionChangeResult result)
    {
        var localized = result.MessageKey.GetLocalized();

        return string.Equals(localized, result.MessageKey, StringComparison.Ordinal)
            ? result.Message
            : localized;
    }
}
