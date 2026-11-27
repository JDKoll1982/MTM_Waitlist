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
    /// The lugger arrangement's view model, routed the same way (013-lugger-location-config). The edit itself lives
    /// on that screen's view model, which refuses the route for a person who does not hold its own key.
    /// </summary>
    internal const string LuggerArrangementViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.LuggerArrangementViewModel";

    /// <summary>The General page: the settings every person may change for themselves.</summary>
    internal const string GeneralSettingsRouteViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.GeneralSettingsRoute";

    /// <summary>The Operations page: the settings a lead changes for the floor.</summary>
    internal const string OperationsSettingsRouteViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.OperationsSettingsRoute";

    /// <summary>The Pictures page: picture assignments, picture folders and the local copies.</summary>
    internal const string PicturesSettingsRouteViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.PicturesSettingsRoute";

    /// <summary>The Devices page: the computer registry.</summary>
    internal const string DevicesSettingsRouteViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.DevicesSettingsRoute";

    /// <summary>The Diagnostics page: the developer log, its retention, and the cache refresh.</summary>
    internal const string DiagnosticsSettingsRouteViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.DiagnosticsSettingsRoute";

    /// <summary>The Administration page: people, permissions and the lugger arrangement.</summary>
    internal const string AdministrationSettingsRouteViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.AdministrationSettingsRoute";


    /// <summary>
    /// Every permission this screen asks about, in one read: the four Settings subjects it gates itself, the three
    /// navigation entries it offers, and the two its child view models gate on (FR-056).
    /// </summary>
    /// <remarks>
    /// This is a list of permission keys the declaration holds, not a list of role names: a gate that kept its own
    /// role names is what this feature replaced (FR-054), and the parity of each key with the list it replaced is
    /// asserted against the shipped baselines rather than here.
    /// </remarks>
    private static readonly string[] s_permissionKeys =
    [
        PermissionKeys.SettingsIgnoredLocations,
        PermissionKeys.SettingsIgnoredLocationsEdit,
        PermissionKeys.SettingsSessionLength,
        PermissionKeys.SettingsLogPanel,
        PermissionKeys.SettingsHotWorkCenters,
        PermissionKeys.SettingsBuildings,
        PermissionKeys.SettingsPartPictures,
        PermissionKeys.SettingsStoragePaths,
        PermissionKeys.SettingsCacheRefresh,
        PermissionKeys.SettingsUrgencyMinutes,
        PermissionKeys.SettingsComputers,
        PermissionKeys.AdminUsers,
        PermissionKeys.AdminPermissions,
        PermissionKeys.SettingsLuggerArrangement,
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
        nameof(IsBuildingsPanelVisible),
        nameof(IsDunnageTypeVisibilityPanelVisible),
        nameof(IsIgnoredLocationsPanelVisible),
        nameof(IsSessionLengthPanelVisible),
        nameof(IsDeveloperLogPanelVisible),
        nameof(IsCacheRefreshPanelVisible),
        nameof(IsAboutPanelVisible),
        nameof(IsComputersPanelVisible),
        nameof(IsNewRequestAlertsPanelVisible),
        nameof(IsUrgencyAllotmentsPanelVisible),
        nameof(IsImageLocationSettingsPanelVisible),
        nameof(IsStoragePathsPanelVisible),
        nameof(IsPictureCachePanelVisible),
        nameof(IsLogStoreLevelPanelVisible),
        nameof(IsUserManagementEntryVisible),
        nameof(IsPermissionsEntryVisible),
        nameof(IsPartPicturesEntryVisible),
        nameof(IsLuggerArrangementEntryVisible),
    ];

    private readonly IThemeSelectorService _themeSelectorService;
    private readonly IIgnoredLocationsService _ignoredLocationsService;
    private readonly IScopedPreferenceStore _preferences;
    private readonly IWorkCenterCatalogService _workCenterCatalogService;
    private readonly IDunnageTypeVisibilityCatalogService _dunnageTypeVisibilityCatalogService;
    private readonly INewRequestAlertService _newRequestAlertService;
    private readonly IPermissionService _permissionService;
    private readonly INavigationService _navigationService;
    private readonly IPersonIdentity _personIdentity;
    private readonly IMockServiceRefreshClient _mockServiceRefreshClient;
    private readonly IImageStorageConfigurationResolver? _imageStorageConfigurationResolver;
    private readonly IConfigSettingsValueService? _configSettingsValueService;
    private readonly IImageCacheSyncService? _imageCacheSyncService;
    private readonly IPictureEnlargePreference? _pictureEnlargePreference;
    private readonly ILogStoreLevelService? _logStoreLevelService;
    private readonly IFolderBrowserService? _folderBrowserService;
    private readonly IBuildingCatalogService? _buildingCatalogService;
    private readonly IBuildingSelectionService? _buildingSelectionService;

    /// <summary>
    /// Every candidate work centre before the building filter is applied, so switching the filter re-reads nothing
    /// from the store and a work centre pinned or unpinned is added to one list rather than to two.
    /// </summary>
    private IReadOnlyList<string> _allOtherWorkCenters = Array.Empty<string>();

    /// <summary>The building each catalog work centre records, which is what the Local Work Centers filter matches on.</summary>
    private IReadOnlyDictionary<string, string> _workCenterBuildings =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The catalog read a workstation change started, so the screen's own load can wait for it rather than leave a
    /// second, concurrent read of the same catalog behind.
    /// </summary>
    private Task _catalogLoad = Task.CompletedTask;

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

    /// <summary>
    /// The building the selected computer was told it stands in when it was set up. It is what the Local Work
    /// Centers filter uses in "Saved Building" mode, and what the toggle's own text states.
    /// </summary>
    [ObservableProperty]
    public partial string SelectedWorkstationBuilding
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// Whether the candidate work centres are limited to the selected computer's saved building (<c>true</c>) or
    /// drawn from every building (<c>false</c>). The toggle beside the computer choice is this flag.
    /// </summary>
    /// <remarks>
    /// On by default: the list an operator pins a computer's press list from is the list of work centres in the
    /// building that computer stands in, and the whole plant is the exception they switch to.
    /// </remarks>
    [ObservableProperty]
    public partial bool IsBuildingFilterEnabled
    {
        get; set;
    } = true;

    /// <summary>
    /// The building the candidate list is narrowed to while the filter is off: one building, or
    /// <see cref="BuildingScope.AllBuildings"/> for the whole plant.
    /// </summary>
    [ObservableProperty]
    public partial string SelectedWorkCenterBuilding
    {
        get; set;
    } = BuildingScope.AllBuildings;

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

    /// <summary>The building the Buildings panel is renaming, or <c>null</c> while a new one is being added.</summary>
    [ObservableProperty]
    public partial string? SelectedBuildingForEdit
    {
        get; set;
    }

    /// <summary>The name the Buildings panel is about to write.</summary>
    [ObservableProperty]
    public partial string BuildingNameInput
    {
        get; set;
    } = string.Empty;

    /// <summary>Whether the Buildings panel is reading or writing, so its save is not started twice.</summary>
    [ObservableProperty]
    public partial bool IsBuildingsBusy
    {
        get; set;
    }

    /// <summary>What the Buildings panel says about the last save: the confirmation or the stated refusal.</summary>
    [ObservableProperty]
    public partial string BuildingsStatusMessage
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// Whether the signed-in person may add or rename a building, answered from
    /// <c>permission.settings.buildings</c> rather than from a list of role names kept here.
    /// </summary>
    [ObservableProperty]
    public partial bool CanManageBuildings
    {
        get; set;
    }

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
    /// The least severe entry the log store keeps, as the store holds it.
    /// </summary>
    /// <remarks>
    /// The value in force, shown beside the box so a reader can see what the store is actually doing rather than
    /// only what the box holds. It is a severity name from the store's own vocabulary, and it is applied to the
    /// live threshold because the logging module's single <see cref="LogStoreLevel"/> is what both the seam and
    /// the <c>ILogger</c> provider consult.
    /// </remarks>
    [ObservableProperty]
    public partial string LogStoreMinimumLevel
    {
        get; set;
    } = LogStoreLevel.ShippedMinimum.ToString();

    /// <summary>What the level box holds, kept apart from the value in force so an unsaved pick is never treated as saved.</summary>
    [ObservableProperty]
    public partial string LogStoreMinimumLevelInput
    {
        get; set;
    } = LogStoreLevel.ShippedMinimum.ToString();

    /// <summary>What the log-store section last did, in a sentence.</summary>
    [ObservableProperty]
    public partial string LogStoreStatusMessage
    {
        get; set;
    } = string.Empty;

    /// <summary>Whether the level is being written, so the button is not started twice.</summary>
    [ObservableProperty]
    public partial bool IsLogStoreBusy
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

    /// <summary>
    /// What the dunnage root box holds, which is the root the dunnage pictures are read from for every computer
    /// (FR-040).
    /// </summary>
    [ObservableProperty]
    public partial string DunnageRootInput
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// What the hopper picture folder box holds: where the three hopper size reference pictures live, relative to
    /// the picture folder unless it is given as an absolute path (FR-078).
    /// </summary>
    /// <remarks>
    /// The three files it must contain are named after the size words — <c>Small</c>, <c>Medium</c> and
    /// <c>Large</c>, with a <c>.png</c>, <c>.jpg</c> or <c>.jpeg</c> extension — and the panel states them, because
    /// a folder whose contract is not written down is a folder somebody fills with the wrong names. The contract is
    /// documented in <c>docs/guides/guides-pictures-image-naming.md</c>.
    /// </remarks>
    [ObservableProperty]
    public partial string HopperFolderInput
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// What the lugger picture folder box holds: where the picture behind each lugger card lives.
    /// </summary>
    /// <remarks>
    /// One file per lugger, named after its place in the arrangement rather than after its name: <c>Lugger01</c> …
    /// <c>Lugger09</c>, then <c>Lugger10</c> … <c>Lugger99</c>. There is no Lugger100, because the arrangement holds at
    /// most 99 luggers. The panel states the rule, because a folder whose contract is not written down is a folder
    /// somebody fills with the wrong names.
    /// </remarks>
    [ObservableProperty]
    public partial string LuggerFolderInput
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// What the no-picture image box holds: the picture every surface draws where it has no picture of its own.
    /// </summary>
    /// <remarks>
    /// One picture rather than a folder, unlike the rows above it, because there is only ever one answer: an item,
    /// a work center and a lugger with no picture all draw the same thing, and that sameness is what teaches a
    /// person to read it as "no picture". A path that names no file is stored as it is typed and reported, rather
    /// than refused, because the artwork usually lives on the same share as the folders above: a share that is down
    /// for a minute must not refuse a save the person cannot complete later either.
    /// </remarks>
    [ObservableProperty]
    public partial string NoImagePathInput
    {
        get; set;
    } = ConfigSettingKeys.ImageStorageNoImagePathDefault;

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

    public ObservableCollection<ComputerOption> AvailableWorkstations { get; } = new();

    public ObservableCollection<string> HotWorkCenters { get; } = new();

    public ObservableCollection<string> OtherWorkCenters { get; } = new();

    /// <summary>
    /// The buildings the Local Work Centers filter offers, with <see cref="BuildingScope.AllBuildings"/> last.
    /// </summary>
    public ObservableCollection<string> BuildingChoices { get; } = new();

    /// <summary>The plant's buildings, in catalog order, as the Buildings panel lists them for editing.</summary>
    public ObservableCollection<string> EditableBuildings { get; } = new();

    /// <summary>
    /// What the filter's toggle says: the saved building's own name while the filter is on, and the words
    /// "All Work Centers" while it is off.
    /// </summary>
    public string BuildingFilterToggleLabel => IsBuildingFilterEnabled
        ? string.IsNullOrWhiteSpace(SelectedWorkstationBuilding)
            ? "Saved Building"
            : $"Saved Building — {SelectedWorkstationBuilding}"
        : "All Work Centers";

    /// <summary>
    /// Whether the building choice beside the filter can be used: it can only when the filter is off, because
    /// while the filter is on the answer is the selected computer's saved building and is not a choice.
    /// </summary>
    public bool IsBuildingChoiceEnabled => !IsBuildingFilterEnabled;

    public ObservableCollection<DunnageTypeVisibilityOption> VisibleDunnageTypes { get; } = new();

    public ObservableCollection<DunnageTypeVisibilityOption> HiddenDunnageTypes { get; } = new();

    public ObservableCollection<string> IgnoredLocations { get; } = new();

    /// <summary>
    /// The severities a person may choose between, from the store's own vocabulary.
    /// </summary>
    /// <remarks>
    /// Read from <see cref="LogStoreLevel.Chooseable"/> rather than spelled again here, so a severity added to the
    /// store is offered by the screen instead of being unreachable from it.
    /// </remarks>
    public IReadOnlyList<string> LogStoreLevelOptions { get; } =
        LogStoreLevel.Chooseable.Select(severity => severity.ToString()).ToArray();

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
    /// Whether the signed-in person may open the lugger arrangement, from
    /// <c>permission.settings.lugger_arrangement</c> (FR-028). The card is drawn from this answer alone and is not
    /// drawn at all when it is false, because a shown-but-disabled card is the state FR-029 forbids.
    /// </summary>
    [ObservableProperty]
    public partial bool CanOpenLuggerArrangement
    {
        get; set;
    }

    /// <summary>
    /// The dunnage-type visibility panel shares the hot work centres' permission, because it decides the same
    /// thing about the same screen and was never a second gate.
    /// </summary>
    public bool CanManageDunnageTypeVisibility => CanManageHotWorkCenters;

    /// <summary>
    /// Whether the signed-in person may see the locations to hide. This is
    /// <c>permission.settings.ignored_locations</c>, whose grants are deliberately unchanged (research D10).
    /// </summary>
    [ObservableProperty]
    public partial bool CanReadIgnoredLocations
    {
        get; set;
    }

    /// <summary>
    /// Whether the signed-in person may change the locations to hide. This is the separate
    /// <c>permission.settings.ignored_locations_edit</c> key, which is the deliberate narrowing: FR-024 admits
    /// two roles to the write while FR-030 keeps the existing read restriction exactly as it was.
    /// </summary>
    [ObservableProperty]
    public partial bool CanManageIgnoredLocations
    {
        get; set;
    }

    /// <summary>
    /// Whether the signed-in person may change how long a session lasts, from
    /// <c>permission.settings.session_length</c> (FR-029). The panel refuses the write on the same answer, so the
    /// gate is enforced where the change happens rather than only where the control is drawn.
    /// </summary>
    [ObservableProperty]
    public partial bool CanManageSessionLength
    {
        get; set;
    }

    /// <summary>
    /// Whether the signed-in person may open the developer log panel, from <c>permission.settings.log_panel</c>.
    /// This only keeps the panel out of the tree; the panel's own view model is what enforces the read and the
    /// copy (US5).
    /// </summary>
    [ObservableProperty]
    public partial bool CanOpenDeveloperLogPanel
    {
        get; set;
    }

    /// <summary>
    /// How long a sign-in lasts, in minutes, for the whole plant.
    /// </summary>
    /// <remarks>
    /// Held as a double because the control that edits it is a number box, and because a value typed into one
    /// arrives as a fractional number. It is rounded and clamped before it is stored.
    /// </remarks>
    [ObservableProperty]
    public partial double SessionLengthMinutes
    {
        get; set;
    } = ScopedPreferenceKeys.DefaultSessionLengthMinutes;

    /// <summary>What the session-length panel has to say: a refusal, a confirmation, or a fault.</summary>
    [ObservableProperty]
    public partial string SessionLengthStatusMessage
    {
        get; set;
    } = string.Empty;

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
    /// The Administration category: shown when the reader may open at least one of its entries and may find it, and
    /// not at all when they may open none of them (FR-081).
    /// </summary>
    public bool IsAdministrationCategoryVisible => IsUserManagementEntryVisible ||
        IsPermissionsEntryVisible ||
        IsLuggerArrangementEntryVisible;

    /// <summary>
    /// Whether the General page is offered. It holds only personal choices, so it is always offered — this answer
    /// exists so every card on the hub is drawn from a named answer rather than one card being a special case.
    /// </summary>
    public bool IsGeneralSettingsPageAvailable => true;

    /// <summary>Whether the Operations page is offered: at least one of its panels may be used.</summary>
    public bool IsOperationsSettingsPageAvailable =>
        CanManageHotWorkCenters
        || CanReadIgnoredLocations
        || CanManageSessionLength
        || UrgencyAllotments.CanManageUrgencySettings;

    /// <summary>Whether the Pictures page is offered: either picture entitlement opens it.</summary>
    public bool IsPicturesSettingsPageAvailable => CanManageImageLocationSettings || CanManageStoragePaths;

    /// <summary>Whether the Diagnostics page is offered: the log entitlement or the refresh entitlement opens it.</summary>
    public bool IsDiagnosticsSettingsPageAvailable => CanOpenDeveloperLogPanel || CanRequestCacheRefresh;

    /// <summary>Whether the Administration page is offered: at least one of its entries may be opened.</summary>
    public bool IsAdministrationSettingsPageAvailable =>
        CanOpenUserManagement || CanOpenPermissions || CanOpenLuggerArrangement;


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
            _personIdentity.UserId > 0 ? _personIdentity.UserId : null).ConfigureAwait(true);

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
            AppLog.Info("SettingsCacheRefresh", "RequestCacheRefreshAsync started.");

            var result = await _mockServiceRefreshClient.RequestRefreshAsync(null).ConfigureAwait(true);

            if (!result.Succeeded)
            {
                CacheRefreshStatusMessage = result.Message
                    ?? "The cache refresh service did not accept the request.";
                AppLog.Info("SettingsCacheRefresh", $"Request not completed: {CacheRefreshStatusMessage}");
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

            AppLog.Info("SettingsCacheRefresh", $"Request completed. {CacheRefreshStatusMessage}");
        }
        catch (Exception ex)
        {
            CacheRefreshStatusMessage = $"Unable to request a cache refresh: {ex.Message}";
            AppLog.Error("SettingsCacheRefresh", ex, "RequestCacheRefreshAsync failed.");
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
        OnPropertyChanged(nameof(IsImagesPanelVisible));
        OnPropertyChanged(nameof(IsPictureAssignmentsVisible));
        OnPropertyChanged(nameof(IsAppearanceCategoryVisible));
        OnPropertyChanged(nameof(IsOperationsCategoryVisible));
        OnPropertyChanged(nameof(IsAboutCategoryVisible));
        OnPropertyChanged(nameof(IsAdministrationCategoryVisible));

        // The hub's cards are computed from the same answers, so they announce with them.
        OnPropertyChanged(nameof(IsGeneralSettingsPageAvailable));
        OnPropertyChanged(nameof(IsOperationsSettingsPageAvailable));
        OnPropertyChanged(nameof(IsPicturesSettingsPageAvailable));
        OnPropertyChanged(nameof(IsDiagnosticsSettingsPageAvailable));
        OnPropertyChanged(nameof(IsAdministrationSettingsPageAvailable));
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
