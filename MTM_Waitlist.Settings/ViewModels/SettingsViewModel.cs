using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using Windows.ApplicationModel;

namespace MTM_Waitlist.Module_Settings.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private static readonly string[] AllowedIgnoredLocationManageRoles =
    {
        "Admin",
        "Developer",
        "Plant Manager",
        "Production",
        "Production Lead",
        "Setup",
        "Setup Lead",
    };

    private static readonly string[] AllowedHotWorkCenterManageRoles =
    {
        "Admin",
        "Developer",
        "Plant Manager",
        "Setup Lead",
        "Production Lead",
    };

    private static readonly string[] AllowedImageLocationManageRoles =
    {
        "Admin",
        "Developer",
    };

    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IWorkCenterCatalogService _workCenterCatalogService;
    private readonly IDunnageTypeVisibilityCatalogService _dunnageTypeVisibilityCatalogService;
    private readonly INewRequestAlertService _newRequestAlertService;
    private readonly StartupState _startupState;

    // Suppresses the OnNewRequestAlertsEnabledChanged side effect while the initial value is loaded in the
    // constructor, so opening the page does not log a misleading "changed" or re-persist.
    private bool _newRequestAlertInitializing = true;

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

    public ObservableCollection<ComputerOption> AvailableWorkstations { get; } = new();

    public ObservableCollection<string> HotWorkCenters { get; } = new();

    public ObservableCollection<string> OtherWorkCenters { get; } = new();

    public ObservableCollection<DunnageTypeVisibilityOption> VisibleDunnageTypes { get; } = new();

    public ObservableCollection<DunnageTypeVisibilityOption> HiddenDunnageTypes { get; } = new();

    public ObservableCollection<string> IgnoredLocations { get; } = new();

    public bool CanManageHotWorkCenters => AllowedHotWorkCenterManageRoles.Any(role =>
        string.Equals(role, _startupState.CurrentRole, StringComparison.OrdinalIgnoreCase));

    public bool CanManageImageLocationSettings => AllowedImageLocationManageRoles.Any(role =>
        string.Equals(role, _startupState.CurrentRole, StringComparison.OrdinalIgnoreCase));

    public bool CanManageDunnageTypeVisibility => CanManageHotWorkCenters;

    public bool CanManageIgnoredLocations => AllowedIgnoredLocationManageRoles.Any(role =>
        string.Equals(role, _startupState.CurrentRole, StringComparison.OrdinalIgnoreCase));

    public bool IsAppearancePanelVisible => MatchesSearch("appearance", "app theme", "light", "dark", "default", SelectedThemeText);

    public bool IsNewRequestAlertsPanelVisible => MatchesSearch(
        "alert",
        "notification",
        "notify",
        "new request",
        "waitlist");

    public bool IsUrgencyAllotmentsPanelVisible => MatchesSearch(
        "urgency",
        "max allotted",
        "allotted",
        "deadline",
        "overdue",
        "remaining time");

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

    public bool IsAppearanceCategoryVisible => IsAppearancePanelVisible;

    public bool IsImageLocationSettingsPanelVisible => CanManageImageLocationSettings && MatchesSearch(
        "image location",
        "image settings",
        "request type images",
        "work center images",
        "subtype images",
        "image path",
        "request type",
        "subtype",
        "work center");

    public bool IsComputersPanelVisible => ComputerManagement.CanManageComputers && MatchesSearch(
        "computer",
        "computers",
        "registry",
        "mac",
        "display name",
        string.Join(" ", ComputerManagement.Computers.Select(record => record.GetDisplayLabel())));

    public bool IsOperationsCategoryVisible => IsHotWorkCentersPanelVisible || IsDunnageTypeVisibilityPanelVisible || IsImageLocationSettingsPanelVisible || IsComputersPanelVisible || IsIgnoredLocationsPanelVisible || IsNewRequestAlertsPanelVisible || IsUrgencyAllotmentsPanelVisible;

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
        StartupState startupState,
        ComputerManagementViewModel computerManagement,
        UrgencyAllotmentEditorViewModel urgencyAllotments)
    {
        StartupDebugLog.Info("SettingsViewModel", "Constructor started.");
        _themeSelectorService = themeSelectorService;
        _localSettingsService = localSettingsService;
        _workCenterCatalogService = workCenterCatalogService;
        _dunnageTypeVisibilityCatalogService = dunnageTypeVisibilityCatalogService;
        _newRequestAlertService = newRequestAlertService;
        _startupState = startupState;
        ComputerManagement = computerManagement;
        UrgencyAllotments = urgencyAllotments;

        ElementTheme = _themeSelectorService.Theme;
        VersionDescription = GetVersionDescription();

        // Per-user new-request alert toggle, default OFF when never set.
        _newRequestAlertInitializing = true;
        NewRequestAlertsEnabled = _newRequestAlertService.GetEnabledAsync().GetAwaiter().GetResult();
        _newRequestAlertInitializing = false;

        _ = UrgencyAllotments.LoadAsync();
        InitializeIgnoredLocations();

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

        StartupDebugLog.Info("SettingsViewModel", $"NewRequestAlertsEnabled changed to {value}.");
        _ = _newRequestAlertService.SetEnabledAsync(value);
        RefreshSearchVisibility();
    }

    partial void OnSearchQueryChanged(string value)
    {
        StartupDebugLog.Info("SettingsViewModel", $"SearchQuery changed to '{value}'.");
        RefreshSearchVisibility();
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

    private void RefreshSearchVisibility()
    {
        OnPropertyChanged(nameof(IsAppearancePanelVisible));
        OnPropertyChanged(nameof(IsHotWorkCentersPanelVisible));
        OnPropertyChanged(nameof(IsDunnageTypeVisibilityPanelVisible));
        OnPropertyChanged(nameof(IsIgnoredLocationsPanelVisible));
        OnPropertyChanged(nameof(IsAboutPanelVisible));
        OnPropertyChanged(nameof(IsComputersPanelVisible));
        OnPropertyChanged(nameof(IsAppearanceCategoryVisible));
        OnPropertyChanged(nameof(IsOperationsCategoryVisible));
        OnPropertyChanged(nameof(IsAboutCategoryVisible));
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
