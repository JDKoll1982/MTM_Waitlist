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

            CanReadIgnoredLocations = answers[PermissionKeys.SettingsIgnoredLocations];
            CanManageIgnoredLocations = answers[PermissionKeys.SettingsIgnoredLocationsEdit];
            CanManageSessionLength = answers[PermissionKeys.SettingsSessionLength];
            CanOpenDeveloperLogPanel = answers[PermissionKeys.SettingsLogPanel];
            CanManageHotWorkCenters = answers[PermissionKeys.SettingsHotWorkCenters];
            CanManageBuildings = answers[PermissionKeys.SettingsBuildings];
            CanManageImageLocationSettings = answers[PermissionKeys.SettingsPartPictures];
            CanManageStoragePaths = answers[PermissionKeys.SettingsStoragePaths];
            CanRequestCacheRefresh = answers[PermissionKeys.SettingsCacheRefresh];
            CanOpenUserManagement = answers[PermissionKeys.AdminUsers];
            CanOpenPermissions = answers[PermissionKeys.AdminPermissions];
            CanOpenLuggerArrangement = answers[PermissionKeys.SettingsLuggerArrangement];

            UrgencyAllotments.ApplyPermission(answers[PermissionKeys.SettingsUrgencyMinutes]);
            ComputerManagement.ApplyPermission(answers[PermissionKeys.SettingsComputers]);

            RefreshSearchVisibility();
        }
        catch (Exception ex)
        {
            AppLog.Error(
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
        AppLog.Info("SettingsViewModel", $"Theme changed to '{value}'.");
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
            AppLog.Info("SettingsViewModel", "NewRequestAlertsEnabled was asked for on an installation that cannot deliver a notification; nothing was stored.");
            return;
        }

        AppLog.Info("SettingsViewModel", $"NewRequestAlertsEnabled changed to {value}.");
        _ = _newRequestAlertService.SetEnabledAsync(value);
        RefreshSearchVisibility();
    }

    partial void OnSearchQueryChanged(string value)
    {
        AppLog.Info("SettingsViewModel", $"SearchQuery changed to '{value}'.");
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

        AppLog.Info("SettingsViewModel", $"EnlargePicturesOnClick changed to {value}.");
        _ = _pictureEnlargePreference?.SetEnabledAsync(value);
    }

    partial void OnSelectedWorkstationChanged(string value)
    {
        AppLog.Info("SettingsViewModel", $"SelectedWorkstation changed to '{value}'.");
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _catalogLoad = LoadCatalogForWorkstationAsync(value);
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
        AppLog.Info("SettingsHotWorkCenters", $"AddHotWorkCenterAsync started. WorkCenter='{normalizedWorkCenter}', Workstation='{SelectedWorkstation}'.");
        if (HotWorkCenters.Any(value => string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase)))
        {
            AppLog.Info("SettingsHotWorkCenters", $"AddHotWorkCenterAsync skipped because '{normalizedWorkCenter}' is already pinned.");
            return;
        }

        var existingOthers = _allOtherWorkCenters
            .Where(value => !string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        HotWorkCenters.Add(normalizedWorkCenter);
        SortCollection(HotWorkCenters);
        _allOtherWorkCenters = existingOthers;
        ApplyWorkCenterBuildingFilter();

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
        AppLog.Info("SettingsHotWorkCenters", $"RemoveHotWorkCenterAsync started. WorkCenter='{normalizedWorkCenter}', Workstation='{SelectedWorkstation}'.");
        var updatedHot = HotWorkCenters
            .Where(value => !string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        ReplaceCollectionValues(HotWorkCenters, updatedHot);

        if (!_allOtherWorkCenters.Any(value => string.Equals(value, normalizedWorkCenter, StringComparison.OrdinalIgnoreCase)))
        {
            _allOtherWorkCenters = _allOtherWorkCenters.Append(normalizedWorkCenter).ToArray();
        }

        ApplyWorkCenterBuildingFilter();

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

        AppLog.Info("SettingsDunnageVisibility", $"ShowDunnageTypeAsync started. DunnageTypeId='{normalizedId}'.");

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

        AppLog.Info("SettingsDunnageVisibility", $"HideDunnageTypeAsync started. DunnageTypeId='{normalizedId}'.");

        var nextVisible = VisibleDunnageTypes.Where(item => !string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase)).ToArray();
        var nextHidden = HiddenDunnageTypes.Concat(new[] { selectedOption }).ToArray();

        ReplaceDunnageTypeValues(VisibleDunnageTypes, nextVisible);
        ReplaceDunnageTypeValues(HiddenDunnageTypes, nextHidden);
        RefreshSearchVisibility();

        await SaveCurrentDunnageTypeVisibilityAsync().ConfigureAwait(true);
    }

    private async Task InitializeHotWorkCentersAsync()
    {
        AppLog.Info("SettingsViewModel", "InitializeHotWorkCentersAsync started.");
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
            if (string.IsNullOrWhiteSpace(SelectedWorkstation) || workstationChanged)
            {
                // The property change is what starts the catalog read, so the wait is on that read rather than on a
                // second one started here.
                SelectedWorkstation = resolvedWorkstation;
                await _catalogLoad.ConfigureAwait(true);
            }
            else
            {
                await LoadCatalogForWorkstationAsync(SelectedWorkstation).ConfigureAwait(true);
            }

            RefreshSearchVisibility();

            AppLog.Info("SettingsViewModel", $"InitializeHotWorkCentersAsync completed. Workstation='{SelectedWorkstation}', AvailableCount={AvailableWorkstations.Count}.");
        }
        finally
        {
            IsHotWorkCentersBusy = false;
        }
    }

    private async Task LoadCatalogForWorkstationAsync(string workstationName)
    {
        AppLog.Info("SettingsViewModel", $"LoadCatalogForWorkstationAsync started. Workstation='{workstationName}'.");
        IsHotWorkCentersBusy = true;
        try
        {
            var catalog = await _workCenterCatalogService.GetCatalogAsync(workstationName).ConfigureAwait(true);
            ReplaceCollectionValues(HotWorkCenters, catalog.HotWorkCenters);

            // The candidates are held before the building filter is applied, so switching the filter narrows what
            // was read rather than reading the catalog again.
            _allOtherWorkCenters = catalog.OtherWorkCenters.ToArray();
            _workCenterBuildings = catalog.WorkCenterDetails
                .Where(detail => !string.IsNullOrWhiteSpace(detail.Value.Building))
                .ToDictionary(
                    detail => detail.Key,
                    detail => detail.Value.Building,
                    StringComparer.OrdinalIgnoreCase);

            await LoadWorkstationBuildingAsync(workstationName).ConfigureAwait(true);
            ApplyWorkCenterBuildingFilter();
            HotWorkCentersStatusMessage = string.Empty;
            RefreshSearchVisibility();
            AppLog.Info("SettingsViewModel", $"LoadCatalogForWorkstationAsync completed. Workstation='{workstationName}', HotCount={HotWorkCenters.Count}, OtherCount={OtherWorkCenters.Count}, Building='{SelectedWorkstationBuilding}'.");
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsViewModel", ex, $"LoadCatalogForWorkstationAsync failed. Workstation='{workstationName}'.");
            HotWorkCentersStatusMessage = $"Unable to load work centers: {ex.Message}";
        }
        finally
        {
            IsHotWorkCentersBusy = false;
        }
    }

    /// <summary>
    /// Reads the building the selected computer was set up in, which is what the filter's "Saved Building" mode
    /// narrows to and what the toggle states.
    /// </summary>
    private async Task LoadWorkstationBuildingAsync(string workstationName)
    {
        if (_buildingCatalogService is null)
        {
            return;
        }

        try
        {
            SelectedWorkstationBuilding = await _buildingCatalogService
                .GetComputerBuildingAsync(workstationName)
                .ConfigureAwait(true) ?? string.Empty;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "SettingsHotWorkCenters",
                ex,
                $"The saved building for workstation '{workstationName}' could not be read, so the Local Work Centers filter falls back to every building.");
            SelectedWorkstationBuilding = string.Empty;
        }

        OnPropertyChanged(nameof(BuildingFilterToggleLabel));

        if (IsBuildingFilterEnabled)
        {
            SelectedWorkCenterBuilding = SelectedWorkstationBuilding;
        }
    }

    /// <summary>
    /// Narrows the candidate work centres to the building the filter asks for: the selected computer's saved
    /// building while the filter is on, otherwise the building chosen beside it, or every building when that choice
    /// is <see cref="BuildingScope.AllBuildings"/>.
    /// </summary>
    /// <remarks>
    /// A work centre the catalog holds no building for is always offered: hiding a work centre this screen cannot
    /// classify would be a guess, and a person could not explain why it had disappeared.
    /// </remarks>
    private void ApplyWorkCenterBuildingFilter()
    {
        var buildingFilter = BuildingScope.AsFilter(
            IsBuildingFilterEnabled ? SelectedWorkstationBuilding : SelectedWorkCenterBuilding);

        var visible = _allOtherWorkCenters.Where(name =>
            buildingFilter is null
            || !_workCenterBuildings.TryGetValue(name, out var building)
            || string.Equals(building, buildingFilter, StringComparison.OrdinalIgnoreCase));

        ReplaceCollectionValues(OtherWorkCenters, visible);
        RefreshSearchVisibility();
    }

    /// <summary>
    /// Turns the Local Work Centers filter on or off. Turning it on follows the selected computer's saved building;
    /// turning it off leaves the building choice beside it, which starts on every building.
    /// </summary>
    partial void OnIsBuildingFilterEnabledChanged(bool value)
    {
        SelectedWorkCenterBuilding = value && !string.IsNullOrWhiteSpace(SelectedWorkstationBuilding)
            ? SelectedWorkstationBuilding
            : BuildingScope.AllBuildings;

        OnPropertyChanged(nameof(BuildingFilterToggleLabel));
        OnPropertyChanged(nameof(IsBuildingChoiceEnabled));
        ApplyWorkCenterBuildingFilter();
    }

    partial void OnSelectedWorkCenterBuildingChanged(string value) => ApplyWorkCenterBuildingFilter();

    partial void OnSelectedWorkstationBuildingChanged(string value) =>
        OnPropertyChanged(nameof(BuildingFilterToggleLabel));

    /// <summary>Clears the Buildings panel so the next save adds a building rather than renaming one.</summary>
    [RelayCommand]
    private void BeginNewBuilding()
    {
        SelectedBuildingForEdit = null;
        BuildingNameInput = string.Empty;
        BuildingsStatusMessage = string.Empty;
    }

    /// <summary>Loads one building into the panel so it can be renamed.</summary>
    /// <param name="building">The building to edit. A blank one changes nothing.</param>
    [RelayCommand]
    private void EditBuilding(string? building)
    {
        if (string.IsNullOrWhiteSpace(building))
        {
            return;
        }

        SelectedBuildingForEdit = building.Trim();
        BuildingsStatusMessage = string.Empty;
    }

    /// <summary>What the panel is about to do, so Save is never pressed without knowing whether it adds or renames.</summary>
    public string BuildingEditHeading => string.IsNullOrWhiteSpace(SelectedBuildingForEdit)
        ? "Adding a new building."
        : $"Renaming '{SelectedBuildingForEdit}'.";

    /// <summary>Choosing a building to rename fills the name box with the name it already has.</summary>
    partial void OnSelectedBuildingForEditChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            BuildingNameInput = value;
        }

        OnPropertyChanged(nameof(BuildingEditHeading));
    }

    /// <summary>
    /// Adds the building named in the panel, or renames the one chosen there.
    /// </summary>
    /// <remarks>
    /// The write goes through <see cref="IBuildingCatalogService"/>, whose procedure also rewrites the work centres
    /// and the computers that name the old spelling, so a rename cannot leave anything naming a building the
    /// catalog no longer holds. The selection service is asked to read again afterwards, which is what carries a
    /// new or renamed building into the shell's chooser without restarting the session.
    /// </remarks>
    [RelayCommand]
    private async Task SaveBuildingAsync()
    {
        if (!CanManageBuildings)
        {
            BuildingsStatusMessage = "You do not have permission to change the building list.";
            return;
        }

        if (_buildingCatalogService is null || IsBuildingsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(BuildingNameInput))
        {
            BuildingsStatusMessage = "A building name is required.";
            return;
        }

        IsBuildingsBusy = true;
        try
        {
            var result = await _buildingCatalogService
                .SaveBuildingAsync(SelectedBuildingForEdit, BuildingNameInput)
                .ConfigureAwait(true);

            BuildingsStatusMessage = result.Message;

            if (!result.Succeeded)
            {
                return;
            }

            SelectedBuildingForEdit = null;
            BuildingNameInput = string.Empty;
            await LoadBuildingsAsync().ConfigureAwait(true);

            if (_buildingSelectionService is not null)
            {
                await _buildingSelectionService.RefreshAsync().ConfigureAwait(true);
            }

            AppLog.Info("SettingsBuildings", $"Building saved. Message='{BuildingsStatusMessage}'.");
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsBuildings", ex, "SaveBuildingAsync failed.");
            BuildingsStatusMessage = $"Unable to save the building: {ex.Message}";
        }
        finally
        {
            IsBuildingsBusy = false;
        }
    }

    /// <summary>Reads the plant's buildings for both the Buildings panel and the Local Work Centers filter.</summary>
    private async Task InitializeBuildingsAsync()
    {
        IsBuildingsBusy = true;
        try
        {
            await LoadBuildingsAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsBuildings", ex, "The plant's building list could not be read.");
            BuildingsStatusMessage = $"Unable to load the buildings: {ex.Message}";
        }
        finally
        {
            IsBuildingsBusy = false;
        }
    }

    /// <summary>
    /// Fills the Buildings panel and the filter's building choice from the catalog, in the catalog's own order, with
    /// <see cref="BuildingScope.AllBuildings"/> last in the choice.
    /// </summary>
    private async Task LoadBuildingsAsync()
    {
        if (_buildingCatalogService is null)
        {
            return;
        }

        var buildings = await _buildingCatalogService.GetBuildingsAsync().ConfigureAwait(true);
        var active = buildings
            .Where(building => building.IsActive && !string.IsNullOrWhiteSpace(building.BuildingName))
            .Select(building => building.BuildingName)
            .ToArray();

        EditableBuildings.Clear();
        foreach (var building in active)
        {
            EditableBuildings.Add(building);
        }

        BuildingChoices.Clear();
        foreach (var building in active)
        {
            BuildingChoices.Add(building);
        }

        BuildingChoices.Add(BuildingScope.AllBuildings);

        // A building that was renamed or withdrawn is no longer a choice, so the filter falls back to every
        // building rather than to a name the list does not hold.
        if (!BuildingChoices.Contains(SelectedWorkCenterBuilding, StringComparer.OrdinalIgnoreCase))
        {
            SelectedWorkCenterBuilding = BuildingScope.AllBuildings;
        }

        RefreshSearchVisibility();
    }

    private async Task SaveCurrentHotWorkCentersAsync()
    {
        try
        {
            AppLog.Info("SettingsHotWorkCenters", $"SaveCurrentHotWorkCentersAsync started. Workstation='{SelectedWorkstation}', Count={HotWorkCenters.Count}.");
            var saveMessage = await _workCenterCatalogService
                .SaveHotWorkCentersAsync(SelectedWorkstation, HotWorkCenters.ToArray())
                .ConfigureAwait(true);

            HotWorkCentersStatusMessage = string.IsNullOrWhiteSpace(saveMessage)
                ? "Local workcenters saved."
                : saveMessage;

            AppLog.Info("SettingsHotWorkCenters", $"SaveCurrentHotWorkCentersAsync completed. Workstation='{SelectedWorkstation}', Message='{HotWorkCentersStatusMessage}'.");
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsHotWorkCenters", ex, $"SaveCurrentHotWorkCentersAsync failed. Workstation='{SelectedWorkstation}', Count={HotWorkCenters.Count}.");
            HotWorkCentersStatusMessage = $"Unable to save Local workcenters: {ex.Message}";
        }
    }

    private async Task InitializeDunnageTypeVisibilityAsync()
    {
        AppLog.Info("SettingsDunnageVisibility", "InitializeDunnageTypeVisibilityAsync started.");
        IsDunnageTypeVisibilityBusy = true;
        try
        {
            var catalog = await _dunnageTypeVisibilityCatalogService.GetCatalogAsync().ConfigureAwait(true);
            ReplaceDunnageTypeValues(VisibleDunnageTypes, catalog.VisibleDunnageTypes);
            ReplaceDunnageTypeValues(HiddenDunnageTypes, catalog.HiddenDunnageTypes);
            DunnageTypeVisibilityStatusMessage = string.Empty;
            RefreshSearchVisibility();
            AppLog.Info("SettingsDunnageVisibility", $"InitializeDunnageTypeVisibilityAsync completed. VisibleCount={VisibleDunnageTypes.Count}, HiddenCount={HiddenDunnageTypes.Count}.");
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsDunnageVisibility", ex, "InitializeDunnageTypeVisibilityAsync failed.");
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

            AppLog.Info("SettingsDunnageVisibility", $"SaveCurrentDunnageTypeVisibilityAsync completed. VisibleCount={VisibleDunnageTypes.Count}, HiddenCount={HiddenDunnageTypes.Count}, Message='{DunnageTypeVisibilityStatusMessage}'.");
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsDunnageVisibility", ex, "SaveCurrentDunnageTypeVisibilityAsync failed.");
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

    /// <summary>
    /// Seeds the editor from the plant-wide list the service reads, so this screen and every filter that hides a
    /// location agree by construction rather than by two reads of one key happening to match.
    /// </summary>
    /// <remarks>
    /// The duplicate read and write this screen used to keep are gone with the local settings mechanism (T146).
    /// The service is the one reader of the stored list, and the one writer of it.
    /// </remarks>
    private async Task InitializeIgnoredLocationsAsync()
    {
        try
        {
            var stored = await _ignoredLocationsService.GetIgnoredLocationsAsync().ConfigureAwait(true);
            ReplaceCollectionValues(IgnoredLocations, stored);
            IgnoredLocationsStatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsIgnoredLocations", ex, "The ignored-locations list could not be read; the shipped defaults are shown.");
            ReplaceCollectionValues(IgnoredLocations, IgnoredLocationDefaults.Locations);
        }

        RefreshSearchVisibility();
    }

    [RelayCommand]
    private async Task AddIgnoredLocationAsync()
    {
        if (!CanManageIgnoredLocations)
        {
            // Refused where the write would happen rather than only where the control is drawn, and refused with a
            // reason the reader can act on (FR-024, FR-029).
            IgnoredLocationsStatusMessage = "Settings_IgnoredLocations_EditRefused.Text".GetLocalized();
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

        AppLog.Info("SettingsIgnoredLocations", $"AddIgnoredLocationAsync adding '{code}'.");
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
            IgnoredLocationsStatusMessage = "Settings_IgnoredLocations_EditRefused.Text".GetLocalized();
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
        AppLog.Info("SettingsIgnoredLocations", $"RemoveIgnoredLocationAsync removed '{code}'. Remaining={IgnoredLocations.Count}.");
        await SaveIgnoredLocationsAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Stores the plant-wide list through the service, which is where the edit entitlement is enforced.
    /// </summary>
    /// <remarks>
    /// A refusal is a permission answer rather than a fault, so it is stated in words and is not thrown. A fault is
    /// still reported, because a list that did not save is something the reader needs to know about.
    /// </remarks>
    private async Task SaveIgnoredLocationsAsync()
    {
        try
        {
            var saved = await _ignoredLocationsService
                .SaveIgnoredLocationsAsync(IgnoredLocations.ToList())
                .ConfigureAwait(true);

            if (!saved)
            {
                AppLog.Info("SettingsIgnoredLocations", "The list was not saved because this person may not change the plant-wide list.");
                IgnoredLocationsStatusMessage = "Settings_IgnoredLocations_EditRefused.Text".GetLocalized();
                return;
            }

            IgnoredLocationsStatusMessage = string.Empty;
            AppLog.Info("SettingsIgnoredLocations", $"SaveIgnoredLocationsAsync saved {IgnoredLocations.Count} location(s).");
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsIgnoredLocations", ex, "SaveIgnoredLocationsAsync failed.");
            IgnoredLocationsStatusMessage = $"Unable to save ignored locations: {ex.Message}";
        }
    }

    /// <summary>
    /// Reads the session length the launch will apply, so the panel opens on the value in force rather than on the
    /// shipped default (FR-029).
    /// </summary>
    private async Task InitializeSessionLengthAsync()
    {
        if (!CanManageSessionLength)
        {
            return;
        }

        try
        {
            var stored = await _preferences
                .ReadNumberAsync(ScopedPreferenceKeys.SessionLengthMinutes, PreferenceScope.Plant)
                .ConfigureAwait(true);

            SessionLengthMinutes = stored is > 0 ? stored.Value : ScopedPreferenceKeys.DefaultSessionLengthMinutes;
            SessionLengthStatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsSessionLength", ex, "The session length could not be read; the shipped default is shown.");
            SessionLengthMinutes = ScopedPreferenceKeys.DefaultSessionLengthMinutes;
        }

        RefreshSearchVisibility();
    }

    /// <summary>
    /// Stores the session length for the whole plant, and refuses an unauthorised change where the write happens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The gate is read here rather than inferred from whether the control was drawn, so a caller that reaches the
    /// command without going through the panel is still refused (FR-029).
    /// </para>
    /// <para>
    /// The value is clamped to the declared floor and ceiling before it is stored. A length below the floor would
    /// sign everybody out mid-shift, and the store refuses a non-positive length as well, so neither half of the
    /// pair is the only guard.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task SaveSessionLengthAsync()
    {
        if (!CanManageSessionLength)
        {
            SessionLengthStatusMessage = "Settings_SessionLength_EditRefused.Text".GetLocalized();
            return;
        }

        var requested = (int)Math.Round(SessionLengthMinutes, MidpointRounding.AwayFromZero);
        var clamped = Math.Clamp(
            requested,
            ScopedPreferenceKeys.MinimumSessionLengthMinutes,
            ScopedPreferenceKeys.MaximumSessionLengthMinutes);

        try
        {
            await _preferences
                .WriteNumberAsync(ScopedPreferenceKeys.SessionLengthMinutes, PreferenceScope.Plant, clamped)
                .ConfigureAwait(true);

            SessionLengthMinutes = clamped;
            SessionLengthStatusMessage = "Settings_SessionLength_Saved.Text".GetLocalized();
            AppLog.Info("SettingsSessionLength", $"The session length was stored as {clamped} minute(s).");
        }
        catch (Exception ex)
        {
            AppLog.Error("SettingsSessionLength", ex, "The session length could not be stored.");
            SessionLengthStatusMessage = "Settings_SessionLength_SaveFailed.Text".GetLocalized();
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
            AppLog.Error(
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
            AppLog.Error("SettingsPictureCache", ex, "The picture cache toggle could not be stored.");
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
            AppLog.Error("SettingsPictureCache", ex, "The picture cache folder could not be stored.");
        }
    }

    /// <summary>
    /// Reads the level the log store keeps entries at, so the section opens on the value in force rather than on
    /// the shipped default (FR-032).
    /// </summary>
    private async Task InitializeLogStoreLevelAsync()
    {
        if (_logStoreLevelService is null)
        {
            return;
        }

        try
        {
            var severity = await _logStoreLevelService.ReadStoredLevelAsync().ConfigureAwait(true);

            LogStoreMinimumLevel = severity.ToString();
            LogStoreMinimumLevelInput = severity.ToString();
            LogStoreStatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            // The shipped default is what the store is already doing in this case, so the box is seeded from it
            // rather than left empty, and the failure is recorded rather than swallowed.
            LogStoreMinimumLevel = LogStoreLevel.ShippedMinimum.ToString();
            LogStoreMinimumLevelInput = LogStoreLevel.ShippedMinimum.ToString();
            AppLog.Error("SettingsLogStore", ex, "The log store's level could not be read; the shipped default is shown.");
        }

        RefreshSearchVisibility();
    }

    /// <summary>
    /// Stores how much the log store keeps, for every computer, and applies it to the store straight away.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The gate is read here rather than inferred from whether the control was drawn, so a caller that reaches the
    /// command without going through the panel is refused, and refused in the words the hidden control implies
    /// (FR-054, FR-117).
    /// </para>
    /// <para>
    /// <b>The change needs no restart.</b> The logging module holds one threshold and both doors onto the store
    /// read it, so adopting the new value is what puts it in force for the next entry raised — on this computer
    /// at once, and on every other computer the next time it reads the setting (FR-032).
    /// </para>
    /// <para>
    /// A name this cannot parse is refused rather than quietly stored as the default, because a silent change to
    /// what is recorded is the one outcome a reader could never detect.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task SaveLogStoreLevelAsync()
    {
        if (!CanOpenDeveloperLogPanel || _logStoreLevelService is null)
        {
            LogStoreStatusMessage = "Settings_LogStoreLevel_EditRefused.Text".GetLocalized();
            return;
        }

        if (IsLogStoreBusy)
        {
            return;
        }

        if (Enum.TryParse<LogSeverity>(LogStoreMinimumLevelInput?.Trim(), ignoreCase: true, out var severity) is false
            || Enum.IsDefined(severity) is false)
        {
            LogStoreStatusMessage = "Settings_LogStoreLevel_Invalid.Text".GetLocalized();
            return;
        }

        IsLogStoreBusy = true;

        try
        {
            await _logStoreLevelService
                .SaveLevelAsync(severity, _personIdentity.UserId > 0 ? _personIdentity.UserId : null)
                .ConfigureAwait(true);

            LogStoreMinimumLevel = severity.ToString();
            LogStoreMinimumLevelInput = severity.ToString();
            LogStoreStatusMessage = "Settings_LogStoreLevel_Saved.Text".GetLocalized();

            AppLog.Info("SettingsLogStore", $"The log store's level was stored as '{severity}' for every computer.");
        }
        catch (Exception ex)
        {
            LogStoreStatusMessage = "Settings_LogStoreLevel_SaveFailed.Text".GetLocalized();
            AppLog.Error("SettingsLogStore", ex, "The log store's level could not be stored.");
        }
        finally
        {
            IsLogStoreBusy = false;
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

            PictureCacheStatusMessage = result.DestinationsUnavailable.Count > 0
                ? $"Copied {result.Copied}, removed {result.Removed}. Nothing was done for {string.Join(", ", result.DestinationsUnavailable)}, because the folder the copies go into could not be written to."
                : result.SourcesSkipped.Count > 0
                    ? $"Copied {result.Copied}, removed {result.Removed}. Nothing was done for {string.Join(", ", result.SourcesSkipped)}, which could not be reached."
                    : result.NotCopied > 0
                        ? $"Copied {result.Copied}, removed {result.Removed}. {result.NotCopied} picture(s) could not be copied."
                        : $"Copied {result.Copied}, removed {result.Removed}.";
        }
        catch (Exception ex)
        {
            PictureCacheStatusMessage = "Unable to copy the pictures.";
            AppLog.Error("SettingsPictureCache", ex, "The picture copy requested from the Settings screen failed.");
        }
        finally
        {
            IsPictureCacheBusy = false;
        }
    }

    /// <summary>
    /// Opens the folder dialog at the hopper picture folder and puts the folder the person chose in the box.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The dialog starts at what the box already holds, and at the shipped share folder when the box is empty, so
    /// picking a sub-folder of the artwork starts where the artwork is rather than wherever the last dialog was
    /// left.
    /// </para>
    /// <para>
    /// A closed dialog, a dialog that could not be shown and a host with no dialog all leave the box exactly as it
    /// was: choosing nothing is an ordinary answer, and a typed path must not be lost because a mapped drive was
    /// not available. Nothing is stored here — the Save button beside the box still decides that (FR-078).
    /// </para>
    /// </remarks>
    [RelayCommand]
    private Task BrowseHopperFolderAsync() => BrowseIntoAsync(
        HopperFolderInput,
        chosen => HopperFolderInput = chosen,
        ConfigSettingKeys.ImageStorageHopperFolderPathDefault);

    /// <summary>Opens the folder dialog at the lugger picture folder and puts the chosen folder in the box.</summary>
    [RelayCommand]
    private Task BrowseLuggerFolderAsync() => BrowseIntoAsync(
        LuggerFolderInput,
        chosen => LuggerFolderInput = chosen,
        ConfigSettingKeys.ImageStorageLuggerFolderPathDefault);

    /// <summary>Opens the folder dialog at the picture folder and puts the chosen folder in the box.</summary>
    [RelayCommand]
    private Task BrowsePictureFolderAsync() => BrowseIntoAsync(
        PictureFolderInput,
        chosen => PictureFolderInput = chosen);

    /// <summary>Opens the folder dialog at the dunnage picture root and puts the chosen folder in the box.</summary>
    [RelayCommand]
    private Task BrowseDunnageRootAsync() => BrowseIntoAsync(
        DunnageRootInput,
        chosen => DunnageRootInput = chosen);

    /// <summary>Opens the folder dialog at the cache folder and puts the chosen folder in the box.</summary>
    [RelayCommand]
    private Task BrowsePictureCacheFolderAsync() => BrowseIntoAsync(
        PictureCacheFolderInput,
        chosen => PictureCacheFolderInput = chosen);

    /// <summary>
    /// Asks for a folder, starting where the box points, and hands the answer to the caller.
    /// </summary>
    /// <remarks>
    /// One implementation for every box in the Pictures panel, so all four behave the same way: nothing chosen
    /// leaves the box alone, and nothing is stored — the Save beside the box is what stores a path.
    /// </remarks>
    private async Task BrowseIntoAsync(string? current, Action<string> apply, string? fallback = null)
    {
        if (_folderBrowserService is null)
        {
            return;
        }

        var startingFolder = string.IsNullOrWhiteSpace(current) ? fallback : current.Trim();

        var chosen = await _folderBrowserService
            .PickFolderAsync(startingFolder, CancellationToken.None)
            .ConfigureAwait(true);

        if (!string.IsNullOrWhiteSpace(chosen))
        {
            apply(chosen);
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

            DunnageRootInput = await _imageStorageConfigurationResolver
                .GetDunnageRootPathAsync()
                .ConfigureAwait(true);

            HopperFolderInput = await _imageStorageConfigurationResolver
                .GetHopperFolderPathAsync()
                .ConfigureAwait(true);

            LuggerFolderInput = await _imageStorageConfigurationResolver
                .GetLuggerFolderPathAsync()
                .ConfigureAwait(true);

            NoImagePathInput = await _imageStorageConfigurationResolver
                .GetNoImagePathAsync()
                .ConfigureAwait(true);

            // Adopted here as well as at startup: a host whose launch steps did not run the picture cache still gets
            // the plant's artwork as soon as anyone opens this screen, rather than the shipped one for the rest of
            // the run.
            ImagePicturePolicy.SetNoImageOverride(NoImagePathInput);

            ArchiveKeepDaysInput = await _imageStorageConfigurationResolver
                .GetArchiveKeepDaysAsync()
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StoragePathsStatusMessage = "Settings_StoragePaths_Unavailable.Text".GetLocalized();
            AppLog.Error(
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
    /// Stores the picture folder, the dunnage root and the retention period for every computer.
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
        var dunnageRoot = DunnageRootInput?.Trim() ?? string.Empty;
        var hopperFolder = HopperFolderInput?.Trim() ?? string.Empty;
        var luggerFolder = LuggerFolderInput?.Trim() ?? string.Empty;
        var noImagePath = NoImagePathInput?.Trim() ?? string.Empty;
        var keepDays = (int)Math.Round(ArchiveKeepDaysInput, MidpointRounding.AwayFromZero);

        if (pictureFolder.Length == 0 || dunnageRoot.Length == 0)
        {
            StoragePathsStatusMessage = "Settings_StoragePaths_FolderNeeded.Text".GetLocalized();
            return;
        }

        if (hopperFolder.Length == 0)
        {
            // An empty hopper folder is refused rather than stored: it would leave the three sizes pointing at the
            // picture root itself, and the card would then never find a hopper picture at all (FR-078).
            StoragePathsStatusMessage = "Settings_StoragePaths_HopperFolderNeeded.Text".GetLocalized();
            return;
        }

        if (keepDays < 1)
        {
            StoragePathsStatusMessage = "Settings_StoragePaths_DaysNeeded.Text".GetLocalized();
            return;
        }

        if (luggerFolder.Length == 0)
        {
            // Refused for the hopper folder's reason: an empty value is not a folder, and storing it would leave the
            // lugger cards reading a path that resolves to nothing at all.
            StoragePathsStatusMessage = "Settings_StoragePaths_LuggerFolderNeeded.Text".GetLocalized();
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
                ConfigSettingKeys.DunnageRootPath,
                "text",
                dunnageRoot,
                boolean: null).ConfigureAwait(true);

            await SaveAppWideSettingAsync(
                ConfigSettingKeys.ImageStorageHopperFolderPath,
                "text",
                hopperFolder,
                boolean: null).ConfigureAwait(true);

            await SaveAppWideSettingAsync(
                ConfigSettingKeys.ImageStorageLuggerFolderPath,
                "text",
                luggerFolder,
                boolean: null).ConfigureAwait(true);

            // Empty is a legitimate answer — it means the artwork that ships with the application — so it is stored
            // as the shipped path rather than as a blank the resolver would have to interpret.
            await SaveAppWideSettingAsync(
                ConfigSettingKeys.ImageStorageNoImagePath,
                "text",
                noImagePath.Length == 0 ? ConfigSettingKeys.ImageStorageNoImagePathDefault : noImagePath,
                boolean: null).ConfigureAwait(true);

            await SaveAppWideSettingAsync(
                ConfigSettingKeys.ImageStorageArchiveKeepDays,
                "int",
                text: null,
                boolean: null,
                integer: keepDays).ConfigureAwait(true);

            PictureFolderPath = pictureFolder;
            NoImagePathInput = noImagePath.Length == 0
                ? ConfigSettingKeys.ImageStorageNoImagePathDefault
                : noImagePath;

            // Adopted at once rather than at the next start, so the change the person just made is the change they
            // see when they walk to the next screen.
            ImagePicturePolicy.SetNoImageOverride(NoImagePathInput);

            StoragePathsStatusMessage = noImagePath.Length == 0
                || ImagePicturePolicy.ResolveExistingFile(noImagePath) is not null
                ? "Settings_StoragePaths_Saved.Text".GetLocalized()
                : "Settings_StoragePaths_NoImageMissing.Text".GetLocalized();
        }
        catch (Exception ex)
        {
            StoragePathsStatusMessage = "Settings_StoragePaths_SaveFailed.Text".GetLocalized();
            AppLog.Error("SettingsStoragePaths", ex, "A storage setting could not be stored.");
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
