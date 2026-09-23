using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// The user list: everyone in the store, searchable across the four facts people know and filterable by role
/// (FR-085 to FR-096).
/// </summary>
/// <remarks>
/// <para>
/// <b>Navigation-aware, loading asynchronously.</b> The screen becomes usable when the page reaches it, not when
/// it is constructed: <see cref="OnNavigatedTo"/> starts <see cref="Initialization"/>, and every state below is
/// false until that work has returned, so the reader never meets a list drawn from a stale or half-read answer.
/// Nothing here blocks the interface thread, and a slow store shows a busy state rather than a freeze (FR-114).
/// </para>
/// <para>
/// <b>Five states, each stated in the reader's words.</b> Everyone with the switched-off people marked; a filter
/// matching nobody, which is never the same message as an empty roster; a saved filter that could not be read,
/// which falls back to everyone and says so; a saved filter naming a role that no longer exists, which also falls
/// back to everyone and says so; and a store that cannot be read, which shows an unavailable state with a manual
/// retry. The last one is never an empty list and never a sample row (FR-088, FR-096).
/// </para>
/// <para>
/// <b>The filter is remembered for the person, not for the screen.</b> It is stored as one small payload under
/// <see cref="UserListFilter.SettingKey"/> in the person's own scope, so returning to the list applies the filter
/// they left and names it with the number of people it is hiding (FR-089, FR-094).
/// </para>
/// <para>
/// <b>A row's single job is to open that person.</b> Reset, deactivate and reactivate live on that person's page
/// and nowhere else, so there is exactly one place where a person is changed (FR-087).
/// </para>
/// </remarks>
public partial class UserManagementViewModel : ObservableRecipient, INavigationAware
{
    /// <summary>
    /// The person's page, as the page service routes it. Phase 7 registers that page; until it does, a navigation
    /// request is answered "no page" rather than silently going nowhere.
    /// </summary>
    internal const string PersonPageViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.EditUserViewModel";

    /// <summary>
    /// The page size the table opens at: fifteen rows, which is the size the design that the table follows shows.
    /// </summary>
    private const int RowsPerPageOpening = 15;

    /// <summary>How many people the search and the role filter are hiding.</summary>
    private int _hiddenCount;

    /// <summary>
    /// What the saved filter had to say, if anything, kept apart from the load's own state so a successful load
    /// does not wipe the sentence the reader still needs: that their saved filter could not be read, or that it
    /// named a role which is gone. Only a filter change clears it.
    /// </summary>
    private string _filterStateMessage = string.Empty;

    /// <summary>
    /// True while the saved filter is being applied, so restoring it does not look like the reader changing it and
    /// start a second load of its own.
    /// </summary>
    private bool _isRestoringFilter;

    /// <summary>
    /// The filter that was in force for the previous load, so a load that was not caused by a filter change can
    /// tell whether the filter is what emptied the list.
    /// </summary>
    private bool _filterAppliedAtLastLoad;

    private readonly IUserManagementService _userManagementService;
    private readonly IRoleCatalogService _roleCatalogService;
    private readonly IPermissionService _permissionService;
    private readonly IConfigSettingsValueService _settingsValueService;
    private readonly INavigationService _navigationService;
    private readonly StartupState _startupState;

    public UserManagementViewModel(
        IUserManagementService userManagementService,
        IRoleCatalogService roleCatalogService,
        IPermissionService permissionService,
        IConfigSettingsValueService settingsValueService,
        INavigationService navigationService,
        StartupState startupState)
    {
        _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
        _roleCatalogService = roleCatalogService ?? throw new ArgumentNullException(nameof(roleCatalogService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _settingsValueService = settingsValueService ?? throw new ArgumentNullException(nameof(settingsValueService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _startupState = startupState ?? throw new ArgumentNullException(nameof(startupState));
    }

    /// <summary>The work the page's arrival started, so a caller can await it rather than wait and hope.</summary>
    public Task Initialization { get; private set; } = Task.CompletedTask;

    /// <summary>Everyone the current filter leaves, in the store's order.</summary>
    public ObservableCollection<UserSummary> People { get; } = new();

    /// <summary>
    /// The rows the table is showing: the current page of <see cref="People"/>, in the order the reader chose
    /// (T045). The page is a slice of the filtered roster rather than a second read, so paging can never show a
    /// person the filter excluded.
    /// </summary>
    public ObservableCollection<UserSummary> PageRows { get; } = new();

    /// <summary>The page sizes the footer offers, smallest first.</summary>
    public IReadOnlyList<int> RowsPerPageOptions { get; } = [10, 15, 25, 50];

    /// <summary>The roles the filter offers, from the one catalogue read.</summary>
    public ObservableCollection<UserListFilterOption> RoleOptions { get; } = new();

    /// <summary>
    /// Whether the reader may open the user list at all. Re-checked every time the page is reached, so a reader
    /// whose entitlement changed while they were away does not keep a screen they may no longer use (FR-081).
    /// </summary>
    [ObservableProperty]
    public partial bool IsEntitled
    {
        get; set;
    }

    [ObservableProperty]
    public partial string SearchText
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial UserListFilterOption? SelectedRole
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsBusy
    {
        get; set;
    }

    /// <summary>Whether the store could not be read. Only ever true together with a stated reason.</summary>
    [ObservableProperty]
    public partial bool IsStoreUnavailable
    {
        get; set;
    }

    /// <summary>What the page says about the current state, in the reader's words.</summary>
    [ObservableProperty]
    public partial string StateMessage
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// How many rows the table shows at once. Changing it returns to the first page, because the page the reader
    /// was on no longer contains the row they were reading.
    /// </summary>
    [ObservableProperty]
    public partial int RowsPerPage
    {
        get; set;
    } = RowsPerPageOpening;

    /// <summary>Which page of the filtered roster the table is showing, counting from one.</summary>
    [ObservableProperty]
    public partial int CurrentPage
    {
        get; set;
    } = 1;

    /// <summary>
    /// The column the roster is ordered by, and its direction. The table's headers set both; nothing else does.
    /// </summary>
    public RosterSortColumn SortColumn { get; private set; } = RosterSortColumn.Name;

    /// <summary>Whether the sort column runs to its end rather than its beginning.</summary>
    public bool SortDescending { get; private set; }

    /// <summary>How many pages the filtered roster fills, which is never fewer than one.</summary>
    public int PageCount => Math.Max(1, (int)Math.Ceiling(People.Count / (double)RowsPerPage));

    /// <summary>Whether there is a page before this one.</summary>
    public bool CanGoToPreviousPage => CurrentPage > 1;

    /// <summary>Whether there is a page after this one.</summary>
    public bool CanGoToNextPage => CurrentPage < PageCount;

    /// <summary>
    /// Which people this page is showing, out of how many are listed, said the way a table's footer says it.
    /// </summary>
    public string RangeText
    {
        get
        {
            if (People.Count == 0)
            {
                return string.Empty;
            }

            var first = ((CurrentPage - 1) * RowsPerPage) + 1;
            var last = Math.Min(People.Count, CurrentPage * RowsPerPage);

            return string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                "UserManagement_Pager.Range".GetLocalized(),
                first,
                last,
                People.Count);
        }
    }

    /// <summary>Which page this is, out of how many.</summary>
    public string PageText => string.Format(
        System.Globalization.CultureInfo.CurrentCulture,
        "UserManagement_Pager.Page".GetLocalized(),
        CurrentPage,
        PageCount);

    /// <summary>How many people are listed, said beside the title.</summary>
    public string RosterCountText => string.Format(
        System.Globalization.CultureInfo.CurrentCulture,
        "UserManagement_Title.Count".GetLocalized(),
        People.Count);

    /// <summary>
    /// Whether a filter is in force, which is what makes "nobody matches" different from "there is nobody"
    /// (FR-090).
    /// </summary>
    public bool IsFilterApplied =>
        !string.IsNullOrWhiteSpace(SearchText)
        || SelectedRole is { IsAllRoles: false };

    /// <summary>The number of people the filter is hiding, said only when a filter is applied.</summary>
    public int HiddenCount
    {
        get => _hiddenCount;
        private set
        {
            if (SetProperty(ref _hiddenCount, value))
            {
                OnPropertyChanged(nameof(HiddenCountText));
                OnPropertyChanged(nameof(IsFilterApplied));
            }
        }
    }

    public string HiddenCountText => string.Format(
        System.Globalization.CultureInfo.CurrentCulture,
        "UserManagement_Filter.HiddenCount.Text".GetLocalized(),
        HiddenCount);

    /// <summary>A roster with nobody in it and no filter, which is a different state from a filter matching nobody.</summary>
    public bool IsRosterEmpty => !IsBusy && !IsStoreUnavailable && People.Count == 0 && !_filterAppliedAtLastLoad;

    /// <summary>A filter that matches nobody.</summary>
    public bool IsNoMatch => !IsBusy && !IsStoreUnavailable && People.Count == 0 && _filterAppliedAtLastLoad;

    /// <summary>Whether the list has rows to show.</summary>
    public bool HasPeople => People.Count > 0;

    public string TitleText => "UserManagement_Title.Title".GetLocalized();

    public string SubtitleText => "UserManagement_Subtitle.Text".GetLocalized();

    public string SearchLabelText => "UserManagement_Search.Label".GetLocalized();

    public string SearchPlaceholderText => "UserManagement_Search.PlaceholderText".GetLocalized();

    public string RoleFilterLabelText => "UserManagement_RoleFilter.Label".GetLocalized();

    public string FilterActiveText => "UserManagement_Filter.Active".GetLocalized();

    public string ShowEveryoneText => "UserManagement_Filter.ShowEveryone".GetLocalized();

    public string EmptyText => "UserManagement_State.Empty".GetLocalized();

    public string NoMatchText => "UserManagement_State.NoMatch".GetLocalized();

    public string UnavailableText => "UserManagement_State.Unavailable".GetLocalized();

    public string RetryText => "UserManagement_State.Retry".GetLocalized();

    public string AddLabelText => "UserManagement_Add.Label".GetLocalized();

    public string BackLabelText => "UserManagement_Back.Label".GetLocalized();

    /// <summary>The five column headings, in the order the table shows the five facts (FR-086).</summary>
    public string ColumnNameHeaderText => "UserManagement_Column.Name".GetLocalized();

    public string ColumnSignInNameHeaderText => "UserManagement_Column.SignInName".GetLocalized();

    public string ColumnEmployeeNumberHeaderText => "UserManagement_Column.EmployeeNumber".GetLocalized();

    public string ColumnRoleHeaderText => "UserManagement_Column.Role".GetLocalized();

    public string ColumnStatusHeaderText => "UserManagement_Column.Status".GetLocalized();

    public string RowsPerPageLabelText => "UserManagement_Pager.RowsPerPage".GetLocalized();

    public string PreviousPageText => "UserManagement_Pager.Previous".GetLocalized();

    public string NextPageText => "UserManagement_Pager.Next".GetLocalized();

    /// <inheritdoc />
    public void OnNavigatedTo(object parameter) => Initialization = InitializeAsync();

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// The page's arrival: check the reader's entitlement, read the catalogue for the filter, restore the saved
    /// filter, then load.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await RefreshEntitlementAsync(cancellationToken).ConfigureAwait(true);

        if (!IsEntitled)
        {
            StateMessage = string.Empty;
            return;
        }

        await LoadRoleOptionsAsync(cancellationToken).ConfigureAwait(true);
        await RestoreSavedFilterAsync(cancellationToken).ConfigureAwait(true);
        await LoadAsync(cancellationToken).ConfigureAwait(true);
    }

    /// <summary>
    /// Reads the reader's entitlement from the declaration rather than from the answer the page was opened with.
    /// </summary>
    public async Task RefreshEntitlementAsync(CancellationToken cancellationToken = default)
    {
        IsEntitled = await _permissionService
            .HasPermissionAsync(PermissionKeys.AdminUsers, cancellationToken)
            .ConfigureAwait(true);
    }

    /// <summary>Loads the list for the filter in force. The reader's own retry goes through here.</summary>
    [RelayCommand]
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        IsStoreUnavailable = false;

        // A load is a new answer, so the reader starts at its beginning rather than on a page that a narrower
        // filter may no longer have.
        CurrentPage = 1;

        var filter = CurrentFilter();
        _filterAppliedAtLastLoad = filter.IsApplied;

        try
        {
            var rows = await _userManagementService
                .SearchAsync(filter.SearchText, filter.RoleCode, cancellationToken)
                .ConfigureAwait(true);

            var hidden = 0;
            if (filter.IsApplied)
            {
                // The count of hidden people is the unfiltered total less what the filter left. Asking the store
                // for both is what lets the page say how many it is hiding rather than only that it is hiding
                // some (FR-094).
                var everyone = await _userManagementService
                    .SearchAsync(null, null, cancellationToken)
                    .ConfigureAwait(true);

                hidden = Math.Max(0, everyone.Count - rows.Count);
            }

            ReplacePeople(rows);
            HiddenCount = hidden;
            StateMessage = _filterStateMessage;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "UserManagement",
                ex,
                "The user store could not be read, so the list shows its unavailable state rather than an empty list (FR-096).");

            People.Clear();
            HiddenCount = 0;
            IsStoreUnavailable = true;
            StateMessage = UnavailableText;
        }
        finally
        {
            IsBusy = false;
            ShowCurrentPage();
            AnnounceState();
        }
    }

    /// <summary>Moves to the page before this one. The control is only usable when there is one.</summary>
    [RelayCommand]
    public void PreviousPage()
    {
        if (!CanGoToPreviousPage)
        {
            return;
        }

        CurrentPage--;
        ShowCurrentPage();
    }

    /// <summary>Moves to the page after this one. The control is only usable when there is one.</summary>
    [RelayCommand]
    public void NextPage()
    {
        if (!CanGoToNextPage)
        {
            return;
        }

        CurrentPage++;
        ShowCurrentPage();
    }

    /// <summary>
    /// Orders the roster by one of the five facts and shows its first page. Clicking the column already ordered
    /// by reverses it, which is what a table's heading does. The argument is the column's name as the page spells
    /// it, and a name that matches no column is ignored rather than guessed at.
    /// </summary>
    [RelayCommand]
    public void SortBy(string column)
    {
        if (!Enum.TryParse<RosterSortColumn>(column, out var chosen))
        {
            return;
        }

        if (SortColumn == chosen)
        {
            SortDescending = !SortDescending;
        }
        else
        {
            SortColumn = chosen;
            SortDescending = false;
        }

        CurrentPage = 1;
        ShowCurrentPage();
    }

    /// <summary>Clears the filter and shows everyone, which is what the "show everyone" control does.</summary>
    [RelayCommand]
    public async Task ShowEveryoneAsync(CancellationToken cancellationToken = default)
    {
        _filterStateMessage = string.Empty;
        _isRestoringFilter = true;
        try
        {
            SearchText = string.Empty;
            SelectedRole = RoleOptions.FirstOrDefault(option => option.IsAllRoles);
        }
        finally
        {
            _isRestoringFilter = false;
        }

        await SaveFilterAsync(cancellationToken).ConfigureAwait(true);
        await LoadAsync(cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Opens the person a row names. This is the row's whole job.</summary>
    [RelayCommand]
    public void OpenPerson(UserSummary? person)
    {
        if (person is null)
        {
            return;
        }

        _navigationService.NavigateTo(PersonPageViewModelName, person.UserId);
    }

    /// <summary>Opens the create form.</summary>
    [RelayCommand]
    public void AddPerson() =>
        _navigationService.NavigateTo("MTM_Waitlist.Module_Settings.ViewModels.CreateUserViewModel");

    partial void OnSearchTextChanged(string value) => OnFilterHalfChanged();

    partial void OnSelectedRoleChanged(UserListFilterOption? value) => OnFilterHalfChanged();

    /// <summary>
    /// Starts the save-and-reload for a filter change, unless the change was the screen restoring the saved filter
    /// rather than the reader making one.
    /// </summary>
    private void OnFilterHalfChanged()
    {
        if (_isRestoringFilter)
        {
            return;
        }

        _filterStateMessage = string.Empty;
        _ = ApplyFilterChangeAsync();
    }

    /// <summary>
    /// Saves the filter and reloads when either half of it changes, including when it is cleared.
    /// </summary>
    private async Task ApplyFilterChangeAsync(CancellationToken cancellationToken = default)
    {
        await SaveFilterAsync(cancellationToken).ConfigureAwait(true);
        await LoadAsync(cancellationToken).ConfigureAwait(true);
    }

    /// <summary>
    /// Reads the roles the filter offers from the one catalogue read, with an "every role" choice first.
    /// </summary>
    private async Task LoadRoleOptionsAsync(CancellationToken cancellationToken)
    {
        var roles = await _roleCatalogService.GetRolesAsync(cancellationToken).ConfigureAwait(true);

        for (var index = RoleOptions.Count - 1; index >= 0; index--)
        {
            RoleOptions.RemoveAt(index);
        }

        RoleOptions.Add(UserListFilterOption.AllRoles("UserManagement_RoleFilter.All".GetLocalized()));

        foreach (var role in roles)
        {
            RoleOptions.Add(UserListFilterOption.Role(role.RoleCode, role.RoleName));
        }

        // Choosing the opening choice is the screen setting itself up, not the reader narrowing the list, so it
        // must not save a filter or start a load: doing either would overwrite the filter this visit is about to
        // restore with an empty one.
        _isRestoringFilter = true;
        try
        {
            SelectedRole = RoleOptions[0];
        }
        finally
        {
            _isRestoringFilter = false;
        }
    }

    /// <summary>
    /// Restores the filter the person left, and states what happened when it cannot be used.
    /// </summary>
    /// <remarks>
    /// Two different failures, two different sentences, and both fall back to everyone rather than applying half
    /// of somebody's filter: something was saved that cannot be read, or the filter names a role the catalogue no
    /// longer holds (FR-092, FR-093).
    /// </remarks>
    private async Task RestoreSavedFilterAsync(CancellationToken cancellationToken)
    {
        var stored = await _settingsValueService
            .GetSettingValueAsync(UserListFilter.SettingKey, UserScopeKey())
            .ConfigureAwait(true);

        var outcome = UserListFilter.TryRead(stored?.SettingValue, out var filter);

        switch (outcome)
        {
            case UserListFilter.FilterReadOutcome.None:
                return;

            case UserListFilter.FilterReadOutcome.Unreadable:
                _filterStateMessage = "UserManagement_State.FilterUnreadable".GetLocalized();
                StateMessage = _filterStateMessage;
                return;

            default:
                if (!string.IsNullOrWhiteSpace(filter.RoleCode)
                    && !RoleOptions.Any(option => string.Equals(option.RoleCode, filter.RoleCode, StringComparison.Ordinal)))
                {
                    _filterStateMessage = "UserManagement_State.RoleGone".GetLocalized();
                    StateMessage = _filterStateMessage;
                    return;
                }

                _isRestoringFilter = true;
                try
                {
                    SearchText = filter.SearchText;
                    SelectedRole = RoleOptions.FirstOrDefault(option => string.Equals(option.RoleCode, filter.RoleCode, StringComparison.Ordinal))
                        ?? RoleOptions[0];
                }
                finally
                {
                    _isRestoringFilter = false;
                }

                return;
        }
    }

    /// <summary>
    /// Writes the filter for this person. A preference is not a permission, so this writes no history row, and a
    /// failure to save it is recorded rather than shown: the reader asked to filter a list, not to store one.
    /// </summary>
    private async Task SaveFilterAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _settingsValueService.SetSettingValueAsync(
                new ConfigSettingValue
                {
                    SettingKey = UserListFilter.SettingKey,
                    ScopeType = "user",
                    ScopeKey = UserScopeKey(),
                    UserId = _startupState.UserId > 0 ? _startupState.UserId : null,
                    SettingValue = CurrentFilter().ToStoredValue(),
                    ValueType = "text",
                },
                _startupState.UserId > 0 ? _startupState.UserId : null).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "UserManagement",
                ex,
                "The list filter could not be stored for this person; the list still works, and the next visit simply starts unfiltered.");
        }
    }

    private UserListFilter CurrentFilter() => new(
        SearchText ?? string.Empty,
        SelectedRole is { IsAllRoles: false } ? SelectedRole.RoleCode : string.Empty);

    private string UserScopeKey() => $"user:{_startupState.UserId}";

    private void ReplacePeople(IReadOnlyList<Module_Core.Models.UserManagement.UserRosterRow> rows)
    {
        People.Clear();
        foreach (var row in rows)
        {
            People.Add(new UserSummary(row));
        }
    }

    /// <summary>
    /// Rebuilds the table's rows: the whole filtered roster in the reader's order, cut down to the page they are
    /// on. Paging is this slice and nothing else, which is what keeps it inside the filter (T045).
    /// </summary>
    private void ShowCurrentPage()
    {
        // A page size or a filter can leave the page the reader was on past the end of the roster.
        CurrentPage = Math.Clamp(CurrentPage, 1, PageCount);

        var ordered = OrderedPeople();

        PageRows.Clear();
        foreach (var person in ordered.Skip((CurrentPage - 1) * RowsPerPage).Take(RowsPerPage))
        {
            PageRows.Add(person);
        }

        AnnouncePageState();
    }

    private IEnumerable<UserSummary> OrderedPeople() => SortColumn switch
    {
        RosterSortColumn.SignInName => OrderByText(person => person.UsernameNormalized),
        RosterSortColumn.EmployeeNumber => OrderByText(person => person.EmployeeIdentifier),
        RosterSortColumn.Role => OrderByText(person => person.RoleText),
        RosterSortColumn.Status => SortDescending
            ? People.OrderByDescending(person => person.IsActive)
            : People.OrderBy(person => person.IsActive),
        _ => OrderByText(person => person.DisplayName),
    };

    /// <summary>
    /// Orders text the way a person reads it: the machine's own collation with case set aside, so the sign-in
    /// names, which the store holds in capitals, sort where a reader looks for them rather than before every
    /// lower-case entry.
    /// </summary>
    private IOrderedEnumerable<UserSummary> OrderByText(Func<UserSummary, string> key) =>
        SortDescending
            ? People.OrderByDescending(key, StringComparer.CurrentCultureIgnoreCase)
            : People.OrderBy(key, StringComparer.CurrentCultureIgnoreCase);

    /// <summary>A different page size is a different set of pages, so the reader goes back to the first one.</summary>
    partial void OnRowsPerPageChanged(int value)
    {
        CurrentPage = 1;
        ShowCurrentPage();
    }

    private void AnnounceState()
    {
        OnPropertyChanged(nameof(HasPeople));
        OnPropertyChanged(nameof(IsRosterEmpty));
        OnPropertyChanged(nameof(IsNoMatch));
        OnPropertyChanged(nameof(IsFilterApplied));
        OnPropertyChanged(nameof(HiddenCountText));
        AnnouncePageState();
    }

    private void AnnouncePageState()
    {
        OnPropertyChanged(nameof(PageCount));
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        OnPropertyChanged(nameof(RangeText));
        OnPropertyChanged(nameof(PageText));
        OnPropertyChanged(nameof(RosterCountText));
        OnPropertyChanged(nameof(HasPeople));
    }
}
