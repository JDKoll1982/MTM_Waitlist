using System.Collections.ObjectModel;
using System.Globalization;
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
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using Windows.ApplicationModel;

namespace MTM_Waitlist.Module_Settings.ViewModels;

public partial class SettingsViewModel : ObservableRecipient, INavigationAware
{
    /// <summary>
    /// The user list's view model, as the page service routes it. Named here because the two pages this feature
    /// adds are routed by view-model name like every other page, and this screen reaches them by name rather than
    /// by a reference to a type in another part of the feature.
    /// </summary>
    internal const string UserManagementViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.UserManagementViewModel";

    /// <summary>The permissions page's view model, routed the same way.</summary>
    internal const string PermissionsViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.PermissionsViewModel";

    /// <summary>
    /// The part-picture screen's view model, routed the same way (008-part-pictures). It is reached by name for
    /// the same reason the two Administration pages are: this screen must not hold a reference to a type the story
    /// phase that builds the page owns.
    /// </summary>
    internal const string PartPictureManagerViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.PartPictureManagerViewModel";

    /// <summary>
    /// Every permission this screen asks about, in one read: the four Settings subjects it gates itself, the two
    /// Administration entries it offers, and the two its child view models gate on (FR-056).
    /// </summary>
    /// <remarks>
    /// This is a list of permission keys the declaration holds, not a list of role names: a gate that kept its own
    /// role names is what this feature replaced (FR-054), and the parity of each key with the list it replaced is
    /// asserted against the shipped baselines rather than here.
    /// </remarks>
    private static readonly string[] s_permissionKeys =
    [
        PermissionKeys.SettingsIgnoredLocations,
        PermissionKeys.SettingsHotWorkCenters,
        PermissionKeys.SettingsPartPictures,
        PermissionKeys.SettingsStoragePaths,
        PermissionKeys.SettingsCacheRefresh,
        PermissionKeys.SettingsUrgencyMinutes,
        PermissionKeys.SettingsComputers,
        PermissionKeys.AdminUsers,
        PermissionKeys.AdminPermissions,
    ];

    /// <summary>
    /// Every property whose getter consumes <c>MatchesSearch</c>.
    /// </summary>
    /// <remarks>
    /// Declared as data so <see cref="RefreshSearchVisibility"/> can iterate it and a coverage check can
    /// prove the next panel was registered rather than silently forgotten — a panel missing from this list
    /// keeps showing the previous term's answer, which is a wrong result rather than a cosmetic gap
    /// (FR-019/FR-020/FR-021).
    /// </remarks>
    private static readonly string[] s_searchAwareProperties =
    [
        nameof(IsAppearancePanelVisible),
        nameof(IsEnlargePicturesPanelVisible),
        nameof(IsHotWorkCentersPanelVisible),
        nameof(IsDunnageTypeVisibilityPanelVisible),
        nameof(IsIgnoredLocationsPanelVisible),
        nameof(IsCacheRefreshPanelVisible),
        nameof(IsAboutPanelVisible),
        nameof(IsComputersPanelVisible),
        nameof(IsNewRequestAlertsPanelVisible),
        nameof(IsUrgencyAllotmentsPanelVisible),
        nameof(IsImageLocationSettingsPanelVisible),
        nameof(IsStoragePathsPanelVisible),
        nameof(IsPictureCachePanelVisible),
        nameof(IsUserManagementEntryVisible),
        nameof(IsPermissionsEntryVisible),
        nameof(IsPartPicturesEntryVisible),
    ];

    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IWorkCenterCatalogService _workCenterCatalogService;
    private readonly IDunnageTypeVisibilityCatalogService _dunnageTypeVisibilityCatalogService;
    private readonly INewRequestAlertService _newRequestAlertService;
    private readonly IPermissionService _permissionService;
    private readonly INavigationService _navigationService;
    private readonly StartupState _startupState;
    private readonly IMockServiceRefreshClient _mockServiceRefreshClient;
    private readonly IImageStorageConfigurationResolver? _imageStorageConfigurationResolver;
    private readonly IConfigSettingsValueService? _configSettingsValueService;
    private readonly IImageCacheSyncService? _imageCacheSyncService;
    private readonly IPictureEnlargePreference? _pictureEnlargePreference;

    // Suppresses the OnNewRequestAlertsEnabledChanged side effect while the initial value is loaded in the
    // constructor, so opening the page does not log a misleading "changed" or re-persist.
    private bool _newRequestAlertInitializing = true;

    // The same guard for the picture cache's toggle: the stored value arrives after the constructor has run, and
    // reading it must not be mistaken for somebody having changed it.
    private bool _pictureCacheInitializing = true;

    // And the same guard for the enlarge-pictures toggle.
    private bool _enlargePicturesInitializing = true;

    public ComputerManagementViewModel ComputerManagement { get; }

    public UrgencyAllotmentEditorViewModel UrgencyAllotments { get; }

    [ObservableProperty]
    public partial ElementTheme ElementTheme
    {
        get; set;
    }

    [ObservableProperty]
    public partial string VersionDescription
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool NewRequestAlertsEnabled
    {
        get; set;
    }

    /// <summary>
    /// Whether clicking a picture enlarges it. A reading preference of the signed-in person (default on), so it
    /// needs no permission gate: anybody may decide this for themselves.
    /// </summary>
    [ObservableProperty]
    public partial bool EnlargePicturesOnClick
    {
        get; set;
    } = true;

    [ObservableProperty]
    public partial string SearchQuery
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string SelectedWorkstation
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsHotWorkCentersBusy
    {
        get; set;
    }

    [ObservableProperty]
    public partial string HotWorkCentersStatusMessage
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsDunnageTypeVisibilityBusy
    {
        get; set;
    }

    [ObservableProperty]
    public partial string DunnageTypeVisibilityStatusMessage
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string IgnoredLocationInput
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string IgnoredLocationsStatusMessage
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsCacheRefreshing
    {
        get; set;
    }

    [ObservableProperty]
    public partial string CacheRefreshStatusMessage
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// Whether pictures are copied onto this computer, for every computer that signs in rather than only this one.
    /// </summary>
    [ObservableProperty]
    public partial bool IsPictureCacheEnabled
    {
        get; set;
    } = true;

    /// <summary>The folder this computer keeps its cached pictures in, as configured.</summary>
    [ObservableProperty]
    public partial string PictureCacheFolderPath
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// What the folder box holds.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="PictureCacheFolderPath"/> so that typing in the box does not look like a saved
    /// change, and an edit nobody saved is never treated as the configured folder.
    /// </remarks>
    [ObservableProperty]
    public partial string PictureCacheFolderInput
    {
        get; set;
    } = string.Empty;

    /// <summary>What the picture cache section last did, in a sentence.</summary>
    [ObservableProperty]
    public partial string PictureCacheStatusMessage
    {
        get; set;
    } = string.Empty;

    /// <summary>Whether a picture copy is running, so the button is not started twice.</summary>
    [ObservableProperty]
    public partial bool IsPictureCacheBusy
    {
        get; set;
    }

    /// <summary>
    /// The picture folder that is in effect for every computer, as the store holds it (FR-009, FR-010).
    /// </summary>
    [ObservableProperty]
    public partial string PictureFolderPath
    {
        get; set;
    } = string.Empty;

    /// <summary>What the picture folder box holds, kept apart from the saved value for the same reason as the cache folder.</summary>
    [ObservableProperty]
    public partial string PictureFolderInput
    {
        get; set;
    } = string.Empty;

    /// <summary>The folder holding the key files, as the store holds it (FR-016).</summary>
    [ObservableProperty]
    public partial string KeysFolderPath
    {
        get; set;
    } = string.Empty;

    /// <summary>What the key folder box holds.</summary>
    [ObservableProperty]
    public partial string KeysFolderInput
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// How many days a replaced picture is kept before it is cleaned up (FR-037).
    /// </summary>
    /// <remarks>
    /// A <see cref="double"/> because it is bound to a <c>NumberBox</c>, whose value is one; it is rounded to a
    /// whole number of days before it is stored, so a half-typed figure never reaches the store.
    /// </remarks>
    [ObservableProperty]
    public partial double ArchiveKeepDaysInput
    {
        get; set;
    } = ConfigSettingKeys.ImageStorageArchiveKeepDaysDefault;

    /// <summary>What the storage panel last did, in a sentence.</summary>
    [ObservableProperty]
    public partial string StoragePathsStatusMessage
    {
        get; set;
    } = string.Empty;

    /// <summary>Whether the storage panel is saving, so its button is not started twice.</summary>
    [ObservableProperty]
    public partial bool IsStoragePathsBusy
    {
        get; set;
    }

    /// <summary>
    /// One line naming this computer's own configured picture folder when that is not the folder every computer
    /// reads, and empty when the two agree.
    /// </summary>
    /// <remarks>
    /// The store is the truth (OQ-3, settled by this feature): a computer whose settings file still carries the old
    /// path must be told so in one sentence rather than left to wonder why a picture is not where it expected
    /// (FR-010). Reporting it beats silently following the file, which is what would make two computers disagree
    /// about where one recorded picture lives.
    /// </remarks>
    [ObservableProperty]
    public partial string StoragePathsDisagreementMessage
    {
        get; set;
    } = string.Empty;

    public ObservableCollection<ComputerOption> AvailableWorkstations { get; } = new();

    public ObservableCollection<string> HotWorkCenters { get; } = new();

    public ObservableCollection<string> OtherWorkCenters { get; } = new();

    public ObservableCollection<DunnageTypeVisibilityOption> VisibleDunnageTypes { get; } = new();

    public ObservableCollection<DunnageTypeVisibilityOption> HiddenDunnageTypes { get; } = new();

    public ObservableCollection<string> IgnoredLocations { get; } = new();

    /// <summary>
    /// Whether the signed-in person may change the hot work centres, answered from
    /// <c>permission.settings.hot_work_centers</c> rather than from a list of role names kept here (FR-054).
    /// </summary>
    /// <remarks>
    /// False until the screen's permission read returns, so the control is never shown on a guess. The read is
    /// asynchronous and runs when the page is navigated to (FR-114).
    /// </remarks>
    [ObservableProperty]
    public partial bool CanManageHotWorkCenters
    {
        get; set;
    }

    /// <summary>Whether the signed-in person may change the part-picture locations.</summary>
    [ObservableProperty]
    public partial bool CanManageImageLocationSettings
    {
        get; set;
    }

    /// <summary>
    /// Whether the signed-in person may change where the pictures are kept (FR-017).
    /// </summary>
    /// <remarks>
    /// This is <c>permission.settings.storage_paths</c>, which is not the picture entitlement: FR-025 widens the
    /// picture entitlement to Setup Lead and Plant Manager, and the folders stay with IT Department and Developer.
    /// Reading the two keys separately is what keeps the widening from handing a Setup Lead the picture root.
    /// </remarks>
    [ObservableProperty]
    public partial bool CanManageStoragePaths
    {
        get; set;
    }

    /// <summary>Whether the signed-in person may open the user list.</summary>
    [ObservableProperty]
    public partial bool CanOpenUserManagement
    {
        get; set;
    }

    /// <summary>Whether the signed-in person may open the permissions page.</summary>
    [ObservableProperty]
    public partial bool CanOpenPermissions
    {
        get; set;
    }

    /// <summary>
    /// The dunnage-type visibility panel shares the hot work centres' permission, because it decides the same
    /// thing about the same screen and was never a second gate.
    /// </summary>
    public bool CanManageDunnageTypeVisibility => CanManageHotWorkCenters;

    /// <summary>Whether the signed-in person may change the ignored locations.</summary>
    [ObservableProperty]
    public partial bool CanManageIgnoredLocations
    {
        get; set;
    }

    /// <summary>
    /// Whether this operator may ask the on-host service to rebuild the cache now (T160).
    /// </summary>
    /// <remarks>
    /// Authorization is layered, and this is the outer layer: the service independently reads
    /// <c>permission.cache.refresh_api</c> for the presented user and refuses a caller who does not hold it. This
    /// gate reads the Settings screen's own key, and the one subset rule ties the two together: whatever
    /// <c>permission.settings.cache_refresh</c> admits is also admitted by <c>permission.cache.refresh_api</c>
    /// (FR-057), so an operator is never shown a control the service would refuse.
    /// </remarks>
    [ObservableProperty]
    public partial bool CanRequestCacheRefresh
    {
        get; set;
    }

    /// <summary>
    /// The Administration category: shown when the reader may open at least one of its two entries and may find
    /// it, and not at all when they may open neither (FR-081).
    /// </summary>
    public bool IsAdministrationCategoryVisible => IsUserManagementEntryVisible || IsPermissionsEntryVisible;

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

    public bool IsDunnageTypeVisibilityPanelVisible => MatchesSearch(
        "dunnage",
        "visibility",
        "shown",
        "hidden",
        string.Join(" ", VisibleDunnageTypes.Select(item => item.Name)),
        string.Join(" ", HiddenDunnageTypes.Select(item => item.Name)));

    public bool IsIgnoredLocationsPanelVisible => MatchesSearch(
        "ignored",
        "inventory location",
        "location",
        "infor visual",
        "quantity in house",
        string.Join(" ", IgnoredLocations));

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

    /// <summary>The storage panel's heading.</summary>
    public string StoragePathsPanelTitle => "Settings_StoragePaths_SectionTitle.Text".GetLocalized();

    /// <summary>What the storage panel holds, in a sentence.</summary>
    public string StoragePathsPanelDescription => "Settings_StoragePaths_Description.Text".GetLocalized();

    /// <summary>The label on the picture-folder box.</summary>
    public string StoragePathsImageFolderLabel => "Settings_StoragePaths_ImageFolder.Label".GetLocalized();

    /// <summary>The label on the key-folder box.</summary>
    public string StoragePathsKeysFolderLabel => "Settings_StoragePaths_KeysFolder.Label".GetLocalized();

    /// <summary>The label on the retention-period box.</summary>
    public string StoragePathsArchiveKeepDaysLabel => "Settings_StoragePaths_ArchiveKeepDays.Label".GetLocalized();

    /// <summary>The panel's save action.</summary>
    public string StoragePathsSaveLabel => "Settings_StoragePaths_Save.Label".GetLocalized();

    /// <summary>The line saying that what is shown is what every computer reads.</summary>
    public string StoragePathsStoreIsTruthText => "Settings_StoragePaths_StoreIsTruth.Text".GetLocalized();

    /// <summary>
    /// Whether the disagreement line has anything to say.
    /// </summary>
    /// <remarks>
    /// Collapsed rather than empty, so a machine that agrees with the store is not shown a blank line that reads
    /// as a message that failed to load.
    /// </remarks>
    public Visibility StoragePathsDisagreementVisibility =>
        string.IsNullOrEmpty(StoragePathsDisagreementMessage) ? Visibility.Collapsed : Visibility.Visible;

    partial void OnStoragePathsDisagreementMessageChanged(string value) =>
        OnPropertyChanged(nameof(StoragePathsDisagreementVisibility));

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
            "replaced picture");

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

    public bool IsComputersPanelVisible => ComputerManagement.CanManageComputers && MatchesSearch(
        "computer",
        "computers",
        "registry",
        "mac",
        "display name",
        string.Join(" ", ComputerManagement.Computers.Select(record => record.GetDisplayLabel())));

    public bool IsOperationsCategoryVisible => IsHotWorkCentersPanelVisible || IsDunnageTypeVisibilityPanelVisible || IsImageLocationSettingsPanelVisible || IsPictureCachePanelVisible || IsPartPicturesEntryVisible || IsComputersPanelVisible || IsIgnoredLocationsPanelVisible || IsNewRequestAlertsPanelVisible || IsUrgencyAllotmentsPanelVisible;

    public bool IsAboutCategoryVisible => IsAboutPanelVisible;

    // FIX: A clean, type-safe string representation of the Enum for the XAML engine
    public string SelectedThemeText => ElementTheme.ToString();

    public ICommand SwitchThemeCommand
    {
        get;
    }

    public SettingsViewModel(
        IThemeSelectorService themeSelectorService,
        ILocalSettingsService localSettingsService,
        IWorkCenterCatalogService workCenterCatalogService,
        IDunnageTypeVisibilityCatalogService dunnageTypeVisibilityCatalogService,
        INewRequestAlertService newRequestAlertService,
        IPermissionService permissionService,
        INavigationService navigationService,
        StartupState startupState,
        ComputerManagementViewModel computerManagement,
        UrgencyAllotmentEditorViewModel urgencyAllotments,
        IMockServiceRefreshClient mockServiceRefreshClient,
        IImageStorageConfigurationResolver? imageStorageConfigurationResolver = null,
        IConfigSettingsValueService? configSettingsValueService = null,
        IImageCacheSyncService? imageCacheSyncService = null,
        IPictureEnlargePreference? pictureEnlargePreference = null)
    {
        StartupDebugLog.Info("SettingsViewModel", "Constructor started.");
        _themeSelectorService = themeSelectorService;
        _localSettingsService = localSettingsService;
        _workCenterCatalogService = workCenterCatalogService;
        _dunnageTypeVisibilityCatalogService = dunnageTypeVisibilityCatalogService;
        _newRequestAlertService = newRequestAlertService;
        _permissionService = permissionService;
        _navigationService = navigationService;
        _startupState = startupState;
        _mockServiceRefreshClient = mockServiceRefreshClient;

        // Optional so that a host without a picture cache still opens this screen: with none registered the
        // section stays hidden rather than offering controls that would do nothing.
        _imageStorageConfigurationResolver = imageStorageConfigurationResolver;
        _configSettingsValueService = configSettingsValueService;
        _imageCacheSyncService = imageCacheSyncService;
        _pictureEnlargePreference = pictureEnlargePreference;
        ComputerManagement = computerManagement;
        UrgencyAllotments = urgencyAllotments;

        ElementTheme = _themeSelectorService.Theme;
        VersionDescription = GetVersionDescription();

        // Per-user new-request alert toggle, default OFF when never set.
        _newRequestAlertInitializing = true;
        NewRequestAlertsEnabled = _newRequestAlertService.GetEnabledAsync().GetAwaiter().GetResult();
        _newRequestAlertInitializing = false;

        // The signed-in person's enlarge-pictures preference, default ON. The service holds the value already, so
        // reading it here costs nothing; with no service registered the shipped default is shown.
        _enlargePicturesInitializing = true;
        EnlargePicturesOnClick = _pictureEnlargePreference?.IsEnabled ?? true;
        _enlargePicturesInitializing = false;

        _ = UrgencyAllotments.LoadAsync();
        InitializeIgnoredLocations();
        _ = InitializeStoragePathsAsync();
        _ = InitializePictureCacheAsync();

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    ElementTheme = param;
                    await _themeSelectorService.SetThemeAsync(param);
                }
            });

        _ = InitializeHotWorkCentersAsync();
        _ = InitializeDunnageTypeVisibilityAsync();
        RefreshSearchVisibility();
        StartupDebugLog.Info("SettingsViewModel", $"Constructor completed. Theme='{ElementTheme}', Version='{VersionDescription}'.");
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
    /// Loads this screen's permission answers when the page is reached, so the gates are read per visit rather
    /// than decided from stored session state at construction (FR-114).
    /// </summary>
    /// <remarks>
    /// The read is issued here rather than in a layout pass, so the screen is never drawn from an answer that has
    /// not arrived and the interface thread is never blocked waiting for one.
    /// </remarks>
    public void OnNavigatedTo(object parameter) => PermissionLoad = LoadPermissionsAsync();

    /// <summary>
    /// Nothing is torn down on the way out: the screen subscribes to no service of its own, so it holds nothing to
    /// release.
    /// </summary>
    /// <remarks>
    /// The permission answers are re-read on every entry rather than kept from the first one. That is not a store
    /// read per visit: the permission service caches a session's answers and is invalidated when one is saved, so
    /// the second ask is free unless somebody's permissions actually changed while this screen was away.
    /// </remarks>
    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// Asks the permission service once for every gate this screen and its two child view models need, then
    /// applies the answers.
    /// </summary>
    /// <remarks>
    /// One call answers the whole screen, which is what keeps a store read out of a layout pass and off the
    /// interface thread (FR-056). Every gate stays false until its answer arrives, so a control is never offered
    /// on a guess; and because an unreachable store is answered by each key's shipped fallback rather than by a
    /// refusal (FR-050), the failure path hides the controls instead of breaking the screen. The failure is
    /// recorded rather than swallowed.
    /// </remarks>
    public async Task LoadPermissionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var answers = await _permissionService
                .HasPermissionsAsync(s_permissionKeys, cancellationToken)
                .ConfigureAwait(true);

            CanManageIgnoredLocations = answers[PermissionKeys.SettingsIgnoredLocations];
            CanManageHotWorkCenters = answers[PermissionKeys.SettingsHotWorkCenters];
            CanManageImageLocationSettings = answers[PermissionKeys.SettingsPartPictures];
            CanManageStoragePaths = answers[PermissionKeys.SettingsStoragePaths];
            CanRequestCacheRefresh = answers[PermissionKeys.SettingsCacheRefresh];
            CanOpenUserManagement = answers[PermissionKeys.AdminUsers];
            CanOpenPermissions = answers[PermissionKeys.AdminPermissions];

            UrgencyAllotments.ApplyPermission(answers[PermissionKeys.SettingsUrgencyMinutes]);
            ComputerManagement.ApplyPermission(answers[PermissionKeys.SettingsComputers]);

            RefreshSearchVisibility();
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "SettingsPermissions",
                ex,
                "The Settings screen's permission answers could not be read; every gated control stays hidden rather than being offered on a guess.");
        }
    }

    partial void OnCanManageHotWorkCentersChanged(bool value) =>
        OnPropertyChanged(nameof(CanManageDunnageTypeVisibility));

    // FIX: This partial method is automatically invoked by the MVVM Toolkit source generator 
    // whenever the ElementTheme property is modified, updating our custom XAML text field.
    partial void OnElementThemeChanged(ElementTheme value)
    {
        StartupDebugLog.Info("SettingsViewModel", $"Theme changed to '{value}'.");
        OnPropertyChanged(nameof(SelectedThemeText));
        RefreshSearchVisibility();
    }

    partial void OnNewRequestAlertsEnabledChanged(bool value)
    {
        if (_newRequestAlertInitializing)
        {
            return; // initial load; do not log/persist as if the user changed it
        }

        if (!IsNewRequestAlertsAvailable)
        {
            // The installation cannot deliver a notification, so there is no preference to record. The
            // toggle is disabled for this reason; this guard keeps that structural rather than cosmetic.
            StartupDebugLog.Info("SettingsViewModel", "NewRequestAlertsEnabled was asked for on an installation that cannot deliver a notification; nothing was stored.");
            return;
        }

        StartupDebugLog.Info("SettingsViewModel", $"NewRequestAlertsEnabled changed to {value}.");
        _ = _newRequestAlertService.SetEnabledAsync(value);
        RefreshSearchVisibility();
    }

    partial void OnSearchQueryChanged(string value)
    {
        StartupDebugLog.Info("SettingsViewModel", $"SearchQuery changed to '{value}'.");
        RefreshSearchVisibility();
    }

    /// <summary>
    /// Stores the enlarge-pictures preference for the signed-in person. Applied through the shared service, so the
    /// screens already open follow the change rather than needing a restart.
    /// </summary>
    partial void OnEnlargePicturesOnClickChanged(bool value)
    {
        if (_enlargePicturesInitializing)
        {
            return;
        }

        StartupDebugLog.Info("SettingsViewModel", $"EnlargePicturesOnClick changed to {value}.");
        _ = _pictureEnlargePreference?.SetEnabledAsync(value);
    }

    partial void OnSelectedWorkstationChanged(string value)
    {
        StartupDebugLog.Info("SettingsViewModel", $"SelectedWorkstation changed to '{value}'.");
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _ = LoadCatalogForWorkstationAsync(value);
    }

    [RelayCommand]
    private async Task AddHotWorkCenterAsync(string? workCenter)
    {
        if (!CanManageHotWorkCenters)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(workCenter))
        {
            return;
        }

        var normalizedWorkCenter = workCenter.Trim();
        StartupDebugLog.Info("SettingsHotWorkCenters", $"AddHotWorkCenterAsync started. WorkCenter='{normalizedWorkCenter}', Workstation='{SelectedWorkstation}'.");
        if (HotWorkCenters.Any(value => string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase)))
        {
            StartupDebugLog.Info("SettingsHotWorkCenters", $"AddHotWorkCenterAsync skipped because '{normalizedWorkCenter}' is already pinned.");
            return;
        }

        var existingOthers = OtherWorkCenters
            .Where(value => !string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        HotWorkCenters.Add(normalizedWorkCenter);
        SortCollection(HotWorkCenters);
        ReplaceCollectionValues(OtherWorkCenters, existingOthers);
        RefreshSearchVisibility();

        await SaveCurrentHotWorkCentersAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task RemoveHotWorkCenterAsync(string? workCenter)
    {
        if (!CanManageHotWorkCenters)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(workCenter))
        {
            return;
        }

        var normalizedWorkCenter = workCenter.Trim();
        StartupDebugLog.Info("SettingsHotWorkCenters", $"RemoveHotWorkCenterAsync started. WorkCenter='{normalizedWorkCenter}', Workstation='{SelectedWorkstation}'.");
        var updatedHot = HotWorkCenters
            .Where(value => !string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        ReplaceCollectionValues(HotWorkCenters, updatedHot);

        if (!OtherWorkCenters.Any(value => string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase)))
        {
            OtherWorkCenters.Add(normalizedWorkCenter);
            SortCollection(OtherWorkCenters);
        }

        RefreshSearchVisibility();

        await SaveCurrentHotWorkCentersAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task ShowDunnageTypeAsync(string? dunnageTypeId)
    {
        if (!CanManageDunnageTypeVisibility)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(dunnageTypeId))
        {
            return;
        }

        var normalizedId = dunnageTypeId.Trim();
        var selectedOption = HiddenDunnageTypes.FirstOrDefault(item => string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase));
        if (selectedOption is null)
        {
            return;
        }

        StartupDebugLog.Info("SettingsDunnageVisibility", $"ShowDunnageTypeAsync started. DunnageTypeId='{normalizedId}'.");

        var nextHidden = HiddenDunnageTypes.Where(item => !string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase)).ToArray();
        var nextVisible = VisibleDunnageTypes.Concat(new[] { selectedOption }).ToArray();

        ReplaceDunnageTypeValues(HiddenDunnageTypes, nextHidden);
        ReplaceDunnageTypeValues(VisibleDunnageTypes, nextVisible);
        RefreshSearchVisibility();

        await SaveCurrentDunnageTypeVisibilityAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task HideDunnageTypeAsync(string? dunnageTypeId)
    {
        if (!CanManageDunnageTypeVisibility)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(dunnageTypeId))
        {
            return;
        }

        var normalizedId = dunnageTypeId.Trim();
        var selectedOption = VisibleDunnageTypes.FirstOrDefault(item => string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase));
        if (selectedOption is null)
        {
            return;
        }

        StartupDebugLog.Info("SettingsDunnageVisibility", $"HideDunnageTypeAsync started. DunnageTypeId='{normalizedId}'.");

        var nextVisible = VisibleDunnageTypes.Where(item => !string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase)).ToArray();
        var nextHidden = HiddenDunnageTypes.Concat(new[] { selectedOption }).ToArray();

        ReplaceDunnageTypeValues(VisibleDunnageTypes, nextVisible);
        ReplaceDunnageTypeValues(HiddenDunnageTypes, nextHidden);
        RefreshSearchVisibility();

        await SaveCurrentDunnageTypeVisibilityAsync().ConfigureAwait(true);
    }

    private async Task InitializeHotWorkCentersAsync()
    {
        StartupDebugLog.Info("SettingsViewModel", "InitializeHotWorkCentersAsync started.");
        IsHotWorkCentersBusy = true;
        try
        {
            var workstations = await _workCenterCatalogService.GetAvailableComputersAsync().ConfigureAwait(true);
            ReplaceCollectionValues(AvailableWorkstations, workstations);

            var currentWorkstation = _workCenterCatalogService.GetCurrentComputerName();
            var resolvedWorkstation = (AvailableWorkstations.FirstOrDefault(item =>
                                           string.Equals(item.Key, currentWorkstation, StringComparison.OrdinalIgnoreCase))
                                       ?? AvailableWorkstations.FirstOrDefault())?.Key
                ?? currentWorkstation;

            var workstationChanged = !string.Equals(SelectedWorkstation, resolvedWorkstation, StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(SelectedWorkstation))
            {
                SelectedWorkstation = resolvedWorkstation;
            }
            else if (workstationChanged)
            {
                SelectedWorkstation = resolvedWorkstation;
            }
            else
            {
                await LoadCatalogForWorkstationAsync(SelectedWorkstation).ConfigureAwait(true);
            }

            RefreshSearchVisibility();

            StartupDebugLog.Info("SettingsViewModel", $"InitializeHotWorkCentersAsync completed. Workstation='{SelectedWorkstation}', AvailableCount={AvailableWorkstations.Count}.");
        }
        finally
        {
            IsHotWorkCentersBusy = false;
        }
    }

    private async Task LoadCatalogForWorkstationAsync(string workstationName)
    {
        StartupDebugLog.Info("SettingsViewModel", $"LoadCatalogForWorkstationAsync started. Workstation='{workstationName}'.");
        IsHotWorkCentersBusy = true;
        try
        {
            var catalog = await _workCenterCatalogService.GetCatalogAsync(workstationName).ConfigureAwait(true);
            ReplaceCollectionValues(HotWorkCenters, catalog.HotWorkCenters);
            ReplaceCollectionValues(OtherWorkCenters, catalog.OtherWorkCenters);
            HotWorkCentersStatusMessage = string.Empty;
            RefreshSearchVisibility();
            StartupDebugLog.Info("SettingsViewModel", $"LoadCatalogForWorkstationAsync completed. Workstation='{workstationName}', HotCount={HotWorkCenters.Count}, OtherCount={OtherWorkCenters.Count}.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SettingsViewModel", ex, $"LoadCatalogForWorkstationAsync failed. Workstation='{workstationName}'.");
            HotWorkCentersStatusMessage = $"Unable to load work centers: {ex.Message}";
        }
        finally
        {
            IsHotWorkCentersBusy = false;
        }
    }

    private async Task SaveCurrentHotWorkCentersAsync()
    {
        try
        {
            StartupDebugLog.Info("SettingsHotWorkCenters", $"SaveCurrentHotWorkCentersAsync started. Workstation='{SelectedWorkstation}', Count={HotWorkCenters.Count}.");
            var saveMessage = await _workCenterCatalogService
                .SaveHotWorkCentersAsync(SelectedWorkstation, HotWorkCenters.ToArray())
                .ConfigureAwait(true);

            HotWorkCentersStatusMessage = string.IsNullOrWhiteSpace(saveMessage)
                ? "Local workcenters saved."
                : saveMessage;

            StartupDebugLog.Info("SettingsHotWorkCenters", $"SaveCurrentHotWorkCentersAsync completed. Workstation='{SelectedWorkstation}', Message='{HotWorkCentersStatusMessage}'.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SettingsHotWorkCenters", ex, $"SaveCurrentHotWorkCentersAsync failed. Workstation='{SelectedWorkstation}', Count={HotWorkCenters.Count}.");
            HotWorkCentersStatusMessage = $"Unable to save Local workcenters: {ex.Message}";
        }
    }

    private async Task InitializeDunnageTypeVisibilityAsync()
    {
        StartupDebugLog.Info("SettingsDunnageVisibility", "InitializeDunnageTypeVisibilityAsync started.");
        IsDunnageTypeVisibilityBusy = true;
        try
        {
            var catalog = await _dunnageTypeVisibilityCatalogService.GetCatalogAsync().ConfigureAwait(true);
            ReplaceDunnageTypeValues(VisibleDunnageTypes, catalog.VisibleDunnageTypes);
            ReplaceDunnageTypeValues(HiddenDunnageTypes, catalog.HiddenDunnageTypes);
            DunnageTypeVisibilityStatusMessage = string.Empty;
            RefreshSearchVisibility();
            StartupDebugLog.Info("SettingsDunnageVisibility", $"InitializeDunnageTypeVisibilityAsync completed. VisibleCount={VisibleDunnageTypes.Count}, HiddenCount={HiddenDunnageTypes.Count}.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SettingsDunnageVisibility", ex, "InitializeDunnageTypeVisibilityAsync failed.");
            DunnageTypeVisibilityStatusMessage = $"Unable to load dunnage visibility: {ex.Message}";
        }
        finally
        {
            IsDunnageTypeVisibilityBusy = false;
        }
    }

    private async Task SaveCurrentDunnageTypeVisibilityAsync()
    {
        try
        {
            IsDunnageTypeVisibilityBusy = true;
            var saveMessage = await _dunnageTypeVisibilityCatalogService
                .SaveVisibleDunnageTypesAsync(VisibleDunnageTypes.Select(item => item.Id).ToArray())
                .ConfigureAwait(true);

            DunnageTypeVisibilityStatusMessage = string.IsNullOrWhiteSpace(saveMessage)
                ? "Dunnage visibility saved."
                : saveMessage;

            StartupDebugLog.Info("SettingsDunnageVisibility", $"SaveCurrentDunnageTypeVisibilityAsync completed. VisibleCount={VisibleDunnageTypes.Count}, HiddenCount={HiddenDunnageTypes.Count}, Message='{DunnageTypeVisibilityStatusMessage}'.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SettingsDunnageVisibility", ex, "SaveCurrentDunnageTypeVisibilityAsync failed.");
            DunnageTypeVisibilityStatusMessage = $"Unable to save dunnage visibility: {ex.Message}";
        }
        finally
        {
            IsDunnageTypeVisibilityBusy = false;
        }
    }

    private static void ReplaceCollectionValues(ObservableCollection<string> targetCollection, IEnumerable<string> values)
    {
        targetCollection.Clear();
        foreach (var value in values
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Select(value => value.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            targetCollection.Add(value);
        }
    }

    private static void ReplaceCollectionValues(ObservableCollection<ComputerOption> targetCollection, IEnumerable<ComputerOption> values)
    {
        targetCollection.Clear();
        foreach (var value in values
                     .Where(item => !string.IsNullOrWhiteSpace(item.Key))
                     .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                     .Select(group => group.First())
                     .OrderBy(item => item.Label, StringComparer.OrdinalIgnoreCase))
        {
            targetCollection.Add(value);
        }
    }

    private static void SortCollection(ObservableCollection<string> values)
    {
        var sorted = values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        values.Clear();
        foreach (var value in sorted)
        {
            values.Add(value);
        }
    }

    private static void ReplaceDunnageTypeValues(ObservableCollection<DunnageTypeVisibilityOption> targetCollection, IEnumerable<DunnageTypeVisibilityOption> values)
    {
        targetCollection.Clear();
        foreach (var value in values
                     .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.Name))
                     .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                     .Select(group => group.First())
                     .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            targetCollection.Add(value);
        }
    }

    private bool MatchesSearch(params string[] values)
    {
        var query = SearchQuery?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return values.Any(value =>
            !string.IsNullOrWhiteSpace(value) && value.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Uppercases and trims a raw location code entry; returns an empty string when invalid.
    /// </summary>
    public static string NormalizeLocationCode(string? locationCode)
    {
        var normalized = (locationCode ?? string.Empty).Trim().ToUpperInvariant();
        return IsValidLocationCode(normalized) ? normalized : string.Empty;
    }

    /// <summary>
    /// Validates a location code against a simple code pattern (uppercase letters/digits with
    /// optional inner hyphens, e.g. WC, NCM, V-WC, NCM-VITS, SHIP).
    /// </summary>
    public static bool IsValidLocationCode(string? locationCode)
    {
        var value = (locationCode ?? string.Empty).Trim();
        if (value.Length < 1 || value.Length > 32)
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            var isAlphanumeric = (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
            var isInnerHyphen = c == '-' && i > 0 && i < value.Length - 1;
            if (!isAlphanumeric && !isInnerHyphen)
            {
                return false;
            }
        }

        return true;
    }

    private void InitializeIgnoredLocations()
    {
        var stored = _localSettingsService.ReadSettingAsync<List<string>?>(IgnoredLocationDefaults.SettingKey).GetAwaiter().GetResult();
        var seed = stored is { Count: > 0 }
            ? stored
            : IgnoredLocationDefaults.Locations;

        ReplaceCollectionValues(IgnoredLocations, seed);
        IgnoredLocationsStatusMessage = string.Empty;
        StartupDebugLog.Info("SettingsIgnoredLocations", $"InitializeIgnoredLocations completed. StoredCount={(stored is null ? 0 : stored.Count)}, ActiveCount={IgnoredLocations.Count}.");
    }

    [RelayCommand]
    private async Task AddIgnoredLocationAsync()
    {
        if (!CanManageIgnoredLocations)
        {
            return;
        }

        var code = NormalizeLocationCode(IgnoredLocationInput);
        if (string.IsNullOrWhiteSpace(code))
        {
            IgnoredLocationsStatusMessage = "Enter a valid location code (letters, digits, hyphens).";
            return;
        }

        if (IgnoredLocations.Any(value => string.Equals(value, code, StringComparison.OrdinalIgnoreCase)))
        {
            IgnoredLocationsStatusMessage = $"{code} is already ignored.";
            IgnoredLocationInput = string.Empty;
            return;
        }

        StartupDebugLog.Info("SettingsIgnoredLocations", $"AddIgnoredLocationAsync adding '{code}'.");
        IgnoredLocations.Add(code);
        SortCollection(IgnoredLocations);
        IgnoredLocationInput = string.Empty;
        IgnoredLocationsStatusMessage = string.Empty;
        RefreshSearchVisibility();
        await SaveIgnoredLocationsAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task RemoveIgnoredLocationAsync(string? locationCode)
    {
        if (!CanManageIgnoredLocations)
        {
            return;
        }

        var code = (locationCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        var updated = IgnoredLocations
            .Where(value => !string.Equals(value, code, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        ReplaceCollectionValues(IgnoredLocations, updated);
        IgnoredLocationsStatusMessage = string.Empty;
        RefreshSearchVisibility();
        StartupDebugLog.Info("SettingsIgnoredLocations", $"RemoveIgnoredLocationAsync removed '{code}'. Remaining={IgnoredLocations.Count}.");
        await SaveIgnoredLocationsAsync().ConfigureAwait(true);
    }

    private async Task SaveIgnoredLocationsAsync()
    {
        try
        {
            await _localSettingsService.SaveSettingAsync(IgnoredLocationDefaults.SettingKey, IgnoredLocations.ToList()).ConfigureAwait(true);
            StartupDebugLog.Info("SettingsIgnoredLocations", $"SaveIgnoredLocationsAsync saved {IgnoredLocations.Count} location(s).");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SettingsIgnoredLocations", ex, "SaveIgnoredLocationsAsync failed.");
            IgnoredLocationsStatusMessage = $"Unable to save ignored locations: {ex.Message}";
        }
    }

    /// <summary>
    /// Asks the on-host service to rebuild the cached Infor Visual reads now (T160).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This does not make the application refresh its own cache - the on-host service still owns the schedule
    /// and the write (FR-025). It asks that service to run one cycle early, and the same request can be made
    /// from the host with `POST /api/refresh`.
    /// </para>
    /// <para>
    /// Every failure path is a reported outcome, never an exception: an absent service, an unconfigured
    /// client and a refusal all land in <see cref="CacheRefreshStatusMessage"/> and leave the application
    /// serving cached content (FR-025, SC-011).
    /// </para>
    /// </remarks>
    /// <summary>
    /// Reads the picture cache's stored settings.
    /// </summary>
    /// <remarks>
    /// Started from the constructor and awaited by nothing, like the other panels on this screen: the page must not
    /// block on a store read, and the shipped defaults answer until the real values arrive. A read that fails
    /// leaves those defaults in place and is recorded rather than shown.
    /// </remarks>
    private async Task InitializePictureCacheAsync()
    {
        if (_imageStorageConfigurationResolver is null)
        {
            _pictureCacheInitializing = false;
            return;
        }

        try
        {
            PictureCacheFolderPath = await _imageStorageConfigurationResolver
                .GetImageCacheFolderPathAsync()
                .ConfigureAwait(true);
            PictureCacheFolderInput = PictureCacheFolderPath;

            IsPictureCacheEnabled = await _imageStorageConfigurationResolver
                .GetImageCacheEnabledAsync()
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "SettingsPictureCache",
                ex,
                "The picture cache's settings could not be read; the shipped defaults are shown and the store will be asked again next visit.");
        }
        finally
        {
            _pictureCacheInitializing = false;
            RefreshSearchVisibility();
        }
    }

    partial void OnIsPictureCacheEnabledChanged(bool value)
    {
        if (_pictureCacheInitializing)
        {
            return;
        }

        _ = SavePictureCacheEnabledAsync(value);
    }

    /// <summary>
    /// Stores the picture cache toggle for everybody, then says what will happen next.
    /// </summary>
    /// <remarks>
    /// The copy itself is not started here: switching it off should not mean "stop half way", and switching it on
    /// is what the next start does anyway. The message says "next time" for that reason.
    /// </remarks>
    private async Task SavePictureCacheEnabledAsync(bool value)
    {
        try
        {
            await SaveAppWideSettingAsync(
                ConfigSettingKeys.ImageCacheEnabled,
                "bool",
                text: null,
                boolean: value).ConfigureAwait(true);

            PictureCacheStatusMessage = value
                ? "Saved for every computer. Pictures will be copied onto each one the next time the application starts."
                : "Saved for every computer. Pictures will be read from the network the next time the application starts.";
        }
        catch (Exception ex)
        {
            PictureCacheStatusMessage = "Unable to save that change.";
            StartupDebugLog.Error("SettingsPictureCache", ex, "The picture cache toggle could not be stored.");
        }
    }

    /// <summary>Stores the cache folder that was typed into the box, for everybody.</summary>
    [RelayCommand]
    private async Task SavePictureCacheFolderAsync()
    {
        if (_configSettingsValueService is null || _imageStorageConfigurationResolver is null)
        {
            return;
        }

        var folder = PictureCacheFolderInput?.Trim() ?? string.Empty;

        if (folder.Length == 0)
        {
            PictureCacheStatusMessage = "A folder is needed before this can be saved.";
            return;
        }

        try
        {
            await SaveAppWideSettingAsync(
                ConfigSettingKeys.ImageCacheFolderPath,
                "text",
                folder,
                boolean: null).ConfigureAwait(true);

            PictureCacheFolderPath = folder;
            PictureCacheStatusMessage = $"Saved for every computer. Pictures will be cached in {folder}.";
        }
        catch (Exception ex)
        {
            PictureCacheStatusMessage = "Unable to save that folder.";
            StartupDebugLog.Error("SettingsPictureCache", ex, "The picture cache folder could not be stored.");
        }
    }

    /// <summary>
    /// Copies the pictures onto this computer now, without waiting for the next start.
    /// </summary>
    /// <remarks>
    /// The same synchronisation the startup step runs, so the two can never disagree about what a cached picture
    /// is. What it did is reported as counts, because a folder that turned out to be empty and a copy that did
    /// nothing look identical otherwise.
    /// </remarks>
    [RelayCommand]
    private async Task RefreshPictureCacheAsync()
    {
        if (_imageCacheSyncService is null || IsPictureCacheBusy)
        {
            return;
        }

        IsPictureCacheBusy = true;
        PictureCacheStatusMessage = "Copying pictures...";

        try
        {
            var result = await _imageCacheSyncService.SynchronizeAsync().ConfigureAwait(true);

            PictureCacheStatusMessage = result.SourcesSkipped.Count > 0
                ? $"Copied {result.Copied}, removed {result.Removed}. Nothing was done for {string.Join(", ", result.SourcesSkipped)}, which could not be reached."
                : $"Copied {result.Copied}, removed {result.Removed}.";
        }
        catch (Exception ex)
        {
            PictureCacheStatusMessage = "Unable to copy the pictures.";
            StartupDebugLog.Error("SettingsPictureCache", ex, "The picture copy requested from the Settings screen failed.");
        }
        finally
        {
            IsPictureCacheBusy = false;
        }
    }

    /// <summary>
    /// Loads the two storage folders and the retention period, then reports any disagreement with this machine's
    /// own settings file.
    /// </summary>
    /// <remarks>
    /// Started from the constructor and awaited by nothing, like the other panels here: the page must not block on
    /// a store read. A read that fails leaves the boxes empty and says so, rather than showing a value nobody
    /// configured.
    /// </remarks>
    private async Task InitializeStoragePathsAsync()
    {
        if (_imageStorageConfigurationResolver is null)
        {
            return;
        }

        try
        {
            PictureFolderPath = await _imageStorageConfigurationResolver
                .GetSharedFolderPathAsync()
                .ConfigureAwait(true);
            PictureFolderInput = PictureFolderPath;

            KeysFolderPath = await _imageStorageConfigurationResolver
                .GetKeysFolderPathAsync()
                .ConfigureAwait(true);
            KeysFolderInput = KeysFolderPath;

            ArchiveKeepDaysInput = await _imageStorageConfigurationResolver
                .GetArchiveKeepDaysAsync()
                .ConfigureAwait(true);

            await ReportStoragePathDisagreementAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StoragePathsStatusMessage = "Settings_StoragePaths_Unavailable.Text".GetLocalized();
            StartupDebugLog.Error(
                "SettingsStoragePaths",
                ex,
                "The storage folders could not be read; the boxes are left empty and the store is asked again next visit.");
        }
        finally
        {
            RefreshSearchVisibility();
        }
    }

    /// <summary>
    /// Names this machine's own configured picture folder when it is not the folder every computer reads.
    /// </summary>
    /// <remarks>
    /// The store wins (OQ-3), so a machine still carrying the old path in its settings file is working from the
    /// store's answer while appearing to be configured otherwise. Saying so in one line is what stops that reading
    /// as a picture that is simply not there. Which folder is the truth, and whether this machine's file disagrees
    /// with it, is the resolver's answer rather than a comparison kept here, so the rule has one home (FR-010).
    /// </remarks>
    private async Task ReportStoragePathDisagreementAsync()
    {
        if (_imageStorageConfigurationResolver is null)
        {
            return;
        }

        try
        {
            var resolution = await _imageStorageConfigurationResolver
                .GetSharedFolderResolutionAsync()
                .ConfigureAwait(true);

            StoragePathsDisagreementMessage = resolution.MachineDisagrees
                ? string.Format(
                    CultureInfo.CurrentCulture,
                    "Settings_StoragePaths_Disagreement.Text".GetLocalized(),
                    resolution.MachineFolderPath)
                : string.Empty;
        }
        catch (Exception ex)
        {
            // A folder that cannot be resolved is already reported by the read above; the panel must not fail on
            // the line that explains it.
            StoragePathsDisagreementMessage = string.Empty;
            StartupDebugLog.Error(
                "SettingsStoragePaths",
                ex,
                "The storage folder's disagreement with this machine's own configuration could not be worked out.");
        }
    }

    /// <summary>
    /// Stores the picture folder, the key folder and the retention period for every computer.
    /// </summary>
    /// <remarks>
    /// One button for the three, because they are one subject and one screen: three buttons over three boxes on one
    /// panel is three chances to leave the panel half saved. The period is refused when it is not a positive whole
    /// number of days rather than stored as a figure the cleanup would read as zero.
    /// </remarks>
    [RelayCommand]
    private async Task SaveStoragePathsAsync()
    {
        if (_configSettingsValueService is null || _imageStorageConfigurationResolver is null || IsStoragePathsBusy)
        {
            return;
        }

        var pictureFolder = PictureFolderInput?.Trim() ?? string.Empty;
        var keysFolder = KeysFolderInput?.Trim() ?? string.Empty;
        var keepDays = (int)Math.Round(ArchiveKeepDaysInput, MidpointRounding.AwayFromZero);

        if (pictureFolder.Length == 0 || keysFolder.Length == 0)
        {
            StoragePathsStatusMessage = "Settings_StoragePaths_FolderNeeded.Text".GetLocalized();
            return;
        }

        if (keepDays < 1)
        {
            StoragePathsStatusMessage = "Settings_StoragePaths_DaysNeeded.Text".GetLocalized();
            return;
        }

        IsStoragePathsBusy = true;

        try
        {
            await SaveAppWideSettingAsync(
                ConfigSettingKeys.ImageStorageSharedFolderPath,
                "text",
                pictureFolder,
                boolean: null).ConfigureAwait(true);

            await SaveAppWideSettingAsync(
                ConfigSettingKeys.KeysFolderPath,
                "text",
                keysFolder,
                boolean: null).ConfigureAwait(true);

            await SaveAppWideSettingAsync(
                ConfigSettingKeys.ImageStorageArchiveKeepDays,
                "int",
                text: null,
                boolean: null,
                integer: keepDays).ConfigureAwait(true);

            PictureFolderPath = pictureFolder;
            KeysFolderPath = keysFolder;
            await ReportStoragePathDisagreementAsync().ConfigureAwait(true);
            StoragePathsStatusMessage = "Settings_StoragePaths_Saved.Text".GetLocalized();
        }
        catch (Exception ex)
        {
            StoragePathsStatusMessage = "Settings_StoragePaths_SaveFailed.Text".GetLocalized();
            StartupDebugLog.Error("SettingsStoragePaths", ex, "A storage setting could not be stored.");
        }
        finally
        {
            IsStoragePathsBusy = false;
        }
    }

    /// <summary>
    /// Writes one picture-cache setting for every user of the application.
    /// </summary>
    /// <remarks>
    /// Scoped <c>all_users</c> deliberately: the requirement is that changing this on one computer changes it
    /// everywhere, which a person-scoped row would not do. The resolver's cached answers are dropped afterwards,
    /// because it holds them for minutes and would otherwise hand back the value that was just replaced.
    /// </remarks>
    /// <param name="settingKey">The setting key to write.</param>
    /// <param name="valueType">The value's type, so the right column is written.</param>
    /// <param name="text">The text value, for a text setting.</param>
    /// <param name="boolean">The flag value, for a flag setting.</param>
    /// <param name="integer">The whole-number value, for an int setting.</param>
    private async Task SaveAppWideSettingAsync(
        string settingKey,
        string valueType,
        string? text,
        bool? boolean,
        long? integer = null)
    {
        if (_configSettingsValueService is null)
        {
            throw new InvalidOperationException("No configuration store is available to write this setting to.");
        }

        await _configSettingsValueService.SetSettingValueAsync(
            new ConfigSettingValue
            {
                SettingKey = settingKey,
                ScopeType = "all_users",
                ScopeKey = "all_users",
                SettingValue = text,
                SettingValueBool = boolean,
                SettingValueInt = integer,
                ValueType = valueType,
            },
            _startupState.UserId > 0 ? _startupState.UserId : null).ConfigureAwait(true);

        _imageStorageConfigurationResolver?.InvalidateCache();
    }

    [RelayCommand]
    private async Task RequestCacheRefreshAsync()
    {
        if (!CanRequestCacheRefresh)
        {
            return;
        }

        IsCacheRefreshing = true;
        CacheRefreshStatusMessage = string.Empty;

        try
        {
            StartupDebugLog.Info("SettingsCacheRefresh", "RequestCacheRefreshAsync started.");

            var result = await _mockServiceRefreshClient.RequestRefreshAsync(null).ConfigureAwait(true);

            if (!result.Succeeded)
            {
                CacheRefreshStatusMessage = result.Message
                    ?? "The cache refresh service did not accept the request.";
                StartupDebugLog.Info("SettingsCacheRefresh", $"Request not completed: {CacheRefreshStatusMessage}");
                return;
            }

            var rows = result.ShapeOutcomes
                .Count(outcome => string.Equals(outcome.Value, "refreshed", StringComparison.OrdinalIgnoreCase));
            var notRefreshed = result.ShapeOutcomes
                .Where(outcome => !string.Equals(outcome.Value, "refreshed", StringComparison.OrdinalIgnoreCase))
                .Select(outcome => $"{outcome.Key}: {outcome.Value}")
                .ToArray();

            CacheRefreshStatusMessage = notRefreshed.Length == 0
                ? $"The cache was rebuilt. {rows} read shape(s) refreshed."
                : $"The cache was rebuilt, with {notRefreshed.Length} shape(s) not refreshed - "
                    + string.Join("; ", notRefreshed)
                    + ".";

            StartupDebugLog.Info("SettingsCacheRefresh", $"Request completed. {CacheRefreshStatusMessage}");
        }
        catch (Exception ex)
        {
            CacheRefreshStatusMessage = $"Unable to request a cache refresh: {ex.Message}";
            StartupDebugLog.Error("SettingsCacheRefresh", ex, "RequestCacheRefreshAsync failed.");
        }
        finally
        {
            IsCacheRefreshing = false;
            RefreshSearchVisibility();
        }
    }

    private void RefreshSearchVisibility()
    {
        foreach (var propertyName in s_searchAwareProperties)
        {
            OnPropertyChanged(propertyName);
        }

        // The category aggregates are computed from the panels above, so they announce after them.
        OnPropertyChanged(nameof(IsAppearanceCategoryVisible));
        OnPropertyChanged(nameof(IsOperationsCategoryVisible));
        OnPropertyChanged(nameof(IsAboutCategoryVisible));
        OnPropertyChanged(nameof(IsAdministrationCategoryVisible));
    }

    private static string GetVersionDescription()
    {
        Version version;
        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;
            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"{"AppDisplayName".GetLocalized()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
