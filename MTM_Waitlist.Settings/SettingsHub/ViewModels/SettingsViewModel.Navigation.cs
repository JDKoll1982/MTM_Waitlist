using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Logging;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using Windows.ApplicationModel;

namespace MTM_Waitlist.Module_Settings.ViewModels;

public partial class SettingsViewModel : ObservableRecipient, INavigationAware
{
    /// <summary>Opens the General page.</summary>
    [RelayCommand]
    private void OpenGeneralSettings() => _navigationService.NavigateTo(GeneralSettingsRouteViewModelName);

    /// <summary>Opens the Operations page.</summary>
    [RelayCommand]
    private void OpenOperationsSettings() => _navigationService.NavigateTo(OperationsSettingsRouteViewModelName);

    /// <summary>Opens the Pictures page.</summary>
    [RelayCommand]
    private void OpenPicturesSettings() => _navigationService.NavigateTo(PicturesSettingsRouteViewModelName);

    /// <summary>Opens the Devices page.</summary>
    [RelayCommand]
    private void OpenDevicesSettings() => _navigationService.NavigateTo(DevicesSettingsRouteViewModelName);

    /// <summary>Opens the Diagnostics page.</summary>
    [RelayCommand]
    private void OpenDiagnosticsSettings() => _navigationService.NavigateTo(DiagnosticsSettingsRouteViewModelName);

    /// <summary>Opens the Administration page.</summary>
    [RelayCommand]
    private void OpenAdministrationSettings() => _navigationService.NavigateTo(AdministrationSettingsRouteViewModelName);

    /// <summary>What the hub's search box currently holds.</summary>
    /// <remarks>
    /// Deliberately not the pages' own <see cref="SearchQuery"/>: that one filters the panels of the page you are
    /// already on, while this one finds the page you are not on yet.
    /// </remarks>
    [ObservableProperty]
    public partial string PageSearchQuery
    {
        get; set;
    } = string.Empty;

    /// <summary>The settings the hub's search box offers for the current term.</summary>
    public ObservableCollection<SettingsSearchTarget> PageSearchSuggestions { get; } = [];

    /// <summary>
    /// Fills the suggestion list for <paramref name="query"/>, leaving out every page this reader may not open.
    /// </summary>
    public void UpdatePageSearch(string? query)
    {
        PageSearchSuggestions.Clear();

        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        foreach (var target in SettingsSearchCatalog.All.Where(target => IsPageAvailable(target.PageRoute) && target.Matches(query)))
        {
            PageSearchSuggestions.Add(target);
        }
    }

    /// <summary>The first setting that answers <paramref name="query"/>, or null when nothing does.</summary>
    public SettingsSearchTarget? FindPageSearch(string? query) =>
        SettingsSearchCatalog.All.FirstOrDefault(target => IsPageAvailable(target.PageRoute) && target.Matches(query));

    /// <summary>
    /// Opens the page that owns a setting and asks it to bring that setting's own section into view.
    /// </summary>
    /// <remarks>
    /// The section travels as the navigation parameter, because each page has its own instance of this view model:
    /// a field set here would belong to the hub's instance and never reach the page's.
    /// </remarks>
    [RelayCommand]
    private void OpenSearchTarget(SettingsSearchTarget? target)
    {
        if (target is null || !IsPageAvailable(target.PageRoute))
        {
            return;
        }

        _navigationService.NavigateTo(target.PageRoute, target.SectionKey);
    }

    /// <summary>Whether the page behind <paramref name="pageRoute"/> is offered to this reader.</summary>
    private bool IsPageAvailable(string pageRoute) => pageRoute switch
    {
        GeneralSettingsRouteViewModelName => IsGeneralSettingsPageAvailable,
        OperationsSettingsRouteViewModelName => IsOperationsSettingsPageAvailable,
        PicturesSettingsRouteViewModelName => IsPicturesSettingsPageAvailable,
        DevicesSettingsRouteViewModelName => ComputerManagement.CanManageComputers,
        DiagnosticsSettingsRouteViewModelName => IsDiagnosticsSettingsPageAvailable,
        AdministrationSettingsRouteViewModelName => IsAdministrationSettingsPageAvailable,
        _ => false,
    };

    /// <summary>
    /// The Users entry. It navigates rather than expanding, so it is a card rather than an expander, and it is
    /// reachable from the Settings search like every other entry on this screen.
    /// </summary>
    public bool IsUserManagementEntryVisible => CanOpenUserManagement && MatchesSearch(
        "user",
        "users",
        "person",
        "people",
        "account",
        "accounts",
        "roster",
        "employee",
        "administration");

    /// <summary>The Permissions entry. It navigates rather than expanding, and it is searchable.</summary>
    public bool IsPermissionsEntryVisible => CanOpenPermissions && MatchesSearch(
        "permission",
        "permissions",
        "access",
        "allowed",
        "who can",
        "administration");

    /// <summary>Opens the user list. The page service routes by view-model name, as every other page does.</summary>
    [RelayCommand]
    private void OpenUserManagement() => _navigationService.NavigateTo(UserManagementViewModelName);

    /// <summary>Opens the permissions page.</summary>
    [RelayCommand]
    private void OpenPermissions() => _navigationService.NavigateTo(PermissionsViewModelName);

    /// <summary>
    /// Opens the lugger arrangement (FR-030). The route is refused on the same permission answer the card is drawn
    /// from, so a person who does not hold the key cannot be taken there even if the command is reached some other
    /// way (FR-029).
    /// </summary>
    [RelayCommand]
    private void OpenLuggerArrangement()
    {
        if (!CanOpenLuggerArrangement)
        {
            return;
        }

        _navigationService.NavigateTo(LuggerArrangementViewModelName);
    }

    /// <summary>The Administration category's heading, resolved from the resource map.</summary>
    public string AdministrationCategoryTitle => "Administration_Category.Title".GetLocalized();

    /// <summary>The Users entry's title.</summary>
    public string UserManagementEntryTitle => "Administration_Users.Title".GetLocalized();

    /// <summary>What the Users entry opens, in a sentence.</summary>
    public string UserManagementEntryDescription => "Administration_Users.Description".GetLocalized();

    /// <summary>The Permissions entry's title.</summary>
    public string PermissionsEntryTitle => "Administration_Permissions.Title".GetLocalized();

    /// <summary>What the Permissions entry opens, in a sentence.</summary>
    public string PermissionsEntryDescription => "Administration_Permissions.Description".GetLocalized();

    /// <summary>
    /// The lugger arrangement entry (013-lugger-location-config, FR-030). Offered to an account holding its own
    /// permission and absent for an account without it, exactly as the two Administration entries are.
    /// </summary>
    public bool IsLuggerArrangementEntryVisible => CanOpenLuggerArrangement && MatchesSearch(
        "lugger",
        "luggers",
        "scrap",
        "dump",
        "arrangement",
        "location",
        "locations",
        "handler",
        "handlers");

    /// <summary>The lugger entry's title.</summary>
    public string LuggerArrangementEntryTitle => "Settings_Lugger_Arrangement.EntryTitle".GetLocalized();

    /// <summary>What the lugger entry opens, in a sentence.</summary>
    public string LuggerArrangementEntryDescription => "Settings_Lugger_Arrangement.EntryDescription".GetLocalized();

    /// <summary>
    /// The part-picture entry (008-part-pictures, FR-024). Offered to an account holding the picture entitlement
    /// and absent for an account without it, exactly as the two Administration entries are: the entitlement is
    /// read from the permission store and never from a list of role names kept here.
    /// </summary>
    public bool IsPartPicturesEntryVisible => CanManageImageLocationSettings && MatchesSearch(
        "part",
        "parts",
        "picture",
        "pictures",
        "image",
        "images",
        "photo",
        "photos",
        "visual",
        "wip",
        "missing");

    /// <summary>Opens the part-picture screen.</summary>
    [RelayCommand]
    private void OpenPartPictures() => _navigationService.NavigateTo(PartPictureManagerViewModelName);

    /// <summary>The part-picture entry's title.</summary>
    public string PartPicturesEntryTitle => "Settings_PartPictures_SectionTitle.Text".GetLocalized();

    /// <summary>What the part-picture entry opens, in a sentence.</summary>
    public string PartPicturesEntryDescription => "Settings_PartPictures_Description.Text".GetLocalized();

    public bool IsCacheRefreshPanelVisible => CanRequestCacheRefresh && MatchesSearch(
        "cache",
        "refresh",
        "cached data",
        "infor visual",
        "stale");

    public bool IsAppearancePanelVisible => MatchesSearch("appearance", "app theme", "light", "dark", "default", SelectedThemeText);

    /// <summary>
    /// The enlarge-pictures row. It lives inside the Appearance section, which is what the category below accounts
    /// for: searching for this row must leave its section standing rather than hiding the thing that matched.
    /// </summary>
    public bool IsEnlargePicturesPanelVisible => MatchesSearch(
        "enlarge",
        "enlarged",
        "click",
        "zoom",
        "full size",
        "full picture",
        "picture",
        "image");

    /// <summary>
    /// Whether this installation can actually deliver a new-request notification.
    /// </summary>
    /// <remarks>
    /// This is the same condition the delivery path evaluates — <c>AppNotificationService.Initialize</c> and
    /// <c>Show</c> both refuse unless the app has package identity — so the panel can never offer a control
    /// the notification path would silently ignore. It is a per-installation answer, not a platform rule.
    /// </remarks>
    public bool IsNewRequestAlertsAvailable => RuntimeHelper.IsMSIX;

    /// <summary>Localized explanation shown when this installation cannot deliver a notification.</summary>
    public string NewRequestAlertsUnavailableMessage => "Settings_NewRequestAlerts.Unavailable".GetLocalized();

    /// <summary>
    /// The panel's description: what the alert does when this installation can deliver one, and why the
    /// setting is unavailable when it cannot. Evaluated once — the answer cannot change while the app runs.
    /// </summary>
    public string NewRequestAlertsDescription => IsNewRequestAlertsAvailable
        ? "Settings_NewRequestAlerts.Description".GetLocalized()
        : NewRequestAlertsUnavailableMessage;

    /// <summary>The enlarge-pictures row's label, in the reader's language.</summary>
    public string EnlargePicturesTitle => "Settings_EnlargePictures.Title".GetLocalized();

    /// <summary>What the row does, and whose setting it is.</summary>
    public string EnlargePicturesDescription => "Settings_EnlargePictures.Description".GetLocalized();

    public bool IsNewRequestAlertsPanelVisible => MatchesSearch(
        "alert",
        "notification",
        "notify",
        "new request",
        "waitlist");

    /// <summary>
    /// The minutes panel: the configured figure per Item beside the average its completed requests took.
    /// </summary>
    public bool IsUrgencyAllotmentsPanelVisible => MatchesSearch(
        "urgency",
        "max allotted",
        "allotted",
        "allotted minutes",
        "deadline",
        "overdue",
        "remaining time",
        "item minutes");

    public bool IsHotWorkCentersPanelVisible => MatchesSearch(
        "Local Work Centers",
        "workstation",
        "computer",
        string.Join(" ", HotWorkCenters),
        string.Join(" ", OtherWorkCenters),
        string.Join(" ", AvailableWorkstations.Select(option => option.Label)));

    /// <summary>
    /// The Buildings panel: the plant's building list, which both a work centre and a computer name.
    /// </summary>
    public bool IsBuildingsPanelVisible => MatchesSearch(
        "building",
        "buildings",
        "plant",
        "facility",
        string.Join(" ", EditableBuildings));

    public bool IsDunnageTypeVisibilityPanelVisible => MatchesSearch(
        "dunnage",
        "visibility",
        "shown",
        "hidden",
        string.Join(" ", VisibleDunnageTypes.Select(item => item.Name)),
        string.Join(" ", HiddenDunnageTypes.Select(item => item.Name)));

    /// <summary>
    /// The locations-to-hide panel. Reading it is open to a person holding
    /// <c>permission.settings.ignored_locations</c>, whose grants are unchanged (SC-012); whether the controls
    /// inside it accept a change is a separate answer, from the edit key.
    /// </summary>
    public bool IsIgnoredLocationsPanelVisible => CanReadIgnoredLocations && MatchesSearch(
        "ignored",
        "inventory location",
        "location",
        "infor visual",
        "quantity in house",
        string.Join(" ", IgnoredLocations));

    /// <summary>
    /// The session-length panel. Offered only to a person who may change the length, and the write itself is
    /// refused on the same answer (FR-029).
    /// </summary>
    public bool IsSessionLengthPanelVisible => CanManageSessionLength && MatchesSearch(
        "session",
        "session length",
        "sign in",
        "timeout",
        "sign out",
        "how long");

    /// <summary>
    /// The developer log panel. Kept out of the tree for a person who may not open it, and the panel's own view
    /// model refuses the read and the copy on the same key (US5, SC-005).
    /// </summary>
    public bool IsDeveloperLogPanelVisible => CanOpenDeveloperLogPanel && MatchesSearch(
        "log",
        "logs",
        "diagnostics",
        "fault",
        "error",
        "exception");

    /// <summary>
    /// The row that decides how much the log store keeps, offered beside the log panel to the same person.
    /// </summary>
    /// <remarks>
    /// Gated on <c>permission.settings.log_panel</c> rather than on a key of its own, because it decides the same
    /// thing about the same screen: whoever may read the log is the person who may say how much of it is kept.
    /// Nothing here gates <em>writing</em> to the store — every computer records what happens on it, for every
    /// person who signs in, and this row only widens or narrows what is worth keeping (FR-032, FR-025).
    /// </remarks>
    public bool IsLogStoreLevelPanelVisible => CanOpenDeveloperLogPanel
        && _logStoreLevelService is not null
        && MatchesSearch(
            "log",
            "logs",
            "logging",
            "store",
            "severity",
            "level",
            "diagnostics",
            "verbosity",
            "how much is kept");

    /// <summary>The log-store section's heading.</summary>
    public string LogStoreLevelPanelTitle => "Settings_LogStoreLevel_Title.Text".GetLocalized();

    /// <summary>What the section holds, in a sentence.</summary>
    public string LogStoreLevelPanelDescription => "Settings_LogStoreLevel_Description.Text".GetLocalized();

    /// <summary>The label on the severity box.</summary>
    public string LogStoreLevelLabel => "Settings_LogStoreLevel_Label.Text".GetLocalized();

    /// <summary>The section's save action.</summary>
    public string LogStoreLevelSaveLabel => "Settings_LogStoreLevel_Save.Text".GetLocalized();

    /// <summary>The line saying what the store is doing right now.</summary>
    public string LogStoreLevelStoreIsTruthText => "Settings_LogStoreLevel_StoreIsTruth.Text".GetLocalized();

    /// <summary>What the session-length box is for, in the reader's language.</summary>
    public string SessionLengthTitle => "Settings_SessionLength_Title.Text".GetLocalized();

    /// <summary>What changing the length does, and when it takes effect.</summary>
    public string SessionLengthDescription => "Settings_SessionLength_Description.Text".GetLocalized();

    /// <summary>The label on the minutes box.</summary>
    public string SessionLengthLabel => "Settings_SessionLength_Label.Text".GetLocalized();

    /// <summary>The panel's save action.</summary>
    public string SessionLengthSaveLabel => "Settings_SessionLength_Save.Text".GetLocalized();

    /// <summary>The developer log panel's heading, shown only to a person who may open it.</summary>
    public string DeveloperLogPanelTitle => "Settings_LogPanel_Title.Text".GetLocalized();

    public bool IsAboutPanelVisible => MatchesSearch("about", "version", "privacy", VersionDescription, "mtm waitlist");

    public bool IsAppearanceCategoryVisible => IsAppearancePanelVisible || IsEnlargePicturesPanelVisible;

    /// <summary>
    /// The picture screen's entry point. Its visibility is gated on
    /// <see cref="CanManageImageLocationSettings"/>, which is <b>not</b> the minutes editor's gate: FR-020's
    /// "the role gate" is two gates, and collapsing them would hand the picture screen to a role that never had
    /// it.
    /// </summary>
    public bool IsImageLocationSettingsPanelVisible => CanManageImageLocationSettings && MatchesSearch(
        "image location",
        "image settings",
        "request type images",
        "item images",
        "work center images",
        "image path",
        "request type",
        "item",
        "work center");

    /// <summary>The label on the picture-folder box.</summary>
    public string StoragePathsImageFolderLabel => "Settings_StoragePaths_ImageFolder.Label".GetLocalized();

    /// <summary>The label on the dunnage-picture-root box.</summary>
    public string StoragePathsDunnageRootLabel => "Settings_StoragePaths_DunnageRoot.Label".GetLocalized();

    /// <summary>The label on the hopper-picture-folder box (FR-078).</summary>
    public string StoragePathsHopperFolderLabel => "Settings_StoragePaths_HopperFolder.Label".GetLocalized();

    /// <summary>
    /// The sentence under the hopper-picture-folder box, which names the three files the folder must hold. The
    /// contract is written in the panel rather than only in a document, because a folder whose naming rule nobody
    /// has read is a folder filled with the wrong names (FR-078).
    /// </summary>
    public string StoragePathsHopperFolderHint => "Settings_StoragePaths_HopperFolder.Hint".GetLocalized();

    /// <summary>The label on the button that opens a folder dialog beside the hopper-picture-folder box.</summary>
    public string StoragePathsHopperFolderBrowseLabel =>
        "Settings_StoragePaths_HopperFolderBrowse.Text".GetLocalized();

    /// <summary>
    /// What a screen reader announces for that button, which names the box it fills rather than repeating the
    /// button's own word.
    /// </summary>
    public string StoragePathsHopperFolderBrowseName =>
        "Settings_StoragePaths_HopperFolderBrowse.Name".GetLocalized();

    /// <summary>The label on the lugger-picture-folder box.</summary>
    public string StoragePathsLuggerFolderLabel => "Settings_StoragePaths_LuggerFolder.Label".GetLocalized();

    /// <summary>
    /// The sentence under the lugger-picture-folder box, which states the file-naming rule. Written in the panel for
    /// the same reason the hopper rule is: a folder whose naming rule nobody has read is a folder filled with the
    /// wrong names.
    /// </summary>
    public string StoragePathsLuggerFolderHint => "Settings_StoragePaths_LuggerFolder.Hint".GetLocalized();

    /// <summary>The label on the button that opens a folder dialog beside the lugger-picture-folder box.</summary>
    public string StoragePathsLuggerFolderBrowseLabel =>
        "Settings_StoragePaths_LuggerFolderBrowse.Text".GetLocalized();

    /// <summary>What a screen reader announces for that button.</summary>
    public string StoragePathsLuggerFolderBrowseName =>
        "Settings_StoragePaths_LuggerFolderBrowse.Name".GetLocalized();

    /// <summary>The label on the no-picture-image box.</summary>
    public string StoragePathsNoImagePathLabel => "Settings_StoragePaths_NoImagePath.Label".GetLocalized();

    /// <summary>The sentence under the no-picture-image box, which says where it is drawn.</summary>
    public string StoragePathsNoImagePathHint => "Settings_StoragePaths_NoImagePath.Hint".GetLocalized();

    /// <summary>The label on the button that picks the no-picture image.</summary>
    public string StoragePathsNoImagePathBrowseLabel =>
        "Settings_StoragePaths_NoImagePathBrowse.Text".GetLocalized();

    /// <summary>What a screen reader announces for that button.</summary>
    public string StoragePathsNoImagePathBrowseName =>
        "Settings_StoragePaths_NoImagePathBrowse.Name".GetLocalized();

    /// <summary>The label on the retention-period box.</summary>
    public string StoragePathsArchiveKeepDaysLabel => "Settings_StoragePaths_ArchiveKeepDays.Label".GetLocalized();

    /// <summary>The panel's save action.</summary>
    public string StoragePathsSaveLabel => "Settings_StoragePaths_Save.Label".GetLocalized();

    /// <summary>The line saying that what is shown is what every computer reads.</summary>
    public string StoragePathsStoreIsTruthText => "Settings_StoragePaths_StoreIsTruth.Text".GetLocalized();

    /// <summary>
    /// The panel holding the two storage folders and the period a replaced picture is kept (FR-016, FR-037).
    /// </summary>
    /// <remarks>
    /// Gated on the storage entitlement rather than the picture entitlement, because FR-017 keeps the folders with
    /// IT Department and Developer while FR-025 widens the picture entitlement past them. A host with no storage
    /// resolver offers nothing, so no control is drawn that would do nothing.
    /// </remarks>
    public bool IsStoragePathsPanelVisible => CanManageStoragePaths
        && _imageStorageConfigurationResolver is not null
        && MatchesSearch(
            "storage",
            "picture folder",
            "image folder",
            "key folder",
            "keys folder",
            "retention",
            "archive",
            "keep days",
            "replaced picture",
            "no picture",
            "no image",
            "placeholder",
            "default picture");

    /// <summary>
    /// The picture cache's settings.
    /// </summary>
    /// <remarks>
    /// Gated on <see cref="CanManageStoragePaths"/> — <c>permission.settings.storage_paths</c>, which is IT
    /// Department and Developer — and <b>not</b> on the picture entitlement. It was on the picture entitlement
    /// only because the two keys happened to have the same members; FR-025 widens the picture entitlement to Setup
    /// Lead and Plant Manager, and this panel chooses the folder this machine copies pictures from, which is a
    /// storage control FR-017 reserves (research D10). Moving the gate is what stops the widening handing a Setup
    /// Lead that folder.
    /// </remarks>
    public bool IsPictureCachePanelVisible => CanManageStoragePaths
        && _imageStorageConfigurationResolver is not null
        && MatchesSearch(
            "picture cache",
            "image cache",
            "cache",
            "cached pictures",
            "offline pictures",
            "local copy",
            "images",
            "pictures");

    /// <summary>
    /// The single Pictures panel: the picture assignments, the picture folders and the copies kept on this
    /// computer sit behind one heading rather than behind three.
    /// </summary>
    /// <remarks>
    /// The panel is the union of its cards and is never gated on its own. Each card inside it keeps the
    /// entitlement it had, so a person holding only the picture entitlement, or only the storage one, sees the
    /// panel and only the card they may use (FR-017, FR-024, FR-025).
    /// </remarks>
    public bool IsImagesPanelVisible =>
        IsPartPicturesEntryVisible
        || IsImageLocationSettingsPanelVisible
        || IsStoragePathsPanelVisible
        || IsPictureCachePanelVisible;

    /// <summary>Whether the Pictures panel offers the card holding the picture assignments.</summary>
    public bool IsPictureAssignmentsVisible =>
        IsImageLocationSettingsPanelVisible || IsPartPicturesEntryVisible;

    /// <summary>The Pictures panel's heading.</summary>
    public string ImagesPanelTitle => "Settings_Images_SectionTitle.Text".GetLocalized();

    /// <summary>What the Pictures panel holds, in a sentence.</summary>
    public string ImagesPanelDescription => "Settings_Images_Description.Text".GetLocalized();

    /// <summary>The caption on every folder choice in the Pictures panel.</summary>
    public string ImagesBrowseLabel => "Settings_Images_Browse.Text".GetLocalized();

    /// <summary>
    /// Whether this host can show a folder dialog at all. A host with no dialog draws no Browse button, because a
    /// button that would do nothing reads as a broken screen rather than as an unregistered service.
    /// </summary>
    public bool CanBrowseFolders => _folderBrowserService is not null;

    public bool IsComputersPanelVisible => ComputerManagement.CanManageComputers && MatchesSearch(
        "computer",
        "computers",
        "registry",
        "mac",
        "display name",
        string.Join(" ", ComputerManagement.Computers.Select(record => record.GetDisplayLabel())));

    public bool IsOperationsCategoryVisible => IsHotWorkCentersPanelVisible || IsBuildingsPanelVisible || IsDunnageTypeVisibilityPanelVisible || IsImagesPanelVisible || IsComputersPanelVisible || IsIgnoredLocationsPanelVisible || IsSessionLengthPanelVisible || IsDeveloperLogPanelVisible || IsLogStoreLevelPanelVisible || IsNewRequestAlertsPanelVisible || IsUrgencyAllotmentsPanelVisible;

    public bool IsAboutCategoryVisible => IsAboutPanelVisible;

    // FIX: A clean, type-safe string representation of the Enum for the XAML engine
    public string SelectedThemeText => ElementTheme.ToString();

    public ICommand SwitchThemeCommand
    {
        get;
    }

    public SettingsViewModel(
        IThemeSelectorService themeSelectorService,
        IIgnoredLocationsService ignoredLocationsService,
        IScopedPreferenceStore preferences,
        IWorkCenterCatalogService workCenterCatalogService,
        IDunnageTypeVisibilityCatalogService dunnageTypeVisibilityCatalogService,
        INewRequestAlertService newRequestAlertService,
        IPermissionService permissionService,
        INavigationService navigationService,
        IPersonIdentity personIdentity,
        ComputerManagementViewModel computerManagement,
        UrgencyAllotmentEditorViewModel urgencyAllotments,
        IMockServiceRefreshClient mockServiceRefreshClient,
        IImageStorageConfigurationResolver? imageStorageConfigurationResolver = null,
        IConfigSettingsValueService? configSettingsValueService = null,
        IImageCacheSyncService? imageCacheSyncService = null,
        IPictureEnlargePreference? pictureEnlargePreference = null,
        ILogStoreLevelService? logStoreLevelService = null,
        IFolderBrowserService? folderBrowserService = null,
        IBuildingCatalogService? buildingCatalogService = null,
        IBuildingSelectionService? buildingSelectionService = null)
    {
        AppLog.Info("SettingsViewModel", "Constructor started.");
        _themeSelectorService = themeSelectorService;
        _ignoredLocationsService = ignoredLocationsService;
        _preferences = preferences;
        _workCenterCatalogService = workCenterCatalogService;
        _dunnageTypeVisibilityCatalogService = dunnageTypeVisibilityCatalogService;
        _newRequestAlertService = newRequestAlertService;
        _permissionService = permissionService;
        _navigationService = navigationService;
        _personIdentity = personIdentity;
        _mockServiceRefreshClient = mockServiceRefreshClient;

        // Optional so that a host without a picture cache still opens this screen: with none registered the
        // section stays hidden rather than offering controls that would do nothing.
        _imageStorageConfigurationResolver = imageStorageConfigurationResolver;
        _configSettingsValueService = configSettingsValueService;
        _imageCacheSyncService = imageCacheSyncService;
        _pictureEnlargePreference = pictureEnlargePreference;
        _logStoreLevelService = logStoreLevelService;

        // Optional for the same reason as the resolver: a host with no folder dialog still opens this screen. The
        // box is then typed into and the Browse button does nothing, which is what a missing dialog amounts to.
        _folderBrowserService = folderBrowserService;

        // Optional for the same reason: a host with no building catalog still opens this screen. The Buildings
        // panel is then empty and the filter has nothing to narrow by, rather than the screen failing to open.
        _buildingCatalogService = buildingCatalogService;
        _buildingSelectionService = buildingSelectionService;
        ComputerManagement = computerManagement;
        UrgencyAllotments = urgencyAllotments;

        ElementTheme = _themeSelectorService.Theme;
        VersionDescription = GetVersionDescription();

        // Per-user new-request alert toggle, default OFF when never set. Read off the interface thread: the
        // screen draws at its shipped default and settles when the stored value arrives (FR-021).
        NewRequestAlertsLoad = InitializeNewRequestAlertsAsync();

        // The signed-in person's enlarge-pictures preference, default ON. The service holds the value already, so
        // reading it here costs nothing; with no service registered the shipped default is shown.
        _enlargePicturesInitializing = true;
        EnlargePicturesOnClick = _pictureEnlargePreference?.IsEnabled ?? true;
        _enlargePicturesInitializing = false;

        _ = UrgencyAllotments.LoadAsync();
        _ = InitializeIgnoredLocationsAsync();
        _ = InitializeSessionLengthAsync();
        _ = InitializeStoragePathsAsync();
        _ = InitializePictureCacheAsync();
        _ = InitializeLogStoreLevelAsync();

        // The two reads the panels depend on are kept, so a page or a test can await them rather than poll.
        HotWorkCentersLoad = InitializeHotWorkCentersAsync();
        BuildingsLoad = InitializeBuildingsAsync();

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    ElementTheme = param;
                    await _themeSelectorService.SetThemeAsync(param);
                }
            });

        HotWorkCentersLoad = InitializeHotWorkCentersAsync();
        BuildingsLoad = InitializeBuildingsAsync();
        _ = InitializeDunnageTypeVisibilityAsync();
        RefreshSearchVisibility();
        AppLog.Info("SettingsViewModel", $"Constructor completed. Theme='{ElementTheme}', Version='{VersionDescription}'.");
    }

    /// <summary>
    /// The permission read this screen started when it was navigated to.
    /// </summary>
    /// <remarks>
    /// Exposed so a caller that needs the gates settled — the page's own load order, or a test — can await it
    /// rather than wait a fixed time and hope. It is <see cref="Task.CompletedTask"/> until the first entry.
    /// </remarks>
    public Task PermissionLoad { get; private set; } = Task.CompletedTask;

    /// <summary>
    /// The Local Work Centers read this screen started when it was built, including the catalog and the selected
    /// computer's saved building, so a caller can await the settled list instead of polling.
    /// </summary>
    public Task HotWorkCentersLoad { get; private set; } = Task.CompletedTask;

    /// <summary>The plant's building list read this screen started when it was built.</summary>
    public Task BuildingsLoad { get; private set; } = Task.CompletedTask;

    /// <summary>
    /// The new-request alert preference this screen started reading when it was built, so a caller can await the
    /// settled value instead of polling.
    /// </summary>
    public Task NewRequestAlertsLoad { get; private set; } = Task.CompletedTask;
}
